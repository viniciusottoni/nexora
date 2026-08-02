import { useEffect, useState } from 'react';
import { AlertBanner, Card, EmptyState } from '@nexora/ui';
import {
  subscribeToAvailability,
  type AvailabilityApi,
  type AvailabilitySubscription,
  type ProductAvailabilityDto,
} from './availability-api.js';
import { UnavailableToggle } from './unavailable-toggle.js';

export interface UnavailablePanelProps {
  readonly api: AvailabilityApi;
  readonly accessToken?: string;
  readonly subscribeFn?: typeof subscribeToAvailability;
}

const noopSubscription = (): AvailabilitySubscription => ({ close: () => undefined });

export function UnavailablePanel({
  api,
  accessToken,
  subscribeFn = subscribeToAvailability,
}: Readonly<UnavailablePanelProps>) {
  const [items, setItems] = useState<readonly ProductAvailabilityDto[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState<string>();

  useEffect(() => {
    let active = true;

    const refresh = async () => {
      try {
        const response = await api.listUnavailable();
        if (active) {
          setItems(response.items);
          setError(undefined);
          setLoaded(true);
        }
      } catch (cause) {
        if (active) {
          setError(
            cause instanceof Error ? cause.message : 'Não foi possível carregar a disponibilidade.',
          );
          setLoaded(true);
        }
      }
    };

    void refresh();
    const subscription = subscribeFn(
      (event) => {
        if (event.type === 'product.available') {
          setItems((current) => current.filter((item) => item.productId !== event.data.productId));
        } else {
          void refresh();
        }
      },
      { api, ...(accessToken ? { accessToken } : {}) },
    );

    return () => {
      active = false;
      subscription.close();
    };
  }, [accessToken, api, subscribeFn]);

  return (
    <Card
      title="Itens indisponíveis"
      subtitle="Visível para toda a operação"
      className="kds-unavailable-panel"
    >
      {error ? (
        <AlertBanner tone="danger" title="Falha ao carregar">
          {error}
        </AlertBanner>
      ) : null}
      {loaded && items.length === 0 ? (
        <EmptyState title="Todos os itens disponíveis">
          Nada marcado como indisponível agora.
        </EmptyState>
      ) : null}
      {items.map((item) => (
        <UnavailableToggle
          key={item.productId}
          productId={item.productId}
          productName={item.productName}
          isAvailable={item.isAvailable}
          unavailableReason={item.unavailableReason}
          api={api}
          {...(accessToken ? { accessToken } : {})}
          subscribeFn={noopSubscription}
          onChanged={(isAvailable) => {
            if (isAvailable) {
              setItems((current) =>
                current.filter((currentItem) => currentItem.productId !== item.productId),
              );
            }
          }}
        />
      ))}
    </Card>
  );
}
