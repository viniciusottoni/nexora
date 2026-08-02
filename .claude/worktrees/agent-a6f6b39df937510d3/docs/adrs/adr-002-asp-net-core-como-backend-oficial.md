# ADR-002 — ASP.NET Core como backend oficial

Status: Aceito

## Contexto

O AWAKEN precisa de backend confiável para autenticação, perfis, onboarding, quests, XP, rank, streak, assinatura, webhooks e integrações.

## Decisão

Usar ASP.NET Core Web API com C# como backend oficial.

## Diretrizes de implementação

- Criar a API em `backend/src/Awaken.Api`.
- Usar .NET 10 LTS ou versão definida pelo time.
- Separar camadas Api, Application, Domain, Infrastructure, Contracts e Shared.
- Usar Entity Framework Core com PostgreSQL.
- Usar MediatR para commands e queries.
- Usar FluentValidation para validação.
- Usar Swagger/OpenAPI para documentação.

## Consequências

A solução ganha robustez, performance e testabilidade. O time deve manter separação clara entre app Flutter e backend .NET.

## Critérios de aceite

- API compila em CI.
- Controllers não contêm regra de negócio.
- Handlers concentram casos de uso.
- Regras críticas possuem testes.
