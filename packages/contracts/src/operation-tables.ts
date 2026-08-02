import { z } from 'zod';

/** Estado físico da mesa no mapa de salão (Nexora.Domain.Operation.TableStatus). */
export const tableStatusSchema = z.enum(['FREE', 'OCCUPIED', 'RESERVED', 'BLOCKED']);

/**
 * Porta de <c>TableResponse</c> (Nexora.Contracts.Operation) — US-020. Nunca carrega
 * <c>qrToken</c>: o token é um segredo de entrada, só sai do servidor embutido no PDF de
 * exportação de QR Codes (`GET /v1/tables/qr-codes.pdf`).
 */
export const tableSchema = z.object({
  id: z.string().uuid(),
  areaId: z.string().uuid(),
  areaName: z.string().min(1),
  label: z.string().min(1),
  seats: z.number().int().positive(),
  status: tableStatusSchema,
  active: z.boolean(),
  sortOrder: z.number().int(),
});

export const tableListResponseSchema = z.object({
  items: z.array(tableSchema),
});

export const createTableRequestSchema = z.object({
  areaId: z.string().uuid('Selecione o ambiente da mesa'),
  label: z.string().trim().min(1, 'Informe o rótulo da mesa').max(16),
  seats: z.number().int().min(1, 'A mesa precisa ter pelo menos um assento'),
});

/** Porta de <c>CreateTablesBulkRequest</c> — cenário Gherkin "Criação em lote" ("criar mesas 1 a 20"). */
export const createTablesBulkRequestSchema = z
  .object({
    areaId: z.string().uuid('Selecione o ambiente das mesas'),
    from: z.number().int().positive('O número inicial deve ser maior que zero'),
    to: z.number().int().positive(),
    seats: z.number().int().min(1, 'A mesa precisa ter pelo menos um assento'),
  })
  .refine((value) => value.to >= value.from, {
    message: 'O número final deve ser maior ou igual ao inicial',
    path: ['to'],
  })
  .refine((value) => value.to - value.from + 1 <= 200, {
    message: 'O lote não pode ter mais que 200 mesas de uma vez',
    path: ['to'],
  });

export const tablesBulkResponseSchema = z.object({
  items: z.array(tableSchema),
});

export const updateTableRequestSchema = z.object({
  areaId: z.string().uuid('Selecione o ambiente da mesa'),
  label: z.string().trim().min(1, 'Informe o rótulo da mesa').max(16),
  seats: z.number().int().min(1, 'A mesa precisa ter pelo menos um assento'),
  sortOrder: z.number().int().default(0),
});

export type TableStatusDto = z.infer<typeof tableStatusSchema>;
export type TableDto = z.infer<typeof tableSchema>;
export type TableListResponse = z.infer<typeof tableListResponseSchema>;
export type CreateTableRequest = z.infer<typeof createTableRequestSchema>;
export type CreateTablesBulkRequest = z.infer<typeof createTablesBulkRequestSchema>;
export type TablesBulkResponse = z.infer<typeof tablesBulkResponseSchema>;
export type UpdateTableRequest = z.infer<typeof updateTableRequestSchema>;
