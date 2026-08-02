# ADR-024 — Storage de objetos: bucket S3-compatível (Railway)

Status: Aceito

Substitui: [ADR-008](adr-008-cloudflare-r2-para-storage.md)

## Contexto

A ADR-008 definiu Cloudflare R2 como storage de objetos do MVP. Na prática, a infraestrutura do MVP roda no Railway, que já provê um bucket interno compatível com a API S3. Manter uma segunda conta/serviço (Cloudflare R2) só para storage não agrega valor nesta fase e adiciona um provedor a mais para configurar e pagar.

## Decisão

Usar o bucket interno do Railway (compatível com S3) como storage principal de objetos, através do mesmo cliente `IAmazonS3` já usado no projeto. A integração deixa de ser específica de um provedor: o backend passa a apontar para qualquer endpoint S3-compatível via configuração, sem assumir o padrão de URL do Cloudflare R2 (`{accountId}.r2.cloudflarestorage.com`).

## Implementação

- Configuração via seção genérica `Storage` (env vars `Storage__Endpoint`, `Storage__AccessKey`, `Storage__SecretKey`, `Storage__Bucket`, `Storage__PublicBaseUrl`), em vez de `Cloudflare__R2*`.
- `Storage__Endpoint` é a URL completa do endpoint S3 fornecida pelo provedor (não um account id a partir do qual uma URL é montada).
- Classes renomeadas de `CloudflareR2MediaStorageService` / `CloudflareR2MediaRedirectService` para `S3MediaStorageService` / `S3MediaRedirectService`, refletindo que a implementação não é mais amarrada a um provedor específico.
- Continua valendo o restante da ADR-008: chave opaca por arquivo, metadados no PostgreSQL, URLs temporárias (presigned) para arquivos privados via backend.
- Trocar de provedor S3-compatível no futuro (voltar para R2, usar S3 da AWS, MinIO, etc.) deve exigir apenas mudança de configuração, não de código.

## Consequências

Um provedor a menos para gerenciar durante o MVP. Se o volume de mídia crescer ao ponto de o bucket do Railway ficar caro ou limitado (egress, CDN, limites de storage), a saída é só trocar as variáveis de `Storage`, já que o código não depende mais de nenhum detalhe específico do R2.

## Critérios de aceite

- Upload de avatar funciona apontando para o bucket do Railway.
- Card gerado pode ser armazenado.
- URLs temporárias (presigned) expiram.
- Chaves de storage não ficam expostas no app.
- Nenhuma referência a `Cloudflare__R2*` ou ao padrão de URL do R2 permanece no código de produção.
