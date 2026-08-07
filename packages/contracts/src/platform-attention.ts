import { z } from 'zod';

/**
 * US-157 · Central operacional, auditoria e atalhos de suporte — espelha
 * `Nexora.Contracts.Platform.PlatformAttentionContracts` (C#). `GET /v1/platform/attention`
 * (fila priorizada), `POST /v1/platform/attention/{itemId}/acknowledgements` (reconhecimento sem
 * apagar o fato original, RN-004) e `GET /v1/platform/attention/export` (CSV, tratado como download
 * binário pelo cliente HTTP — sem schema Zod aqui, mesmo padrão de PDFs no backend).
 */
export const attentionSeveritySchema = z.enum(['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']);

export const attentionItemTypeSchema = z.enum([
  'INSTALLATION_OFFLINE',
  'INSTALLATION_DEGRADED',
  'INVITE_EXPIRED',
  'PROVISIONING_STALLED',
]);

export const attentionActionSchema = z.object({
  kind: z.enum(['OPEN_DIAGNOSTICS', 'OPEN_TENANT']),
  href: z.string().min(1),
});

export const attentionQueueItemSchema = z.object({
  id: z.string().min(1),
  tenantId: z.string().uuid(),
  tenantName: z.string().min(1),
  type: attentionItemTypeSchema,
  severity: attentionSeveritySchema,
  since: z.string().datetime({ offset: true }),
  reason: z.string().min(1),
  action: attentionActionSchema,
});

export const attentionQueueMetaSchema = z.object({
  collectedAt: z.string().datetime({ offset: true }),
  unavailableSources: z.array(z.string()),
});

export const attentionQueueListResponseSchema = z.object({
  data: z.array(attentionQueueItemSchema),
  nextCursor: z.string().nullable(),
  meta: attentionQueueMetaSchema,
});

export const acknowledgeAttentionItemRequestSchema = z.object({
  reason: z.string().trim().min(1, 'O motivo é obrigatório.'),
});

export const attentionAcknowledgementResponseSchema = z.object({
  id: z.string().uuid(),
  itemId: z.string().min(1),
  reason: z.string().min(1),
  acknowledgedAt: z.string().datetime({ offset: true }),
});

export type AttentionSeverity = z.infer<typeof attentionSeveritySchema>;
export type AttentionItemType = z.infer<typeof attentionItemTypeSchema>;
export type AttentionAction = z.infer<typeof attentionActionSchema>;
export type AttentionQueueItem = z.infer<typeof attentionQueueItemSchema>;
export type AttentionQueueMeta = z.infer<typeof attentionQueueMetaSchema>;
export type AttentionQueueListResponse = z.infer<typeof attentionQueueListResponseSchema>;
export type AcknowledgeAttentionItemRequest = z.infer<typeof acknowledgeAttentionItemRequestSchema>;
export type AttentionAcknowledgementResponse = z.infer<
  typeof attentionAcknowledgementResponseSchema
>;
