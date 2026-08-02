---
title: US-231 — Definir programas e restrições por rank
sidebar_position: 231
---

# US-231 — Definir programas e restrições por rank

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-231 |
| Épico | EPIC-021 — Refinamento de Loja, Programas e Perfil |
| Prioridade | P0 |
| Fase | Refinamento funcional pós-fundação de economia |
| Perfil principal | Usuário em Trial ou assinante |
| Dependências | EPIC-004, EPIC-005, EPIC-006, EPIC-007, EPIC-009 |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**, quero **ver apenas programas compatíveis com meu rank e perfil**, para **não escolher um treino avançado demais ou incompatível com minha evolução atual**.

## 3. Contexto

O EPIC-007 permite alterar o tipo de treino antes da quest e selecionar programas. Esta US define o catálogo inicial de programas e suas restrições por rank.

## 4. Objetivo

Criar catálogo de programas de treino com categoria, descrição, frequência, divisão, rank mínimo e disponibilidade.

## 5. Catálogo inicial de programas

| Programa | Categoria indicada | Rank mínimo |
|---|---|---|
| Full Body | Sedentário | E+ |
| AB | Sedentário, Iniciante | D+ |
| ABC | Intermediário | C+ |
| ABCD | Intermediário, Avançado | C+ |
| ABCDE | Avançado | B+ |
| Perfect 2 | Intermediário, Avançado | C+ |
| System | Qualquer um | E+ |

## 6. Descrição inicial dos programas

| Programa | Descrição resumida |
|---|---|
| Full Body | Corpo inteiro em cada sessão. |
| AB | Push + Pull com pernas integradas. |
| ABC | Clássico das academias com maior divisão muscular. |
| ABCD | Divisão intermediária/avançada para maior foco por grupo. |
| ABCDE | Divisão avançada com alto volume semanal. |
| Perfect 2 | Dois exercícios ideais por grupo muscular, com execução intensa. |
| System | Protocolo especial estilo desafio, acessível a qualquer rank, com adaptação por nível. |

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Programa deve ter `programKey`, nome, descrição, rank mínimo, categoria e status. |
| RN-002 | Usuário só pode selecionar programa quando possuir rank igual ou superior ao rank mínimo. |
| RN-003 | Programa bloqueado pode aparecer como bloqueado, mas não pode ser selecionado. |
| RN-004 | O backend valida rank mínimo antes de salvar a escolha. |
| RN-005 | A geração de quest deve respeitar o programa selecionado. |
| RN-006 | Programas devem aceitar evolução futura sem alterar o fluxo principal. |
| RN-007 | System deve adaptar volume/intensidade pelo perfil/rank do usuário. |

## 8. Fluxo principal

1. Usuário abre seleção de programa.
2. Backend retorna catálogo de programas com status permitido/bloqueado.
3. App mostra programas disponíveis e bloqueados.
4. Usuário seleciona programa permitido.
5. Backend valida rank e salva preferência/seleção.
6. Próxima geração usa o programa escolhido.

## 9. Impacto Backend

- Criar tabela/configuração de programas.
- Criar regra de rank mínimo.
- Expor endpoint de catálogo de programas.
- Validar programa na geração de quest.

## 10. Impacto Flutter

- Renderizar lista de programas.
- Mostrar rank mínimo e bloqueio.
- Permitir expansão de detalhes.
- Bloquear CTA quando rank não atender.

## 11. Contrato API sugerido

```txt
GET /api/training-programs
```

Response conceitual:

```json
{
  "items": [
    {
      "programKey": "ab",
      "name": "AB",
      "minimumRank": "D+",
      "isAvailable": true,
      "category": "Sedentário, Iniciante"
    }
  ]
}
```

## 12. Critérios de aceite

### CA-001 — Programa permitido

Dado que o usuário tem rank D+,
quando abrir programas,
então deve conseguir selecionar AB.

### CA-002 — Programa bloqueado

Dado que o usuário tem rank E+,
quando visualizar ABC,
então deve ver bloqueado e não conseguir selecionar.

### CA-003 — Validação server-side

Dado que o app tenta salvar programa acima do rank do usuário,
quando o backend validar,
então deve rejeitar a seleção.

## 13. Decisão registrada

Programas são parte da progressão do Hunter: devem orientar evolução e não permitir que usuário pule para divisões incompatíveis com seu rank.
