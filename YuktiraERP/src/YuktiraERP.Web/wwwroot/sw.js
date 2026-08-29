// Service worker removed — unregister and self-destruct
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', () => {
  self.registration.unregister();
  caches.keys().then(keys => Promise.all(keys.map(k => caches.delete(k))));
  self.clients.matchAll().then(clients => clients.forEach(c => c.navigate(c.url)));
});
