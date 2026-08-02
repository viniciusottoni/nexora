# ADR-020 — Backup e recuperação

Status: Aceito

## Contexto

O AWAKEN armazenará dados importantes de usuários, quests, progressão e assinaturas. A perda desses dados prejudica confiança e operação.

## Decisão

Definir backup e processo de recuperação antes do lançamento público.

## Implementação

- Ativar backup diário do PostgreSQL em produção.
- Definir retenção mínima de 7 a 14 dias no MVP.
- Documentar restauração em runbook.
- Testar restauração antes do go-live.
- Separar produção e staging.
- Controlar acesso ao banco e storage.

## Consequências

A operação fica mais segura e preparada para incidentes. Backup sem teste não deve ser considerado suficiente.

## Critérios de aceite

- Backup automático está ativo.
- Existe runbook.
- Restauração foi testada.
- Produção e staging são separados.
