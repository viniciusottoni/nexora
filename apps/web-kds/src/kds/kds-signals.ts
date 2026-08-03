import type { CatalogChannel } from '@nexora/contracts';

/** Vocabulário de canal de `OrderTicket` (packages/ui) — diferente do vocabulário do contrato (`CatalogChannel`). */
export type OrderTicketChannel = 'DINE_IN' | 'DELIVERY' | 'COUNTER';

/**
 * US-031 §10/critério de aceite "Pedido de delivery ... deve ser visualmente distinguível dos
 * pedidos de salão" — `OrderTicket` já pinta ícone/rótulo por canal; esta função só traduz o
 * vocabulário do contrato (`"DineIn"`/`"Delivery"`/`"Takeout"`/`"Marketplace"`) para o vocabulário
 * que o componente entende. Marketplace (delivery por app de terceiro) usa o MESMO badge visual de
 * Delivery — para a cozinha, os dois chegam embalados para viagem, a distinção de canal comercial
 * não importa na bancada.
 */
export function toOrderTicketChannel(channel: CatalogChannel): OrderTicketChannel {
  switch (channel) {
    case 'Delivery':
    case 'Marketplace':
      return 'DELIVERY';
    case 'Takeout':
      return 'COUNTER';
    case 'DineIn':
    default:
      return 'DINE_IN';
  }
}

/** "há 4 s" / "há 12 min" — formato esperado por `SyncStatus.lastSync` (RF-OFF-05/RF-BI-14). */
export function formatRelativeSync(lastSyncAt: Date, now = new Date()): string {
  const seconds = Math.max(0, Math.round((now.getTime() - lastSyncAt.getTime()) / 1000));
  if (seconds < 60) return `há ${seconds} s`;
  return `há ${Math.round(seconds / 60)} min`;
}
