const CACHE_NAME = 'yuktira-v230';
const API_TTL_MS = 30000;
const SHELL_URL = '/';
const PRECACHE_URLS = [
  '/',
  '/Dashboard',
  '/manifest.json',
  '/css/yuktira.css',
  '/js/yuktira.js',
  '/js/pwa-sync.js',
  '/images/sprite.svg'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => Promise.all(PRECACHE_URLS.map((url) =>
        fetch(url, { credentials: 'same-origin', redirect: 'follow' })
          .then((response) => {
            if (response && (response.ok || response.type === 'opaque')) {
              return cache.put(url, response.clone());
            }
            return null;
          })
          .catch(() => null)
      )))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (!request || request.method !== 'GET') return;

  const url = new URL(request.url);
  if (url.origin !== self.location.origin) return;

  if (isBlockedPath(url.pathname)) return;

  if (request.mode === 'navigate') {
    event.respondWith(handleNavigation(request));
    return;
  }

  if (url.pathname.startsWith('/api/')) {
    event.respondWith(handleApi(event, request));
    return;
  }

  event.respondWith(handleStatic(event, request));
});

function isBlockedPath(pathname) {
  const path = pathname.toLowerCase();
  if (path.startsWith('/api/auth')) return true;
  if (path.startsWith('/auth/')) return true;
  if (path.startsWith('/hubs/')) return true;
  if (path === '/health' || path === '/metrics') return true;
  return false;
}

async function handleNavigation(request) {
  try {
    const fresh = await fetch(request);
    if (fresh && fresh.ok) {
      const cache = await caches.open(CACHE_NAME);
      await cache.put(request, fresh.clone());
    }
    return fresh;
  } catch (error) {
    const cached = await caches.match(request, { ignoreSearch: false });
    if (cached) return cached;
    const shell = await caches.match(SHELL_URL);
    if (shell) return shell;
    return new Response('This page is not available offline yet.', {
      status: 503,
      headers: { 'Content-Type': 'text/plain', 'Cache-Control': 'no-store' }
    });
  }
}

async function handleStatic(event, request) {
  const cached = await caches.match(request);
  const revalidate = fetch(request).then(async (response) => {
    if (response && response.ok) {
      const cache = await caches.open(CACHE_NAME);
      await cache.put(request, response.clone());
    }
    return response;
  });

  if (cached) {
    event.waitUntil(revalidate.catch(() => null));
    return cached;
  }

  try {
    const response = await revalidate;
    if (!response) throw new Error('Empty response');
    return response;
  } catch (error) {
    return new Response('', { status: 504, headers: { 'Cache-Control': 'no-store' } });
  }
}

async function handleApi(event, request) {
  const cache = await caches.open(CACHE_NAME);
  try {
    const fresh = await fetch(request);
    if (fresh && fresh.ok) {
      const payload = await fresh.clone().arrayBuffer();
      const headers = new Headers(fresh.headers);
      headers.set('x-yuktira-cached-at', String(Date.now()));
      const stored = new Response(payload, {
        status: fresh.status,
        statusText: fresh.statusText,
        headers: headers
      });
      await cache.put(request, stored);
    }
    return fresh;
  } catch (error) {
    const cached = await cache.match(request);
    if (!cached) {
      return offlineJson();
    }
    const cachedAt = Number(cached.headers.get('x-yuktira-cached-at')) || 0;
    const age = Date.now() - cachedAt;
    if (cachedAt && age <= API_TTL_MS) {
      const headers = new Headers(cached.headers);
      headers.set('x-yuktira-offline', 'true');
      headers.set('x-yuktira-age', String(age));
      const body = await cached.arrayBuffer();
      return new Response(body, { status: cached.status, statusText: cached.statusText, headers: headers });
    }
    return offlineJson();
  }
}

function offlineJson() {
  return new Response(JSON.stringify({ error: 'You are offline and this response is not cached.', offline: true }), {
    status: 503,
    statusText: 'Offline',
    headers: { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' }
  });
}
