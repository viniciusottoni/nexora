import { useEffect, useMemo, useState } from 'react';
import type { PublicMenuResponse, PublicTableAccessResponse } from '@nexora/contracts';
import { EmptyState, pickBrandLogo, useColorScheme, useRuntimeBranding } from '@nexora/ui';
import { ConsumptionView } from './consumption-view.js';
import { PublicTableApi, PublicTableApiError } from './public-table-api.js';
import { saveTableSession } from './session-storage.js';
import { TableMenuView } from './table-menu-view.js';
import { WaiterCallPanel } from './waiter-call-panel.js';
import './table-access.css';

export interface TableAccessPageProps {
  readonly qrToken: string;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  readonly storage?: Storage;
}

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'invalid'; readonly message: string }
  | { readonly kind: 'ready'; readonly access: PublicTableAccessResponse; readonly menu: PublicMenuResponse };

/**
 * Tela principal de acesso do cliente pela leitura do QR Code (US-021). Zero fricção: nenhuma
 * tela intermediária entre resolver a mesa e ver o cardápio (§10) — o único estado visível antes
 * do cardápio é o esqueleto de carregamento (nunca um giro indefinido).
 */
export function TableAccessPage({
  qrToken,
  baseUrl = '',
  fetcher = fetch,
  storage = typeof window === 'undefined' ? undefined : window.localStorage,
}: Readonly<TableAccessPageProps>) {
  const api = useMemo(() => new PublicTableApi(baseUrl, fetcher), [baseUrl, fetcher]);
  const [state, setState] = useState<LoadState>({ kind: 'loading' });

  useEffect(() => {
    let active = true;
    setState({ kind: 'loading' });

    async function load() {
      try {
        const access = await api.accessTable(qrToken);
        const menu = await api.getMenu();
        if (!active) return;

        if (storage) {
          saveTableSession(storage, {
            qrToken,
            sessionId: access.session.id,
            tableId: access.table.id,
            sessionToken: access.sessionToken,
            savedAt: new Date().toISOString(),
          });
        }

        setState({ kind: 'ready', access, menu });
      } catch (cause) {
        if (!active) return;
        // US-021 §4 "Token inválido ou rotacionado" / §10: mensagem amigável, sem jargão técnico,
        // orientando a chamar o garçom — nunca expõe o código/detalhe técnico do erro.
        setState({
          kind: 'invalid',
          message:
            cause instanceof PublicTableApiError && cause.code === 'INVALID_TABLE_TOKEN'
              ? 'Não conseguimos reconhecer esta mesa. Chame o garçom para continuar.'
              : 'Não foi possível abrir o cardápio agora. Chame o garçom para continuar.',
        });
      }
    }

    void load();
    return () => {
      active = false;
    };
  }, [api, qrToken, storage]);

  if (state.kind === 'loading') {
    return <MenuSkeleton />;
  }

  if (state.kind === 'invalid') {
    return (
      <div className="menu-access-error">
        <EmptyState icon="qr_code_2" title="Não foi possível abrir o cardápio">
          {state.message}
        </EmptyState>
      </div>
    );
  }

  return <BrandedTableMenu qrToken={qrToken} access={state.access} menu={state.menu} />;
}

type AccessTab = 'menu' | 'consumption';

/** Injeta a marca do TENANT (nunca a da Replay, ADR-010) do contexto de branding em runtime na view presentacional. */
function BrandedTableMenu({
  qrToken,
  access,
  menu,
}: Readonly<{ qrToken: string; access: PublicTableAccessResponse; menu: PublicMenuResponse }>) {
  const { branding } = useRuntimeBranding();
  const scheme = useColorScheme();
  const logo = pickBrandLogo(branding.logo, scheme);
  // US-024: aba "Consumo" dentro do MESMO fluxo da tela de acesso do cliente (não uma tela nova) —
  // "Cardápio" continua sendo a aba inicial, então nenhum teste/comportamento existente do
  // cardápio muda; a ConsumptionView (SignalR + polling, ADR-011) só monta quando o cliente troca
  // de aba.
  const [tab, setTab] = useState<AccessTab>('menu');

  return (
    <div className="table-access-tabs">
      <nav className="table-access-tabs__bar" aria-label="Seções da mesa">
        <button
          type="button"
          className={tab === 'menu' ? 'table-access-tabs__tab table-access-tabs__tab--active' : 'table-access-tabs__tab'}
          aria-current={tab === 'menu'}
          onClick={() => setTab('menu')}
        >
          Cardápio
        </button>
        <button
          type="button"
          className={tab === 'consumption' ? 'table-access-tabs__tab table-access-tabs__tab--active' : 'table-access-tabs__tab'}
          aria-current={tab === 'consumption'}
          onClick={() => setTab('consumption')}
        >
          Consumo
        </button>
      </nav>

      {tab === 'menu' ? (
        <TableMenuView table={access.table} menu={menu} {...(logo ? { logo } : {})} />
      ) : (
        <ConsumptionView sessionToken={access.sessionToken} />
      )}

      <WaiterCallPanel qrToken={qrToken} sessionToken={access.sessionToken} />
    </div>
  );
}

/**
 * Esqueleto de conteúdo (US-021 §10: "estado de carregamento com esqueleto de conteúdo, não com
 * giro indefinido") — blocos no formato do cardápio final, sem depender de nenhuma biblioteca.
 */
function MenuSkeleton() {
  return (
    <div className="menu-skeleton" role="status" aria-label="Carregando cardápio">
      <div className="menu-skeleton__hero" />
      {[0, 1, 2].map((row) => (
        <div key={row} className="menu-skeleton__row">
          <div className="menu-skeleton__photo" />
          <div className="menu-skeleton__lines">
            <div className="menu-skeleton__line menu-skeleton__line--title" />
            <div className="menu-skeleton__line" />
            <div className="menu-skeleton__line menu-skeleton__line--short" />
          </div>
        </div>
      ))}
    </div>
  );
}
