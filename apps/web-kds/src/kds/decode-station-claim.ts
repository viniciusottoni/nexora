/**
 * US-031/ADR-011 — a praça (`station:{id}`) deste terminal vem da claim `stn` do access token
 * (preenchida por `JwtTokenIssuer`/`LoginWithPinCommandHandler` a partir de `Device.StationId`,
 * backend/src/Nexora.Infrastructure/Auth/JwtTokenIssuer.cs), nunca de uma escolha do usuário na
 * tela — mesmo princípio do ADR-011 ("a inscrição é derivada dos claims do token, não solicitada
 * pelo cliente") replicado no cliente: o `KdsQueuePage` só lê a claim já validada pelo servidor
 * para saber QUAL fila pedir via `GET /v1/kds/queue?stationId=...`, sem decidir nada por conta
 * própria. Decodificação pura (sem verificar assinatura) — o token já passou pelo servidor em toda
 * chamada autenticada; isto é só leitura de UI, não uma fronteira de segurança.
 */
export function readStationIdFromAccessToken(accessToken: string): string | null {
  try {
    const segments = accessToken.split('.');
    const payloadSegment = segments[1];
    if (!payloadSegment) return null;

    const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
    const padding = (4 - (base64.length % 4)) % 4;
    const padded = base64 + '='.repeat(padding);
    const json = atob(padded);
    const payload = JSON.parse(json) as Record<string, unknown>;
    return typeof payload.stn === 'string' && payload.stn.length > 0 ? payload.stn : null;
  } catch {
    return null;
  }
}
