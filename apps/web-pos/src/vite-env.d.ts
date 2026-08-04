/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** US-081 §7 — origem absoluta da API cloud, só usada para `POST /v1/notifications/subscribe` (ver notifications/push-notifications.ts). */
  readonly VITE_CLOUD_API_BASE_URL?: string;
  /** Chave pública VAPID (base64url) — espelha `WebPush:PublicKeyBase64Url` do backend. */
  readonly VITE_WEB_PUSH_PUBLIC_KEY?: string;
}
