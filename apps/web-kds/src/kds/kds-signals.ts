import type { CatalogChannel, KdsQueueItem } from '@nexora/contracts';

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

/**
 * US-040 §3 ("Grade de cartões, um por pedido") — um cartão por PEDIDO, não por item: a fila que
 * `GetKdsQueueQuery` devolve é sempre por item (mesma decisão de US-031), então o agrupamento é
 * responsabilidade do cliente. Um pedido com item no forno e item em bebidas nesta MESMA praça
 * (ex.: duas pizzas) vira um cartão só, com uma linha por item.
 */
export interface KdsOrderGroup {
  readonly orderId: string;
  readonly orderCode: string;
  readonly table: string | null;
  readonly channel: CatalogChannel;
  /** ISO do item mais antigo do pedido — "o cartão mais antigo sempre visível" usa este valor pra ordenar. */
  readonly oldestPlacedAt: string;
  /** Limiar mais urgente entre os itens do pedido — o cartão fica amarelo/vermelho assim que QUALQUER item precisar de atenção. */
  readonly warnSeconds: number;
  readonly criticalSeconds: number;
  readonly items: readonly KdsQueueItem[];
}

export function groupItemsByOrder(items: readonly KdsQueueItem[]): readonly KdsOrderGroup[] {
  const groups = new Map<string, KdsQueueItem[]>();
  for (const item of items) {
    const existing = groups.get(item.orderId);
    if (existing) {
      existing.push(item);
    } else {
      groups.set(item.orderId, [item]);
    }
  }

  const result: KdsOrderGroup[] = [];
  for (const [orderId, groupItems] of groups) {
    const sorted = [...groupItems].sort(
      (a, b) => new Date(a.placedAt).getTime() - new Date(b.placedAt).getTime(),
    );
    const first = sorted[0]!;
    result.push({
      orderId,
      orderCode: first.orderCode,
      table: first.table,
      channel: first.channel,
      oldestPlacedAt: first.placedAt,
      warnSeconds: Math.min(...sorted.map((i) => i.warnSeconds)),
      criticalSeconds: Math.min(...sorted.map((i) => i.criticalSeconds)),
      items: sorted,
    });
  }

  return result.sort((a, b) => new Date(a.oldestPlacedAt).getTime() - new Date(b.oldestPlacedAt).getTime());
}

/** US-040 §4 ("meio a meio no cartão") — combina o nome-base do item com os sabores da fração: "Pizza G · Mussarela / Calabresa". */
export function formatItemName(item: Pick<KdsQueueItem, 'productName' | 'fractions'>): string {
  if (item.fractions.length === 0) return item.productName;
  return `${item.productName} · ${item.fractions.map((f) => f.productName).join(' / ')}`;
}
