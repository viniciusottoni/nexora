/**
 * US-150 §11/RN-004 "Ação sensível registra autor, horário e contexto" — breadcrumb do lado do
 * cliente para tentativa de acesso negado e saída de sessão por expiração. A autoridade continua
 * sendo o backend (todo 401/403 de `/v1/platform/*` já passa pelo log de requisição do
 * ASP.NET Core/Serilog — ver `Program.cs`); isto só torna a tentativa observável também do lado
 * que o time de produto olha primeiro (console do navegador → ferramenta de RUM, quando existir).
 */
export function reportAccessDenied(context: { readonly pathname: string; readonly at?: Date }): void {
  const at = context.at ?? new Date();
  console.warn('[web-platform] tentativa de acesso negado', { pathname: context.pathname, at: at.toISOString() });
}

export function reportSessionExpired(context: { readonly pathname: string; readonly at?: Date }): void {
  const at = context.at ?? new Date();
  console.warn('[web-platform] sessão expirada', { pathname: context.pathname, at: at.toISOString() });
}
