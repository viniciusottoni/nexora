/**
 * US-021 §3.1/§9 — registra o Service Worker mínimo de cache (branding + cardápio), mesmo padrão
 * de `apps/web-pos/src/register-branding-worker.ts`/`apps/web-kds/src/register-branding-worker.ts`.
 * Cache de imagem/otimização/pré-renderização ficam fora desta wave — ver comentário em
 * `public/branding-sw.js`.
 */
export function registerBrandingWorker(): void {
  if ('serviceWorker' in navigator) void navigator.serviceWorker.register('/branding-sw.js');
}
