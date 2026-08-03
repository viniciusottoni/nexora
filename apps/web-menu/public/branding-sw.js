// US-021 §3.1/§9: "Service Worker com cache do cardápio e das imagens" / "mantém cardápio... em
// cache, o que reduz o tempo de carregamento em acessos subsequentes e sustenta a operação em
// contingência". Escopo mínimo desta wave: cache-then-network para os dois GETs públicos que a
// tela de mesa depende (branding e cardápio) — o suficiente para "Retorno após fechar o
// navegador" (US-021 §4) e para tolerar uma queda momentânea de rede sem tela em branco.
//
// [FORA DO ESCOPO, documentado] Otimização de imagem (thumbnails/formatos modernos) e
// pré-renderização para a meta de 2s em 4G (US-021 §3.1) NÃO estão aqui — exigiriam pipeline de
// build de imagem no backend de mídia (US-010/US-011) e um orçamento de performance dedicado,
// fora do escopo de US-021/US-022. Cache de imagem de produto (`imageUrl` do cardápio) também
// fica para uma próxima wave: precisaria de uma estratia de invalidação por versão de mídia, que
// ainda não existe.
const RUNTIME_CACHE = 'web-menu-runtime-v1';
const CACHEABLE_PATH_SUFFIXES = ['/v1/local/branding', '/v1/public/menu'];

self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  if (event.request.method !== 'GET') return;
  if (!CACHEABLE_PATH_SUFFIXES.some((suffix) => url.pathname.endsWith(suffix.split('?')[0]))) return;
  event.respondWith(fetchWithCacheFallback(event.request));
});

async function fetchWithCacheFallback(request) {
  const cache = await caches.open(RUNTIME_CACHE);
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
