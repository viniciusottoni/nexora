import { z } from 'zod';

/**
 * Catálogo fechado dos códigos de erro estáveis (ADR-021) — mantenha em sincronia com
 * `Nexora.Shared.Errors.ApiErrorCodes.*.cs` no backend (.NET). Este arquivo é a fonte da verdade
 * do CONTRATO (o backend é que precisa bater com isto, não o contrário — ver relatório da tarefa
 * de plumbing que alinhou `ResultExtensions`), mas a lista precisa continuar completa: um código
 * real emitido pela API e ausente aqui faz `problemDetailsSchema.parse` lançar em runtime.
 */
export const errorCodeSchema = z.enum([
  'VALIDATION_FAILED',
  'UNAUTHORIZED',
  'NOT_FOUND',
  'CONFLICT',
  'INTERNAL_ERROR',
  'HTTP_ERROR',

  // Idempotência — ADR-020.
  'IDEMPOTENCY_KEY_REQUIRED',
  'IDEMPOTENCY_KEY_REUSED',
  'REQUEST_IN_PROGRESS',

  // Multi-tenant — ADR-004.
  'TENANT_CONTEXT_MISSING',
  'TENANT_INACTIVE',

  // Auth — ADR-014/ADR-023.
  'AUTH_INVALID_CREDENTIALS',
  'AUTH_TOKEN_EXPIRED',
  'AUTH_USER_BLOCKED',
  'AUTH_USER_INACTIVE',
  'DEVICE_NOT_REGISTERED',
  'PIN_LOCKED',
  'PERMISSION_DENIED',
  'AUTHORIZATION_REQUIRED',
  'AUTHORIZATION_CONTEXT_INVALID',
  'RATE_LIMIT',

  // Backup (edge -> cloud).
  'BACKUP_HASH_MISMATCH',
  'BACKUP_PERMISSION_DENIED',

  // Branding.
  'BRANDING_TENANT_NOT_FOUND',
  'BRANDING_STORAGE_UNAVAILABLE',

  // Devices/pareamento.
  'DEVICE_PAIRING_CODE_INVALID',
  'DEVICE_PAIRING_CODE_EXPIRED',
  'DEVICE_PAIRING_CODE_CONSUMED',
  'DEVICE_PAIRING_RATE_LIMIT',
  'DEVICE_NOT_FOUND',
  'DEVICE_ACTOR_REQUIRED',
  'DEVICE_STORE_CONTEXT_MISSING',

  // Installation (edge/cloud).
  'INSTALLATION_NOT_FOUND',
  'INSTALLATION_TOKEN_EXPIRED',
  'INSTALLATION_TOKEN_CONFLICT',
  'INSTALLATION_PUBLIC_KEY_PERMISSION_DENIED',
  'INSTALLATION_SIGNATURE_INVALID_CREDENTIALS',
  'INSTALLATION_BOOTSTRAP_CONFIG_MISSING',
  'INSTALLATION_BOOTSTRAP_VERSION_MISMATCH',

  // Roles.
  'ROLE_NOT_FOUND',
  'ROLE_CODE_ALREADY_EXISTS',
  'ROLE_OWNER_MUST_KEEP_FULL_ACCESS',

  // Tenants.
  'TENANT_NOT_FOUND',
  'SLUG_ALREADY_TAKEN',
  'OWNER_INVITE_INVALID_CREDENTIALS',

  // Roteiro de implantação autoatendido (US-141) — ver Nexora.Shared.Errors.ApiErrorCodes.Onboarding.cs.
  // [PENDÊNCIA] Ainda sem entrada em ResultExtensions.MapErrorCode no backend — ver relatório da
  // tarefa que introduziu estes dois códigos.
  'ONBOARDING_INCOMPLETE',
  'ONBOARDING_STEP_NOT_FOUND',
]);

export const problemDetailsSchema = z.object({
  type: z.string().url(),
  title: z.string().min(1),
  status: z.number().int().min(400).max(599),
  detail: z.string().min(1),
  instance: z.string().startsWith('/'),
  code: errorCodeSchema,
  recoverable: z.boolean(),
  requiresAuthorization: z.boolean(),
  traceId: z.string().regex(/^[0-9a-f]{32}$/),
  meta: z.record(z.unknown()).optional(),
});

export type ProblemDetails = z.infer<typeof problemDetailsSchema>;
export type ErrorCode = z.infer<typeof errorCodeSchema>;
