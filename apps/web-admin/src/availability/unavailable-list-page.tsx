import { useEffect, useState } from 'react';
import { Badge, Button, Card, EmptyState } from '@nexora/ui';
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

  if (error) {
    return (
      <p className="unavailable-list-error" role="alert">
        {error}
      </p>
    );
  }

  if (!items) {
    return (
      <output className="unavailable-list-loading">
        <span className="nx-spinner" aria-hidden="true" />
        Carregando itens indisponíveis…
      </output>
    );
  }

  if (items.length === 0) {
    return (
      <EmptyState title="Nenhum item indisponível">
        Todo o cardápio está disponível para venda agora.
      </EmptyState>
    );
  }

  return (
    <section className="unavailable-list" aria-label="Itens indisponíveis">
      <header className="unavailable-list__header">
        <h2>Itens indisponíveis</h2>
        <Badge tone="danger">{items.length}</Badge>
      </header>
      <ul className="unavailable-list__items nx-stagger">
        {items.map((item) => (
          <li key={item.productId}>
            <Card className="unavailable-list__card">
              <div>
                <strong>{item.productName}</strong>
                {item.unavailableReason ? <p>{item.unavailableReason}</p> : null}
                {item.unavailableSince ? (
                  <time className="unavailable-list__since" dateTime={item.unavailableSince}>
                    Desde {formatDateTime(item.unavailableSince)}
                  </time>
                ) : null}
              </div>
              <Button
                type="button"
                variant="accent"
                busy={restoringId === item.productId}
                onClick={() => void restore(item.productId)}
              >
                Marcar disponível
              </Button>
            </Card>
          </li>
        ))}
      </ul>
    </section>
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
