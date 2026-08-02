# ADR-008 — Cloudflare R2 para storage

Status: Substituído por [ADR-024](adr-024-storage-s3-compativel-bucket-railway.md)

> O MVP passou a usar o bucket interno do Railway (S3-compatível) em vez de Cloudflare R2. O código de
> storage foi generalizado para qualquer provedor S3-compatível — ver ADR-024. Este documento é mantido
> para histórico da decisão original.

## Contexto

O AWAKEN precisará armazenar avatares, imagens opcionais, cards gerados, assets controlados e possíveis mídias futuras. Esses arquivos não devem ficar no banco relacional e precisam de storage barato, escalável e simples para o MVP.

## Decisão

Usar Cloudflare R2 como storage principal de objetos.

## Implementação

- Criar bucket separado por ambiente.
- Salvar arquivos com chave opaca, sem depender do nome original.
- Armazenar metadados no PostgreSQL.
- Servir arquivos públicos apenas quando forem realmente públicos.
- Para arquivos privados, gerar URL temporária pelo backend.
- Usar CDN Cloudflare quando fizer sentido.
- Definir política de tamanho máximo e tipo permitido.

## Consequências

O custo inicial é baixo e a integração com CDN é simples. A equipe deve evitar expor dados pessoais de forma pública e garantir que o backend controle acesso a arquivos privados.

## Critérios de aceite

- Upload de avatar funciona.
- Card gerado pode ser armazenado.
- URLs temporárias expiram.
- Chaves de storage não ficam expostas no app.
