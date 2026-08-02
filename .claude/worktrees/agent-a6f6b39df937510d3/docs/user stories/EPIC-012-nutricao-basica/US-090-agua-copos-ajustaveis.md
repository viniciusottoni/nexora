---
title: US-090 — Visualizar água em copos ajustáveis
sidebar_position: 90
---

# US-090 — Visualizar água em copos ajustáveis

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-090 |
| Épico | EPIC-012 — Nutrição Básica |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **visualizar minha água em copos ajustáveis**, para **registrar consumo de forma rápida usando o volume que faz sentido para mim**.

## 3. Contexto

Nem todo usuário bebe água em copos do mesmo tamanho. O AWAKEN deve permitir ajustar o volume do copo e refletir isso na quantidade exibida e registrada.

## 4. Objetivo

Permitir configurar o volume padrão do copo e usar esse volume para exibição e registro rápido de água.

## 5. Escopo

### Entra nesta US

- Exibir água em quantidade de copos.
- Permitir ajustar volume do copo.
- Atualizar quantidade exibida ao mudar volume.
- Usar volume do copo no botão de registrar água.
- Persistir preferência do volume do copo.

### Fora desta US

- Múltiplos recipientes salvos.
- Garrafa inteligente.
- Scanner ou integração externa.
- Metas nutricionais avançadas.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O volume do copo deve ser configurável pelo usuário. |
| RN-002 | Quando o volume do copo muda, a quantidade de copos exibida deve ser recalculada. |
| RN-003 | O registro rápido de água deve usar o volume do copo atual. |
| RN-004 | O volume deve ser persistido como preferência do usuário. |
| RN-005 | Valores inválidos de volume devem ser bloqueados. |
| RN-006 | A unidade base persistida deve continuar sendo ml. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode configurar. |
| Usuário em Trial | Pode configurar com trial ativo. |
| Premium Mensal | Pode configurar. |
| Premium Anual | Pode configurar. |
| Trial expirado | Não pode registrar/configurar. |
| Assinatura expirada | Não pode registrar/configurar. |

## 8. Fluxo principal

1. Usuário visualiza card de água.
2. App mostra consumo em ml/litros e em copos equivalentes.
3. Usuário altera volume do copo.
4. App recalcula a quantidade de copos exibida.
5. Próximos registros usam o novo volume.

## 9. Fluxos alternativos

### 9.1. Volume inválido

App bloqueia valor e exibe mensagem clara.

### 9.2. Preferência ausente

Sistema usa volume padrão inicial definido pelo produto.

### 9.3. Acesso expirado

App bloqueia configuração e registro.

## 10. Estados esperados

- volume padrão;
- volume personalizado;
- recalculando copos;
- valor inválido;
- acesso bloqueado;
- erro de salvamento.

## 11. Impacto Flutter

- Controle de volume do copo.
- Indicador visual de copos.
- Atualização reativa da quantidade exibida.
- Botão rápido usando volume atual.

## 12. Impacto Backend

- Persistir preferência do volume do copo.
- Retornar volume atual na consulta de nutrição.
- Registrar água com base no volume informado.
- Validar acesso e valores.

## 13. Impacto DB

Entidades:

- UserNutritionPreference;
- NutritionLog.

Campos sugeridos:

- userId;
- cupVolumeMl;
- waterMl;
- date.

## 14. Impacto Gamificação

- Reduz fricção e aumenta chance de registro diário.
- Não concede XP no MVP.

## 15. Impacto Monetização

- Recurso P1 disponível para acesso ativo.

## 16. Contrato API sugerido

```txt
PATCH /api/nutrition/preferences/cup-volume
```

Request conceitual:

```json
{
  "cupVolumeMl": 300
}
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| water_cup_volume_changed | Quando usuário altera volume do copo. |

## 18. Critérios de aceite

### CA-001 — Recalcular copos

Dado que o usuário consumiu 1000 ml,
Quando alterar o copo de 250 ml para 500 ml,
Então a interface deve mudar de 4 copos para 2 copos.

### CA-002 — Registrar com volume atual

Dado que o copo atual tem 300 ml,
Quando o usuário tocar em adicionar água,
Então o consumo do dia deve aumentar em 300 ml.

## 19. Critérios de teste QA

- volume padrão;
- alterar para 300 ml;
- recalcular quantidade de copos;
- registrar água com volume atual;
- bloquear volume inválido;
- acesso expirado;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

A visualização em copos ajustáveis deve facilitar o registro de água sem alterar a unidade base: tudo continua persistido em ml.
