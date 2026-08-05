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

/**
 * Preferências "por dispositivo" do KDS (EPIC-E-04) — US-042 (`stationIds`, filtro de praça),
 * US-045 (`sound`) e US-047 (`peakMode`), todas gravadas sob a MESMA sub-chave `kds` do objeto de
 * preferências (porta de `Nexora.Domain.Platform.Device.Preferences`, JSONB). Cada campo é
 * opcional porque `PATCH /v1/devices/{id}/preferences` faz mescla profunda no servidor
 * (`DevicePreferencesJsonMerger`) — o cliente só envia a sub-chave que está mudando.
 */
export const kdsDeviceSoundPreferencesSchema = z.object({
  enabled: z.boolean(),
  volume: z.number().min(0).max(1),
  newOrderTone: z.enum(['CHIME', 'ALERT']).optional(),
  lateTone: z.enum(['CHIME', 'ALERT']).optional(),
  lateRepeatSeconds: z.number().int().positive().optional(),
});

export const kdsDevicePeakModeSchema = z.object({
  auto: z.boolean(),
  thresholdItems: z.number().int().positive(),
  hysteresisItems: z.number().int().nonnegative(),
  manuallyDisabled: z.boolean().optional(),
});

export const kdsDevicePreferencesSchema = z.object({
  stationIds: z.array(z.string().uuid()).optional(),
  layout: z.enum(['GRID', 'LIST']).optional(),
  sound: kdsDeviceSoundPreferencesSchema.partial().optional(),
  peakMode: kdsDevicePeakModeSchema.partial().optional(),
});

export const devicePreferencesPatchSchema = z.object({
  kds: kdsDevicePreferencesSchema.partial().optional(),
});

export const devicePreferencesResponseSchema = z.object({
  deviceId: z.string().uuid(),
  preferences: z.object({
    kds: kdsDevicePreferencesSchema.partial().optional(),
  }),
});

export type KdsDeviceSoundPreferences = z.infer<typeof kdsDeviceSoundPreferencesSchema>;
export type KdsDevicePeakModePreferences = z.infer<typeof kdsDevicePeakModeSchema>;
export type KdsDevicePreferences = z.infer<typeof kdsDevicePreferencesSchema>;
export type DevicePreferencesPatch = z.infer<typeof devicePreferencesPatchSchema>;
export type DevicePreferencesResponse = z.infer<typeof devicePreferencesResponseSchema>;

export type DeviceKindDto = z.infer<typeof deviceKindSchema>;
export type PairDeviceRequest = z.infer<typeof pairDeviceRequestSchema>;
export type PairDeviceResponse = z.infer<typeof pairDeviceResponseSchema>;
export type DeviceDto = z.infer<typeof deviceSchema>;
export type DeviceListResponse = z.infer<typeof deviceListResponseSchema>;
export type RenameDeviceRequest = z.infer<typeof renameDeviceRequestSchema>;
