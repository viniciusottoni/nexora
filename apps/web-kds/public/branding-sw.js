const BRANDING_CACHE = 'runtime-branding-v1';

self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  if (event.request.method !== 'GET' || !url.pathname.endsWith('/v1/public/branding')) return;
  event.respondWith(fetchBranding(event.request));
});

async function fetchBranding(request) {
  const cache = await caches.open(BRANDING_CACHE);
  try {
    const response = await fetch(request);
    if (response.ok) await cache.put(request, response.clone());
    return response;
  } catch (error) {
    const cached = await cache.match(request);
    if (cached) return cached;
    throw error;
  }
}
