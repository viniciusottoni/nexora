# ADR-016 — Android mínimo

Status: Aceito

## Contexto

O AWAKEN começa pelo Android e precisa de uma base mínima estável para animações, cache local e notificações.

## Decisão

Usar Android 8.0 ou superior como mínimo prático. Recomendar Android 10 ou superior para melhor experiência.

## Implementação

- Configurar minSdk conforme a meta do projeto.
- Manter targetSdk atualizado para publicação.
- Testar em aparelho com 3 GB RAM.
- Recomendar 4 GB RAM para melhor experiência.
- Validar telas 720p e 1080p.
- Monitorar crashes por versão do sistema.

## Consequências

A equipe reduz custo de suporte a aparelhos antigos e melhora a qualidade visual do app.

## Critérios de aceite

- Build Android gera AAB.
- App roda no mínimo definido.
- QA cobre aparelho mínimo e recomendado.
