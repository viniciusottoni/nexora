---
title: US-222 — Monitorar mídia, CDN e assets do catálogo
sidebar_position: 222
---

# US-222 — Monitorar mídia, CDN e assets do catálogo

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-222 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-214 |
| Prioridade | P1 |
| Fase | Antes de catálogo com mídia pesada no MVP |
| Perfil principal | Admin, Produto, Conteúdo, DevOps e Suporte |
| Plataforma | Web Admin React + Backend Admin API + Storage/CDN |
| Status | Planejada |

## 2. História do usuário

Como **admin do AWAKEN**,

quero **visualizar a saúde de imagens, GIFs, vídeos e assets do catálogo**,

para **prevenir exercícios sem mídia, links quebrados, carregamento lento e custo excessivo na API**.

## 3. Contexto

A US-214 define que mídia de exercícios deve ser servida por storage/CDN, não pela API principal. O Admin precisa diagnosticar se o catálogo está com mídia válida, otimizada e disponível.

## 4. Objetivo

Criar visualização operacional para mídia/CDN do catálogo de exercícios e assets estáticos.

## 5. Escopo

### Entra nesta US

- Cards de cobertura de mídia do catálogo.
- Lista de exercícios sem imagem, sem GIF ou com URL inválida.
- Status de disponibilidade por asset.
- Tempo médio de carregamento quando disponível.
- Sinal de cache/CDN ativo ou sem dados.
- Filtro por ambiente, dificuldade, equipamento, região muscular e status de mídia.
- Link para detalhe do exercício.

### Fora desta US

- Upload completo de mídia no MVP, se não existir.
- Edição automática de vídeo.
- CDN multi-região avançado.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Exercício aprovado para geração deve ter mídia mínima ou fallback definido. |
| RN-002 | Link inválido deve aparecer como problema operacional. |
| RN-003 | Asset pesado ou lento deve aparecer como atenção. |
| RN-004 | API não deve servir o arquivo pesado diretamente. |
| RN-005 | Tela não deve expor dados de acesso ao storage. |

## 7. Indicadores mínimos

- Percentual de exercícios com imagem.
- Percentual de exercícios com GIF/vídeo.
- Quantidade de links inválidos.
- Quantidade de assets sem cache detectado.
- Tempo médio de carregamento.
- Top exercícios com problema de mídia.

## 8. Fluxo principal

1. Admin acessa Mídia/CDN.
2. Sistema exibe cobertura geral.
3. Admin filtra exercícios com problema.
4. Admin abre detalhe do exercício.
5. Admin aciona correção por processo operacional externo ou futura tela de catálogo.

## 9. Impacto no Frontend

- Nova página ou aba `Mídia/CDN`.
- Cards de cobertura e tabela de problemas.
- Preview seguro de mídia quando permitido.

## 10. Impacto no Backend

- Endpoint admin para diagnóstico de mídia.
- Verificação periódica ou sob demanda de URL/status.
- Retorno de dados agregados e seguros.

## 11. Critérios de aceite

- Admin vê cobertura de mídia do catálogo.
- Exercícios com link inválido aparecem em lista.
- Exercícios sem mídia mínima aparecem em destaque.
- API não entrega binários pesados pelo Admin.
- Nenhum dado sensível de storage é exposto.

## 12. Critérios de teste para QA

- exercício com imagem válida;
- exercício sem imagem;
- URL inválida;
- asset lento;
- filtro por status;
- preview/fallback visual.

## ✅ Decisão registrada

O Admin deve diagnosticar mídia e CDN do catálogo para evitar experiência quebrada no treino e proteger a API de tráfego pesado.