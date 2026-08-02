import { z } from 'zod';

export const deviceKindSchema = z.enum(['CASHIER', 'KDS', 'WAITER', 'SUPPORT_TABLET']);

export const createPairingCodeResponseSchema = z.object({
  code: z.string().regex(/^\d{6}$/),
  expiresAt: z.string().datetime({ offset: true }),
  expiresInSeconds: z.literal(600),
});

export const pairingCodeResponseSchema = createPairingCodeResponseSchema;

export const pairDeviceRequestSchema = z.object({
  code: z.string().regex(/^\d{6}$/, 'Código deve ter 6 dígitos'),
  label: z.string().trim().min(1, 'Informe um nome para o dispositivo').max(100),
  kind: deviceKindSchema,
  fingerprint: z.string().trim().min(1).max(512),
});

export const pairDeviceResponseSchema = z.object({
  device: z.object({
    id: z.string().uuid(),
    label: z.string().min(1),
  }),
  deviceSecret: z.string().min(1),
});

export const deviceSchema = z.object({
  id: z.string().uuid(),
  label: z.string().min(1),
  kind: deviceKindSchema,
  active: z.boolean(),
  lastSeenAt: z.string().datetime({ offset: true }).nullable(),
  needsReview: z.boolean(),
});

export const deviceListResponseSchema = z.object({
  items: z.array(deviceSchema),
});

export const renameDeviceRequestSchema = z.object({
  label: z.string().trim().min(1, 'Informe um nome para o dispositivo').max(100),
});

export type DeviceKindDto = z.infer<typeof deviceKindSchema>;
export type PairDeviceRequest = z.infer<typeof pairDeviceRequestSchema>;
export type PairDeviceResponse = z.infer<typeof pairDeviceResponseSchema>;
export type DeviceDto = z.infer<typeof deviceSchema>;
export type DeviceListResponse = z.infer<typeof deviceListResponseSchema>;
export type RenameDeviceRequest = z.infer<typeof renameDeviceRequestSchema>;
