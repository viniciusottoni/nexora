# Runbook · Identidade visual em runtime

## Objetivo

Diagnosticar resolução de tenant, atualização de tema, cache offline e upload dos assets de marca. O mesmo build atende todos os estabelecimentos.

## Configuração

| Variável                           | Uso                                                       |
| ---------------------------------- | --------------------------------------------------------- |
| `PUBLIC_TENANT_BASE_DOMAIN`        | Sufixo dos hosts públicos. Ex.: `menu.plataforma.example` |
| `OBJECT_STORAGE_ENDPOINT`          | Endpoint HTTPS do storage S3 compatível                   |
| `OBJECT_STORAGE_BUCKET`            | Bucket de mídia                                           |
| `OBJECT_STORAGE_REGION`            | Região usada na assinatura SigV4                          |
| `OBJECT_STORAGE_ACCESS_KEY_ID`     | Credencial de upload                                      |
| `OBJECT_STORAGE_SECRET_ACCESS_KEY` | Segredo de upload                                         |
| `MEDIA_CDN_BASE_URL`               | Origem pública HTTPS do CDN                               |

Nunca inserir nome, slug, domínio, cor ou asset de cliente no código ou em variável de build.

## Verificações

1. Consultar `GET /v1/public/branding?host=<host>` e conferir `tenant.id`, `configVersion` e `Cache-Control: public, max-age=60, s-maxage=60`.
2. Consultar `GET /v1/tenant/branding.webmanifest?host=<host>` e conferir nome, cor e ícones.
3. Host desconhecido deve responder 404 sem listar ou sugerir tenants.
4. Em POS/KDS, desligar a rede após uma carga válida e recarregar. Tema cacheado deve continuar aplicado.
5. Limpar o cache `runtime-branding-v1` e o item `runtime-branding:<host>` somente durante diagnóstico; a aplicação deve cair no tema neutro.

## Alteração demora mais de 60 segundos

- Confirmar incremento de `tenant_config.config_version` e `branding_version`.
- Confirmar evento `tenant.branding_updated` ou `tenant.config_updated` com a mesma versão.
- Conferir relógio e política do CDN. A resposta não pode ter TTL maior que 60 segundos.
- Conferir se o navegador requisitou o host correto e não um host fornecido pelo tenant autenticado.

## Upload de mídia

`POST /v1/tenant/branding/logo` inicia upload direto e devolve URL PUT assinada por dez minutos. Caminho público contém SHA-256 do conteúdo e recebe cache imutável no CDN. O worker de mídia deve validar magic bytes, remover EXIF e gerar variantes antes de associar o asset à configuração ativa.

Formatos aceitos: SVG para logos; PNG, JPEG e WebP; máximo de 10 MB. Ícone de PWA não aceita SVG.

## Degradação segura

Falha de rede, JSON inválido ou host ainda sem marca não bloqueia POS/KDS: tema neutro permanece. Nunca reutilizar branding de outro host como fallback.
