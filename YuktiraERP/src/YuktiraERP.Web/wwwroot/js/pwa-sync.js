(function () {
  'use strict';

  var VERSION = 'yuktira-v230';
  var DB_NAME = 'yuktira-pwa';
  var STORE_NAME = 'queue';
  var STATUS_EVENT = 'yuktira:pwa-status';
  var CHIP_ID = 'yuktiraPwaChip';
  var MAX_RETRIES = 5;
  var FLUSH_INTERVAL_MS = 30000;
  var BASE_BACKOFF_MS = 2000;
  var MAX_BACKOFF_MS = 60000;

  var dbPromise = null;
  var flushing = false;
  var backoffUntil = {};
  var listeners = [];
  var chip = null;
  var chipStyle = null;

  var state = {
    version: VERSION,
    online: typeof navigator === 'undefined' || navigator.onLine !== false,
    flushing: false,
    queued: 0,
    sending: 0,
    failed: 0,
    synced: 0,
    lastSyncAt: null,
    lastError: null
  };

  function openDb() {
    if (dbPromise) return dbPromise;
    dbPromise = new Promise(function (resolve, reject) {
      if (typeof indexedDB === 'undefined') {
        reject(new Error('IndexedDB is not available in this browser'));
        return;
      }
      var req = indexedDB.open(DB_NAME, 1);
      req.onupgradeneeded = function () {
        var db = req.result;
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          var store = db.createObjectStore(STORE_NAME, { keyPath: 'id', autoIncrement: true });
          store.createIndex('status', 'status', { unique: false });
        }
      };
      req.onsuccess = function () { resolve(req.result); };
      req.onerror = function () { reject(req.error); };
      req.onblocked = function () { reject(new Error('Offline queue database is blocked')); };
    });
    return dbPromise;
  }

  function runTx(mode, work) {
    return openDb().then(function (db) {
      return new Promise(function (resolve, reject) {
        var tx = db.transaction(STORE_NAME, mode);
        var store = tx.objectStore(STORE_NAME);
        var out = null;
        var req = work(store);
        if (req) {
          req.onsuccess = function () { out = req.result; };
          req.onerror = function () { };
        }
        tx.oncomplete = function () { resolve(out); };
        tx.onerror = function () { reject(tx.error); };
        tx.onabort = function () { reject(tx.error); };
      });
    });
  }

  function snapshot(extra) {
    var payload = {
      version: state.version,
      online: isOnline(),
      flushing: flushing,
      queued: state.queued,
      sending: state.sending,
      failed: state.failed,
      synced: state.synced,
      lastSyncAt: state.lastSyncAt,
      lastError: state.lastError
    };
    return Object.assign(payload, extra || {});
  }

  function emit(extra) {
    var payload = snapshot(extra);
    if (typeof document !== 'undefined') {
      try {
        document.dispatchEvent(new CustomEvent(STATUS_EVENT, { detail: payload }));
      } catch (e) { }
    }
    listeners.slice().forEach(function (fn) {
      try { fn(payload); } catch (e) { }
    });
    paintChip(payload);
    return payload;
  }

  function isOnline() {
    return typeof navigator === 'undefined' || navigator.onLine !== false;
  }

  function normalizeHeaders(headers) {
    var out = {};
    if (!headers) return out;
    if (typeof headers.forEach === 'function' && !Array.isArray(headers)) {
      headers.forEach(function (value, key) { out[key] = value; });
      return out;
    }
    Object.keys(headers).forEach(function (key) { out[key] = headers[key]; });
    return out;
  }

  function toBody(options) {
    var body = options.body;
    if (body === undefined || body === null) return null;
    if (typeof body === 'string') return body;
    try { return JSON.stringify(body); } catch (e) { return String(body); }
  }

  function refreshCounts(records) {
    state.queued = records.filter(function (r) { return r.status === 'queued'; }).length;
    state.sending = records.filter(function (r) { return r.status === 'sending'; }).length;
    state.failed = records.filter(function (r) { return r.status === 'failed'; }).length;
    return state.queued;
  }

  function countQueue() {
    return runTx('readonly', function (store) { return store.getAll(); })
      .then(function (records) { return refreshCounts(records || []); })
      .catch(function () { return state.queued; });
  }

  function enqueue(url, options) {
    var opts = options || {};
    var record = {
      url: String(url),
      method: (opts.method || 'GET').toUpperCase(),
      headers: normalizeHeaders(opts.headers),
      body: toBody(opts),
      createdAt: Date.now(),
      retries: 0,
      lastError: null,
      status: 'queued'
    };

    return runTx('readwrite', function (store) { return store.add(record); })
      .then(function (id) {
        state.queued += 1;
        emit();
        scheduleFlush(1500);
        return Object.assign({}, record, { id: id });
      });
  }

  function enqueueFetch(url, options) {
    return enqueue(url, options).then(function (record) {
      return new Response(JSON.stringify({ queued: true, id: record.id, url: record.url }), {
        status: 202,
        statusText: 'Queued for sync',
        headers: { 'Content-Type': 'application/json' }
      });
    });
  }

  function getQueue() {
    return runTx('readonly', function (store) { return store.getAll(); })
      .then(function (records) {
        var rows = (records || []).slice().sort(function (a, b) { return a.id - b.id; });
        refreshCounts(rows);
        return rows;
      });
  }

  function clearQueue() {
    return runTx('readwrite', function (store) { return store.clear(); })
      .then(function () {
        backoffUntil = {};
        state.queued = 0;
        state.sending = 0;
        state.failed = 0;
        state.lastError = null;
        emit();
      });
  }

  function updateRecord(record, patch) {
    var next = Object.assign({}, record, patch);
    return runTx('readwrite', function (store) { return store.put(next); })
      .then(function () { return next; });
  }

  function deleteRecord(record) {
    return runTx('readwrite', function (store) { return store.delete(record.id); });
  }

  function backoffDelay(retries) {
    var delay = BASE_BACKOFF_MS * Math.pow(2, Math.max(0, retries - 1));
    return Math.min(delay, MAX_BACKOFF_MS);
  }

  async function sendOne(record) {
    await updateRecord(record, { status: 'sending' });
    var init = {
      method: record.method,
      credentials: 'same-origin',
      headers: record.headers || {}
    };
    if (record.body !== null && record.method !== 'GET' && record.method !== 'HEAD') {
      init.body = record.body;
    }

    try {
      var res = await fetch(record.url, init);
      if (res && res.ok) {
        await updateRecord(record, { status: 'sent', lastError: null });
        delete backoffUntil[record.id];
        state.synced += 1;
        state.lastSyncAt = Date.now();
        state.lastError = null;
        return 'sent';
      }
      var status = res ? res.status : 0;
      var message = 'Sync rejected with HTTP ' + status;
      var clientError = status >= 400 && status < 500 && status !== 408 && status !== 429;
      if (clientError) {
        await updateRecord(record, { status: 'failed', lastError: message });
        state.lastError = message;
        return 'failed';
      }
      return await markRetry(record, message);
    } catch (err) {
      var reason = err && err.message ? err.message : 'Network unavailable';
      return await markRetry(record, reason);
    }
  }

  async function markRetry(record, message) {
    var retries = (record.retries || 0) + 1;
    if (retries >= MAX_RETRIES) {
      await updateRecord(record, { status: 'failed', retries: retries, lastError: message });
      state.lastError = message;
      return 'failed';
    }
    backoffUntil[record.id] = Date.now() + backoffDelay(retries);
    await updateRecord(record, { status: 'queued', retries: retries, lastError: message });
    state.lastError = message;
    return 'queued';
  }

  async function flush() {
    if (flushing) return snapshot();
    if (!isOnline()) return emit({ online: false });
    flushing = true;
    state.flushing = true;
    emit();

    try {
      var records = await runTx('readonly', function (store) { return store.getAll(); });
      var ordered = (records || []).slice().sort(function (a, b) { return a.id - b.id; });
      for (var i = 0; i < ordered.length; i++) {
        var record = ordered[i];
        if (record.status === 'sent' || record.status === 'failed') continue;
        var wait = backoffUntil[record.id] || 0;
        if (wait > Date.now()) break;
        if (!isOnline()) break;
        await sendOne(record);
      }

      var remaining = await runTx('readonly', function (store) { return store.getAll(); });
      var sent = (remaining || []).filter(function (r) { return r.status === 'sent'; });
      for (var j = 0; j < sent.length; j++) {
        await deleteRecord(sent[j]);
      }
      refreshCounts(remaining || []);
    } catch (err) {
      state.lastError = err && err.message ? err.message : 'Sync failed';
    } finally {
      flushing = false;
      state.flushing = false;
      emit();
    }

    return snapshot();
  }

  function ensureChip() {
    if (chip || typeof document === 'undefined') return chip;
    if (!document.body) return null;
    chip = document.createElement('button');
    chip.type = 'button';
    chip.id = CHIP_ID;
    chip.className = 'yuktira-pwa-chip';
    chip.setAttribute('aria-live', 'polite');
    chip.addEventListener('click', function () { flush(); });
    document.body.appendChild(chip);

    chipStyle = document.createElement('style');
    chipStyle.id = 'yuktiraPwaChipStyle';
    chipStyle.textContent =
      '.yuktira-pwa-chip{position:fixed;right:18px;bottom:18px;z-index:1080;display:none;align-items:center;gap:8px;' +
      'border:none;border-radius:999px;padding:9px 15px;font-size:12.5px;font-weight:600;letter-spacing:.01em;' +
      'background:#1F497D;color:#fff;box-shadow:0 6px 18px rgba(31,73,125,.35);cursor:pointer;}' +
      '.yuktira-pwa-chip:hover{background:#16365a;}' +
      '.yuktira-pwa-chip.is-visible{display:inline-flex;}' +
      '.yuktira-pwa-chip.is-offline{background:#0f172a;}' +
      '.yuktira-pwa-chip.is-failed{background:#b91c1c;}' +
      '.yuktira-pwa-chip .yuktira-pwa-chip-icon{width:15px;height:15px;flex-shrink:0;}' +
      '.yuktira-pwa-chip.is-busy .yuktira-pwa-chip-icon{animation:yuktiraPwaSpin 1.1s linear infinite;}' +
      '@keyframes yuktiraPwaSpin{from{transform:rotate(0deg)}to{transform:rotate(360deg)}}';
    document.head.appendChild(chipStyle);
    return chip;
  }

  function paintChip(payload) {
    var el = ensureChip();
    if (!el) return;
    var icon = 'bi-cloud-arrow-up';
    var text = '';
    var classes = ['yuktira-pwa-chip'];

    if (!payload.online) {
      icon = 'bi-arrow-repeat';
      text = payload.queued > 0 ? 'Offline · ' + payload.queued + ' change(s) waiting' : 'Offline';
      classes.push('is-offline');
    } else if (payload.flushing) {
      icon = 'bi-arrow-repeat';
      text = 'Syncing changes…';
      classes.push('is-busy');
    } else if (payload.queued > 0) {
      icon = 'bi-cloud-arrow-up';
      text = payload.queued + ' change(s) waiting to sync';
    } else if (payload.failed > 0) {
      icon = 'bi-exclamation-triangle';
      text = payload.failed + ' change(s) failed to sync';
      classes.push('is-failed');
    } else if (payload.synced > 0 && payload.lastSyncAt && Date.now() - payload.lastSyncAt < 6000) {
      icon = 'bi-check-circle';
      text = 'Changes synced';
    } else {
      el.classList.remove('is-visible');
      el.innerHTML = '';
      return;
    }

    el.className = classes.join(' ');
    el.classList.add('is-visible');
    el.title = payload.lastError ? payload.lastError : 'Sync now';
    el.innerHTML =
      '<svg class="bi yuktira-pwa-chip-icon" aria-hidden="true" focusable="false"><use href="/images/sprite.svg#' + icon + '"></use></svg>' +
      '<span>' + text.replace(/&/g, '&amp;').replace(/</g, '&lt;') + '</span>';
  }

  function scheduleFlush(delay) {
    if (typeof window === 'undefined') return;
    window.setTimeout(function () { flush(); }, delay || 0);
  }

  function onStatus(callback) {
    if (typeof callback !== 'function') return function () { };
    listeners.push(callback);
    try { callback(snapshot()); } catch (e) { }
    return function () {
      var index = listeners.indexOf(callback);
      if (index >= 0) listeners.splice(index, 1);
    };
  }

  function bindEnvironment() {
    if (typeof window === 'undefined') return;
    window.addEventListener('online', function () {
      state.online = true;
      emit();
      scheduleFlush(500);
    });
    window.addEventListener('offline', function () {
      state.online = false;
      emit();
    });
    window.setInterval(function () {
      if (isOnline() && state.queued > 0) flush();
      else if (!isOnline()) emit({ online: false });
    }, FLUSH_INTERVAL_MS);
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', function () { paintChip(snapshot()); });
    } else {
      paintChip(snapshot());
    }
  }

  window.YuktiraPwa = {
    version: VERSION,
    get isOnline() { return isOnline(); },
    get pendingCount() { return state.queued; },
    enqueue: enqueue,
    enqueueFetch: enqueueFetch,
    flush: flush,
    getQueue: getQueue,
    clearQueue: clearQueue,
    onStatus: onStatus
  };

  countQueue().then(function () { emit(); });
  bindEnvironment();
})();
