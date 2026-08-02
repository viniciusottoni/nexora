# ADR-019 — Estratégia de observabilidade

Status: Aceito

## Contexto

O AWAKEN precisa acompanhar estabilidade, performance, falhas de geração de treino, erros de assinatura, falhas de push e comportamento de uso. Sem observabilidade, será difícil evoluir retenção e corrigir bugs críticos.

## Decisão

Implementar observabilidade mínima desde o MVP.

## Implementação

- Usar Crashlytics no app Flutter.
- Usar Firebase Analytics para eventos de produto.
- Usar Serilog no backend.
- Usar correlation id nas requisições.
- Registrar tempo de resposta dos endpoints.
- Monitorar falhas em integrações externas.
- Criar health checks para API, banco e cache.
- Configurar alertas básicos de indisponibilidade.

## Consequências

O time consegue agir rápido em crashes, bugs e gargalos. A equipe deve evitar excesso de eventos e manter nomes padronizados.

## Critérios de aceite

- Crash do app aparece no Crashlytics.
- Erro de API possui correlation id.
- Health check responde em produção.
- Eventos principais de produto são enviados.
