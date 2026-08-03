/**
 * US-033 (Cancelar item ou pedido com autorização) §10 — "motivo escolhido de lista curta, com
 * opção de observação — texto livre obrigatório gera preenchimento aleatório". A lista mora aqui,
 * no cliente (Fase 1) — nunca hardcoded no domínio/backend (ADR-013, "cada estabelecimento
 * organiza conforme sua realidade"); quando a US-063 (sincronização de configuração) trouxer a
 * lista por tenant, este arquivo passa a ser só o fallback offline.
 */
export interface CancellationReasonOption {
  readonly code: string;
  readonly label: string;
}

export const CANCELLATION_REASONS: readonly CancellationReasonOption[] = [
  { code: 'CUSTOMER_REQUEST', label: 'Cliente desistiu' },
  { code: 'ORDER_MISTAKE', label: 'Erro no lançamento do pedido' },
  { code: 'PRODUCT_UNAVAILABLE', label: 'Produto ficou indisponível' },
  { code: 'LONG_WAIT', label: 'Demora excessiva na produção' },
  { code: 'QUALITY_ISSUE', label: 'Problema de qualidade' },
  { code: 'OTHER', label: 'Outro motivo' },
];
