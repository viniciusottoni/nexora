/**
 * Extrai o `qr_token` da URL que o navegador abriu ao ler o QR Code (US-021 §4). Aceita dois
 * formatos: caminho `/t/{token}` (preferido — mais curto, melhor para o próprio QR) e query
 * `?t={token}` (fallback, útil para link compartilhado manualmente).
 *
 * [GAP CONHECIDO, fora do escopo desta tarefa] `Nexora.Infrastructure.Operation.TableQrCodesPdfRenderer`
 * (US-020, já implementado) hoje codifica no QR Code o `qr_token` CRU, não uma URL completa — a
 * docstring de `TableQrCodePrintItem.QrPayload` confirma isso ("o qr_token opaco da mesa"). Isso
 * significa que a câmera do celular, ao ler o QR impresso hoje, NÃO abre o navegador sozinha (a
 * maioria dos leitores de QR do sistema operacional só oferece "abrir no navegador" quando o
 * payload já é uma URL) — o requisito "zero fricção... abre no navegador" da US-021 §10 só é
 * cumprido de ponta a ponta quando o PDF passar a codificar
 * `https://<host-do-edge-ou-dominio-publico>/t/{qrToken}`. Consertar a geração do PDF é tocar
 * código da US-020 (fora do pedido desta wave, que é US-021/US-022) — reportado no relatório
 * final como próximo passo. Esta função já assume o formato de URL correto, para o dia em que o
 * PDF for corrigido não exigir nenhuma mudança aqui.
 */
export function extractQrTokenFromLocation(location: Pick<Location, 'pathname' | 'search'>): string | null {
  const pathMatch = /^\/t\/([^/]+)\/?$/.exec(location.pathname);
  if (pathMatch?.[1]) {
    return decodeURIComponent(pathMatch[1]);
  }

  const query = new URLSearchParams(location.search);
  const fromQuery = query.get('t');
  return fromQuery && fromQuery.length > 0 ? fromQuery : null;
}
