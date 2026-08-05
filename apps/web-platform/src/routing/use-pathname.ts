import { useCallback, useEffect, useState } from 'react';

/**
 * US-150 §10 "a rota atual deve permanecer visível após recarregar a página" — navegação via
 * History API (`pushState`/`popstate`), sem biblioteca de rotas: mesma convenção já usada pelo
 * `web-admin` (`isLocalEdgeAdminPath`) e pela checagem de onboarding original deste app — nenhum
 * outro app do monorepo depende de um router de terceiros (CLAUDE.md "motion.css" §"Proibido...
 * biblioteca externa" é sobre animação, mas o espírito — CSS/JS nativo no shell administrativo —
 * se aplica igual aqui: reload em qualquer caminho funciona porque o `pathname` é lido direto de
 * `location`, não de um estado de navegação perdido no reload).
 */
export function usePathname(): readonly [string, (path: string) => void] {
  const [pathname, setPathname] = useState(() => globalThis.location?.pathname ?? '/');

  useEffect(() => {
    const onPopState = () => setPathname(globalThis.location?.pathname ?? '/');
    globalThis.addEventListener?.('popstate', onPopState);
    return () => globalThis.removeEventListener?.('popstate', onPopState);
  }, []);

  const navigate = useCallback((path: string) => {
    if (globalThis.location && `${globalThis.location.pathname}${globalThis.location.search}` === path) return;

    globalThis.history?.pushState({}, '', path);

    // US-152 — `path` pode chegar com querystring (ex.: `/auditoria-suporte?tenantId=...`, para
    // pré-preencher o fluxo de suporte a partir da ficha do estabelecimento). O ESTADO de
    // `pathname` (usado por `matchRoute`, que só entende o caminho puro) fica só com o caminho;
    // a querystring vai inteira para `pushState` — quem precisa lê-la usa
    // `useSearchParams`/`location.search` diretamente (mesma divisão de responsabilidade já
    // existente entre este hook e `use-search-params.ts`).
    setPathname(path.split('?')[0] ?? path);
  }, []);

  return [pathname, navigate] as const;
}
