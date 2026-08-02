# ADR-003 — Modular Monolith no MVP

Status: Aceito

## Contexto

O AWAKEN terá vários domínios: autenticação, onboarding, perfil físico, exercícios, treinos, quests, progressão, nutrição, assinaturas, notificações e analytics. Apesar disso, o MVP não precisa da complexidade operacional de microsserviços.

## Decisão

Usar um Modular Monolith no backend durante o MVP.

## Diretrizes de implementação

- Manter um único deploy principal da API.
- Separar módulos por namespace e pasta dentro da camada Application e Domain.
- Evitar dependências circulares entre módulos.
- Manter contratos públicos em `Awaken.Contracts`.
- Manter integrações externas dentro de Infrastructure.
- Criar boundaries claros para Auth, Profiles, Onboarding, Exercises, Workouts, Quests, Progression, Nutrition, Subscriptions e Notifications.
- Não compartilhar entidades diretamente entre módulos sem necessidade.

## Consequências

O time reduz custo operacional, acelera desenvolvimento e mantém um caminho simples para escalar. Caso um módulo cresça muito, ele poderá ser extraído futuramente para serviço próprio, mas somente depois de validação real de uso.

## Critérios de aceite

- Cada módulo possui commands, queries, validators e handlers próprios.
- Não existe regra de negócio em controller.
- Infrastructure implementa interfaces da Application.
- Testes de arquitetura impedem dependências indevidas.
