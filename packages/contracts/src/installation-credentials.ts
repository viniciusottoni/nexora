import { z } from 'zod';

/**
 * US-156 · Recuperação do provisionamento e token de instalação — portas de
 * `GET /v1/platform/tenants/{tenantId}/deployment`,
 * `POST /v1/platform/installations/{installationId}/tokens` e
 * `DELETE /v1/platform/installations/{installationId}/tokens/{credentialId}`.
 *
 * ATENÇÃO — arquivo NÃO conectado ao barril (`packages/contracts/src/index.ts` está fora do
 * escopo desta tarefa, ver disciplina de isolamento de arquivos do relatório da US-156): nenhum
 * outro arquivo deste pacote reexporta os schemas abaixo ainda, e `@nexora/contracts` só publica
 * (`package.json` `exports`) o barril `dist/index.js` — um import profundo
 * (`@nexora/contracts/installation-credentials`) NÃO resolve em runtime. Por isso
 * `apps/web-platform/src/features/tenants/installation-credentials-api.ts` define seus PRÓPRIOS
 * tipos locais em vez de importar daqui (mesmo padrão de isolamento que o resto da US-156 segue —
 * "componente novo autocontido"). Este arquivo existe pronto para quando a integração central
 * acrescentar `export * from './installation-credentials.js';` a `index.ts` — nesse momento,
 * `installation-credentials-api.ts` pode trocar seus tipos locais por estes sem mudar nenhum
 * comportamento observável (os shapes já são idênticos, campo a campo).
 */
export const tenantDeploymentInstallationStatusSchema = z.enum(['PENDING', 'ACTIVE', 'OFFLINE']);

export const tenantDeploymentInstallationSchema = z.object({
  id: z.string().uuid(),
  status: tenantDeploymentInstallationStatusSchema,
  canReissueToken: z.boolean(),
});

export const tenantDeploymentStatusResponseSchema = z.object({
  completed: z.number().int().nonnegative(),
  total: z.number().int().positive(),
  installation: tenantDeploymentInstallationSchema.nullable(),
  nextAction: z.string().nullable(),
});

export type TenantDeploymentInstallationStatus = z.infer<
  typeof tenantDeploymentInstallationStatusSchema
>;
export type TenantDeploymentInstallation = z.infer<typeof tenantDeploymentInstallationSchema>;
export type TenantDeploymentStatusResponse = z.infer<typeof tenantDeploymentStatusResponseSchema>;

export const reissueInstallationTokenRequestSchema = z.object({
  reason: z.string().min(1),
  expiresInHours: z.number().int().min(1).max(72),
});

/**
 * `installToken`/`installCommand` nulos representam a REPETIÇÃO idempotente (ADR-020 + decisão da
 * US-156 §"Repetição idempotente" — ver `IdempotencyRedactFieldsAttribute` no backend): a resposta
 * ARMAZENADA para reenvio da mesma `Idempotency-Key` tem esses dois campos trocados por `null`
 * antes de ser gravada, então uma repetição verdadeira nunca torna a expor o segredo bruto — a
 * intenção foi atendida (a rotação aconteceu), mas o valor, por definição de exibição única, não
 * volta a aparecer.
 */
export const reissueInstallationTokenResponseSchema = z.object({
  credentialId: z.string().uuid(),
  expiresAt: z.string().datetime({ offset: true }),
  installToken: z.string().nullable(),
  installCommand: z.string().nullable(),
});

export type ReissueInstallationTokenRequest = z.infer<typeof reissueInstallationTokenRequestSchema>;
export type ReissueInstallationTokenResponse = z.infer<
  typeof reissueInstallationTokenResponseSchema
>;

export const revokeInstallationCredentialRequestSchema = z.object({
  reason: z.string().min(1),
});

export type RevokeInstallationCredentialRequest = z.infer<
  typeof revokeInstallationCredentialRequestSchema
>;
