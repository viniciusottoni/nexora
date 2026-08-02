# ADR-004 — PostgreSQL como banco principal

Status: Aceito

## Contexto

O AWAKEN precisa de um banco relacional confiável para dados do usuário, perfil, onboarding, exercícios, treinos, quests, XP, rank, streak, assinatura, notificações e auditoria.

## Decisão

Usar PostgreSQL como banco de dados principal do backend.

## Implementação

- Usar Entity Framework Core.
- Criar migrations versionadas.
- Usar UUID como identificador.
- Registrar datas em UTC.
- Criar índices para consultas frequentes.
- Usar transação em conclusão de quest.
- Preservar histórico de XP e auditoria.

## Consequências

A solução ganha consistência e baixo custo no MVP. A equipe deve manter disciplina nas migrations, constraints, índices e backups.

## Critérios de aceite

- Banco sobe localmente via Docker Compose.
- Migrations rodam no pipeline.
- Operações críticas são transacionais.
- Backups são definidos antes da produção.
