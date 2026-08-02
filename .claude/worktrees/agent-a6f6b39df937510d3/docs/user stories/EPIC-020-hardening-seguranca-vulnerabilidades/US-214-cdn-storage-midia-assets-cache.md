---
title: US-214 — Servir mídia e assets por CDN/storage com cache
sidebar_position: 214
---

# US-214 — Servir mídia e assets por CDN/storage com cache

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-214 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P1 |
| Fase | Antes de catálogo com mídia pesada no MVP |
| Perfil principal | Usuário mobile, catálogo de exercícios, DevOps e storage |
| Plano | Todos |
| Dependência principal | Cloudflare R2/S3, CDN, ExerciseCatalog, Flutter mídia |
| Status | Planejada |

## 2. História do usuário

Como **usuário que visualiza exercícios no app**,

quero **que imagens, GIFs e vídeos carreguem rápido sem pesar na API**,

para **entender o movimento com fluidez durante o treino**.

## 3. Contexto

O catálogo possui URLs de imagem, GIF e vídeo. A API não deve servir esses arquivos diretamente. Mídia deve ficar em storage/CDN com cache adequado, e o app deve consumir diretamente as URLs otimizadas.

## 4. Objetivo

Garantir que mídia e assets estáticos do catálogo sejam servidos por storage/CDN, com cache e formatos adequados para mobile.

## 5. Escopo

### Entra nesta US

- Definir storage/CDN oficial para mídia do catálogo.
- Padronizar URLs públicas ou assinadas conforme necessidade.
- Configurar cache headers adequados.
- Gerar versões leves quando necessário: thumbnail, preview e mídia principal.
- Garantir que API retorne apenas metadados/URLs.
- Ajustar app para cache local de mídia quando aplicável.

### Fora desta US

- Streaming avançado.
- Edição automática de vídeo.
- CDN multi-região complexa.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | API principal não deve servir binários pesados de exercício. |
| RN-002 | Mídia pública e imutável deve usar cache longo. |
| RN-003 | Troca de mídia deve alterar nome/chave ou versão para evitar cache velho. |
| RN-004 | App deve lidar com mídia ausente usando fallback visual. |
| RN-005 | URLs não devem expor credenciais de storage. |

## 7. Tipos de asset

- thumbnail leve;
- imagem principal;
- GIF ou animação curta;
- vídeo instrucional, quando existir;
- placeholder/fallback.

## 8. Fluxo principal

1. Catálogo armazena metadados e URLs de mídia.
2. App recebe URLs pela API.
3. App baixa mídia diretamente do CDN/storage.
4. CDN responde com cache adequado.
5. App usa fallback se mídia falhar.

## 9. Impacto no Backend

- API retorna apenas URLs/metadados.
- Importação/admin precisa validar URL e tamanho esperado.
- Catálogo deve suportar versionamento de asset.

## 10. Impacto no Flutter

- Usar cache local de imagem/mídia quando possível.
- Exibir placeholder e retry controlado.
- Evitar baixar mídia pesada automaticamente fora da tela.

## 11. Impacto no DevOps

- Configurar bucket/storage e CDN.
- Configurar cache headers.
- Definir política de versionamento e invalidação.

## 12. Critérios de aceite

- API não trafega binários pesados de mídia.
- Exercícios usam URLs de CDN/storage.
- Mídia imutável usa cache longo.
- App exibe fallback se mídia falhar.
- URLs não expõem credenciais.
- Documentação de upload/versionamento existe.

## 13. Critérios de teste para QA

- exercício com imagem;
- exercício com GIF;
- exercício sem mídia;
- URL inválida;
- cache funcionando;
- app em rede lenta;
- troca de versão de asset.

## ✅ Decisão registrada

Mídia de exercício e assets estáticos devem sair da API principal e ser servidos por storage/CDN com cache adequado para mobile.