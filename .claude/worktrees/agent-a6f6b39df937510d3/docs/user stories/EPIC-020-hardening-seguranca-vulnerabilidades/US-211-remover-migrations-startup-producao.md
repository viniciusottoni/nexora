---
title: US-211 — Remover migrations automáticas do startup em produção
sidebar_position: 211
---

# US-211 — Remover migrations automáticas do startup em produção

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-211 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Backend, DevOps e banco de dados |
| Plano | Todos |
| Dependência principal | EF Core migrations, deploy, PostgreSQL |
| Status | Planejada |

## 2. História do usuário

Como **responsável pelo deploy do AWAKEN**,

quero **que migrações de banco sejam executadas de forma controlada antes da API subir**,

para **evitar locks, corrida entre réplicas e startup lento em produção**.

## 3. Contexto

Executar migration automaticamente no startup é prático em desenvolvimento, mas em produção com múltiplas réplicas pode gerar disputa, lentidão ou falha de deploy. O MVP deve separar aplicação de schema de banco do boot normal da API.

## 4. Objetivo

Remover migration automática da API em produção e criar etapa controlada de migration no pipeline ou job operacional.

## 5. Escopo

### Entra nesta US

- Permitir migration automática apenas em Development, se desejado.
- Criar comando/job de migration controlado para produção.
- Garantir que apenas uma instância execute migration por deploy.
- Documentar rollback e backup antes de migration sensível.
- Criar health/readiness que valide schema esperado.

### Fora desta US

- Ferramenta externa obrigatória de migration.
- Estratégia avançada blue/green de banco.
- Rollback automático de dados.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | API de produção não deve aplicar migration automaticamente em todo startup. |
| RN-002 | Migration deve rodar uma vez por deploy em etapa controlada. |
| RN-003 | Falha de migration deve bloquear rollout. |
| RN-004 | Migration sensível deve ter backup ou plano de reversão. |
| RN-005 | API deve validar compatibilidade mínima de schema. |

## 7. Fluxo principal

1. Pipeline inicia deploy.
2. Etapa de migration roda em job único.
3. Se migration falhar, deploy é interrompido.
4. Se migration passar, API e Worker são atualizados.
5. Health/readiness confirma operação.

## 8. Impacto no Backend

- Ajustar `Program.cs` para não aplicar migration em produção automaticamente.
- Criar comando de migration executável via pipeline.
- Adicionar validação de ambiente.

## 9. Impacto no DevOps

- Pipeline passa a ter etapa de migration.
- Deploy precisa de variável/flag explícita para rodar migration.
- Logs de migration devem ser guardados.

## 10. Impacto no Banco

- Reduz risco de lock no startup.
- Permite planejamento de migration pesada.

## 11. Impacto no Flutter

Sem impacto direto.

## 12. Critérios de aceite

- API de produção não executa migration automaticamente em toda inicialização.
- Development pode manter fluxo simplificado.
- Pipeline/job executa migration de forma controlada.
- Falha de migration impede rollout.
- Documentação de deploy está atualizada.

## 13. Critérios de teste para QA

- startup Development;
- startup Production sem migration automática;
- job de migration com sucesso;
- job de migration com falha;
- múltiplas réplicas subindo sem corrida de migration.

## ✅ Decisão registrada

Migração de banco em produção deve ser etapa controlada do deploy, não efeito colateral do startup de cada réplica da API.