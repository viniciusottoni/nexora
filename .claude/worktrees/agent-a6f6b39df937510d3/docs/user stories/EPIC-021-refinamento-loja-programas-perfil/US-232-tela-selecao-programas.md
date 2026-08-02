---
title: US-232 — Selecionar programa em tela com detalhes
sidebar_position: 232
---

# US-232 — Selecionar programa em tela com detalhes

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-232 |
| Épico | EPIC-021 — Refinamento de Loja, Programas e Perfil |
| Prioridade | P0 |
| Fase | Refinamento funcional pós-fundação de economia |
| Perfil principal | Usuário em Trial ou assinante |
| Dependências | US-231, EPIC-007 |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**, quero **selecionar um programa em uma tela com detalhes**, para **entender como meus treinos serão divididos antes de confirmar**.

## 3. Contexto

A tela de programas deve seguir o padrão visual dark/RPG do AWAKEN, com cards expansíveis, badges de frequência, rank/categoria e CTA claro.

## 4. Objetivo

Criar tela mobile de seleção de programa com detalhes, bloqueio por rank e confirmação explícita.

## 5. Escopo

### Entra nesta US

- Tela de Programas com botão voltar.
- Lista de programas em cards.
- Cards expansíveis com detalhes.
- Badge de status ativo, rank mínimo, frequência e categoria.
- Descrição da divisão do treino.
- CTA “Selecionar programa”.
- CTA final “Confirmar programa”.
- Estado bloqueado quando rank não atende.

### Fora desta US

- Criação livre de programa pelo usuário.
- Editor avançado de divisão.
- Programas sociais/comunitários.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário deve ver detalhes antes de confirmar o programa. |
| RN-002 | Programa bloqueado por rank não pode ter CTA ativo. |
| RN-003 | O programa atual deve aparecer destacado como ativo. |
| RN-004 | Confirmar programa deve chamar backend. |
| RN-005 | A seleção só vale após confirmação bem-sucedida. |
| RN-006 | Tela deve funcionar em PT-BR, EN e ES. |

## 7. Fluxo principal

1. Usuário acessa Programas.
2. App carrega catálogo com disponibilidade por rank.
3. Usuário expande um programa.
4. App mostra descrição, divisão e requisitos.
5. Usuário toca em selecionar.
6. CTA final fica habilitado.
7. Usuário confirma.
8. Backend salva seleção.

## 8. Estados de tela

- Loading.
- Lista carregada.
- Programa expandido.
- Programa bloqueado.
- Programa selecionado pendente de confirmação.
- Salvando.
- Erro.
- Sucesso.

## 9. Impacto Flutter

- Nova tela `TrainingProgramsPage` ou equivalente.
- Cards expansíveis com design AWAKEN.
- Badges de rank/categoria/frequência.
- Estado de seleção temporária.
- Feedback de sucesso/erro.

## 10. Impacto Backend

- Endpoint para salvar programa selecionado.
- Validação de rank e acesso.
- Retornar erro funcional quando bloqueado.

## 11. Contratos API sugeridos

```txt
GET /api/training-programs
PUT /api/users/me/training-program
```

Request conceitual:

```json
{
  "programKey": "ab"
}
```

## 12. Critérios de aceite

### CA-001 — Ver detalhes

Dado que o usuário abre um card de programa,
quando expandir o card,
então deve ver descrição, divisão, categoria e rank mínimo.

### CA-002 — Confirmar programa

Dado que o usuário selecionou AB,
quando tocar em confirmar,
então o backend deve salvar AB como programa escolhido.

### CA-003 — Programa bloqueado

Dado que o programa exige rank maior,
quando o usuário tentar selecionar,
então o CTA deve permanecer bloqueado ou retornar erro funcional.

## 13. Decisão registrada

A seleção de programas deve ser clara e visual: o usuário entende a divisão antes de confirmar, e o backend valida se ele pode usar o programa.
