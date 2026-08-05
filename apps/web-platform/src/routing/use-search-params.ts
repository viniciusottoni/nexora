import { useCallback, useEffect, useState } from 'react';

/**
 * US-151 §3.1 "preservação de filtros na URL" / critério de aceite "busca e filtros devem
 * permanecer refletidos na URL" — irmão de `use-pathname.ts`: mesma convenção de History API
 * nativa (`pushState`/`replaceState` + `popstate`), sem lib de rotas de terceiros. Só cuida da
 * querystring (`location.search`); o pathname continua por conta de `usePathname`.
 */
export function useSearchParams(): readonly [
  URLSearchParams,
  (params: URLSearchParams, options?: { readonly replace?: boolean }) => void,
] {
  const [search, setSearch] = useState(() => new URLSearchParams(globalThis.location?.search ?? ''));

  useEffect(() => {
    const onPopState = () => setSearch(new URLSearchParams(globalThis.location?.search ?? ''));
    globalThis.addEventListener?.('popstate', onPopState);
    return () => globalThis.removeEventListener?.('popstate', onPopState);
  }, []);

  const setParams = useCallback((params: URLSearchParams, options?: { readonly replace?: boolean }) => {
    const queryString = params.toString();
    const path = `${globalThis.location?.pathname ?? '/'}${queryString ? `?${queryString}` : ''}`;
    // `replace` é o padrão: sincronizar busca/filtro a cada tecla/seleção não deveria empilhar uma
    // entrada de histórico por mudança — só a navegação de página (pushState de `usePathname`)
    // deve virar um passo de "voltar".
    if (options?.replace === false) {
      globalThis.history?.pushState({}, '', path);
    } else {
      globalThis.history?.replaceState({}, '', path);
    }
    setSearch(new URLSearchParams(queryString));
  }, []);

  return [search, setParams] as const;
}
