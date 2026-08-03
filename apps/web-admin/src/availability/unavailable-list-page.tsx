import { useEffect, useState } from 'react';
import { AlertBanner, Badge, Button, Card, EmptyState } from '@nexora/ui';
import {
  AvailabilityApi,
  subscribeToAvailability,
  type AvailabilitySubscription,
  type ProductAvailabilityChangedEvent,
  type ProductAvailabilityDto,
} from './availability-api.js';
import './unavailable-list-page.css';

export interface UnavailableListPageProps {
  /** Injetável para teste — padrão `new AvailabilityApi()`. */
  readonly api?: AvailabilityApi;
  /** Injetável para teste — evita abrir um WebSocket real em ambiente de teste. */
  readonly subscribeFn?: typeof subscribeToAvailability;
}

/**
 * "Lista de itens indisponíveis sempre visível ao gestor" (US-015 §10, extensão do requisito do
 * garçom para o painel de administração) — carrega a lista ao montar e se mantém atualizada em
 * tempo real (WebSocket + fallback de polling a cada 5s, US-015 §9), refletindo tanto marcações
 * feitas na própria tela quanto no KDS/edge (autoridade bidirecional desta história).
 *
 * Auto-suficiente de propósito (busca os próprios dados via `AvailabilityApi`, não recebe props de
 * dado pronto) — mesmo padrão de `BrandingContainer`, porque `apps/web-admin/src/app.tsx` ainda não
 * foi integrado a esta tela nesta tarefa (ver relatório: arquivo fora do escopo permitido de
 * edição, o maintainer decide onde plugar `<UnavailableListPage />` na navegação).
 */
export function UnavailableListPage({
  api = new AvailabilityApi(),
  subscribeFn = subscribeToAvailability,
}: Readonly<UnavailableListPageProps>) {
  const [items, setItems] = useState<readonly ProductAvailabilityDto[]>();
  const [error, setError] = useState<string>();
  const [restoringId, setRestoringId] = useState<string>();

  useEffect(() => {
    let active = true;
    api
      .listUnavailable()
      .then((result) => {
        if (active) setItems(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    return () => {
      active = false;
    };
  }, [api]);

  useEffect(() => {
    const subscription: AvailabilitySubscription = subscribeFn(
      (event: ProductAvailabilityChangedEvent) => applyChange(event, setItems),
      { api },
    );
    return () => subscription.close();
  }, [api, subscribeFn]);

  async function restore(productId: string): Promise<void> {
    setError(undefined);
    setRestoringId(productId);
    try {
      await api.markAvailable(productId);
      setItems((current) => current?.filter((item) => item.productId !== productId));
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setRestoringId(undefined);
    }
  }

  if (!items) {
    return (
      <main className="db-page nx-anim-in" aria-labelledby="unavailable-title">
        <header className="db-page__header">
          <div className="db-page__heading">
            <p className="db-page__eyebrow">Cardápio</p>
            <h1 className="db-page__title" id="unavailable-title">
              Itens indisponíveis
            </h1>
            <p className="db-page__lead">
              Item marcado como indisponível sai do cardápio na hora, em todos os canais. Voltar a
              vender é um clique — e vale imediatamente.
            </p>
          </div>
        </header>
        {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}
        <output className="db-loading">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando itens indisponíveis…
        </output>
      </main>
    );
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="unavailable-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Cardápio</p>
          <h1 className="db-page__title" id="unavailable-title">
            Itens indisponíveis
          </h1>
          <p className="db-page__lead">
            Item marcado como indisponível sai do cardápio na hora, em todos os canais. Voltar a
            vender é um clique — e vale imediatamente.
          </p>
        </div>
      </header>
      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      {items.length === 0 ? (
        <Card padding="none">
          <EmptyState icon="block" title="Nenhum item indisponível">
            Todo o cardápio está disponível para venda agora.
          </EmptyState>
        </Card>
      ) : (
        <Card
          as="section"
          aria-label="Itens indisponíveis"
          title="Fora do cardápio agora"
          subtitle="Enquanto estiver aqui, o item não aparece para o cliente em nenhum canal."
          actions={<Badge tone="danger">{items.length}</Badge>}
        >
          <ul className="unavailable-list__items nx-stagger">
            {items.map((item) => (
              <li key={item.productId} className="unavailable-list__item">
                <span className="db-list__text">
                  <span className="db-list__name">{item.productName}</span>
                  {item.unavailableReason ? (
                    <span className="db-list__meta">{item.unavailableReason}</span>
                  ) : null}
                  {item.unavailableSince ? (
                    <time className="unavailable-list__since" dateTime={item.unavailableSince}>
                      Desde {formatDateTime(item.unavailableSince)}
                    </time>
                  ) : null}
                </span>
                <Button
                  type="button"
                  variant="accent"
                  size="sm"
                  busy={restoringId === item.productId}
                  onClick={() => void restore(item.productId)}
                >
                  Marcar disponível
                </Button>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </main>
  );
}

function applyChange(
  event: ProductAvailabilityChangedEvent,
  setItems: (
    updater: (
      current: readonly ProductAvailabilityDto[] | undefined,
    ) => readonly ProductAvailabilityDto[] | undefined,
  ) => void,
): void {
  if (event.type === 'product.available') {
    setItems((current) => current?.filter((item) => item.productId !== event.data.productId));
    return;
  }

  setItems((current) => {
    if (!current) return current;
    const withoutExisting = current.filter((item) => item.productId !== event.data.productId);
    const restored: ProductAvailabilityDto = {
      productId: event.data.productId,
      productName:
        current.find((item) => item.productId === event.data.productId)?.productName ?? 'Produto',
      isAvailable: false,
      unavailableReason: event.data.reason ?? null,
      unavailableSince: event.data.unavailableSince ?? null,
    };
    return [restored, ...withoutExisting];
  });
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'Não foi possível carregar os itens indisponíveis.';
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(
    new Date(value),
  );
}
