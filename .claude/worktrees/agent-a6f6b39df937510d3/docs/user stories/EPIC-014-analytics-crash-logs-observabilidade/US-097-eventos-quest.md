---
title: US-097 — Rastrear geração, início e conclusão de quest
sidebar_position: 97
---

# US-097 — Rastrear geração, início e conclusão de quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-097 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Engenharia e QA |
| Integrações | Firebase Analytics e logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear geração, início e conclusão de quests**, para **medir se os treinos gerados realmente viram execução e conclusão**.

## 3. Contexto

A quest é o núcleo do AWAKEN. O MVP precisa medir se usuários geram, iniciam, abandonam ou concluem quests diárias, dungeons e raids quando existirem.

## 4. Objetivo

Registrar eventos críticos do ciclo de vida da quest com `quest_type`, status e resultado mínimo.

## 5. Escopo

### Entra nesta US

- Quest diária gerada.
- Dungeon gerada.
- Quest iniciada.
- Quest concluída.
- Falha na geração, início ou conclusão.
- Propriedade `quest_type`: daily, dungeon ou raid.

### Fora desta US

- Funil avançado em tempo real.
- BI externo.
- Dados detalhados de saúde.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Eventos de quest devem informar `quest_type`. |
| RN-002 | Fluxos P0 devem ter evento de início, sucesso e falha quando aplicável. |
| RN-003 | Eventos não devem enviar lista completa de limitações físicas. |
| RN-004 | Conclusão duplicada não deve gerar evento duplicado de recompensa. |
| RN-005 | Falhas devem usar códigos funcionais padronizados. |

## 7. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Quando quest diária é gerada. |
| dungeon_generated | Quando dungeon é gerada. |
| dungeon_viewed | Quando dungeon é visualizada. |
| quest_started | Quando quest inicia. |
| quest_completed | Quando quest conclui. |
| quest_generation_failed | Quando geração falha. |
| quest_completion_failed | Quando conclusão falha. |

## 8. Payload mínimo

```json
{
  "quest_type": "daily",
  "quest_id_hash": "hash",
  "source": "home",
  "result": "success"
}
```

## 9. Impacto Flutter

- Disparar eventos em geração, início e conclusão.
- Garantir deep links e retomadas sem duplicar eventos.
- Usar serviço central de tracking.

## 10. Impacto Backend

- Registrar logs de geração e conclusão.
- Incluir correlationId em erros de API.
- Evitar duplicidade em conclusões idempotentes.

## 11. Impacto QA

- Validar daily, dungeon e raid quando houver.
- Validar evento de falha.
- Validar ausência de dados sensíveis.
- Validar que conclusão duplicada não duplica evento final.

## 12. Critérios de aceite

### CA-001 — Quest concluída rastreada

Dado que uma quest foi concluída,
Quando o backend confirmar sucesso,
Então o evento `quest_completed` deve ser enviado com `quest_type`.

### CA-002 — Falha rastreada

Dado que a geração falhou,
Quando o erro ocorrer,
Então deve existir evento de falha com código padronizado.

## 13. Decisão registrada

O ciclo da quest deve ser rastreado de ponta a ponta para medir ativação, engajamento e conclusão real do treino.
