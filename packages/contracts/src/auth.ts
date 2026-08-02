import { z } from 'zod';

const uuid = z.string().uuid();

export const passwordLoginSchema = z.object({
  email: z
    .string()
    .email()
    .transform((value) => value.toLowerCase()),
  password: z.string().min(8).max(128),
  otp: z
    .string()
    .regex(/^\d{6}$/)
    .optional(),
});

export const pinLoginSchema = z.object({
  pin: z.string().regex(/^\d{4,6}$/),
  deviceId: uuid,
});

export const refreshRequestSchema = z.object({ refreshToken: z.string().min(32) });

export const authorizeRequestSchema = z.object({
  action: z.string().regex(/^[A-Z][A-Z0-9_]{2,63}$/),
  pin: z.string().regex(/^\d{4,6}$/),
  context: z.record(z.unknown()),
});

export const jwtClaimsSchema = z.object({
  sub: uuid,
  tid: uuid,
  sid: uuid,
  roles: z.array(z.string()),
  perms: z.array(z.string()),
  did: uuid.optional(),
  exp: z.number().int().positive(),
});

export const authResponseSchema = z.object({
  accessToken: z.string().min(1),
  refreshToken: z.string().min(1).optional(),
  user: z.object({ id: uuid, name: z.string().min(1) }),
  permissions: z.array(z.string()),
});

export type PasswordLoginRequest = z.infer<typeof passwordLoginSchema>;
export type PinLoginRequest = z.infer<typeof pinLoginSchema>;
export type AuthorizeRequest = z.infer<typeof authorizeRequestSchema>;
export type JwtClaims = z.infer<typeof jwtClaimsSchema>;
