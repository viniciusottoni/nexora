---
title: US-025 — Informar idade, altura, peso e sexo biológico
sidebar_position: 25
---

# US-025 — Informar idade, altura, peso e sexo biológico

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-025 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.dadosFisicos |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **informar idade, altura, peso e sexo biológico**,

para **melhorar a recomendação inicial dos meus treinos**.

---

## 3. Contexto

A etapa 4/8 coleta dados físicos básicos. Esses dados ajudam a calibrar recomendações iniciais, mas não devem ser tratados como avaliação médica ou diagnóstico.

---

## 4. Objetivo

Coletar idade, altura, peso e sexo biológico por meio de opções pré-definidas (homem, mulher, prefiro não informar), salvando os dados no perfil inicial do usuário.

---

## 5. Escopo

### Entra nesta US

- Coletar idade.
- Coletar altura.
- Coletar peso.
- Coletar sexo biológico por meio de opções pré-definidas (homem, mulher, prefiro não informar).
- Validar faixas aceitáveis.
- Exibir aviso de privacidade/uso responsável quando necessário.

### Fora desta US

- Diagnóstico de saúde.
- Cálculo médico.
- IMC como recomendação clínica.
- Avaliação nutricional completa.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Idade, altura, peso e sexo biológico fazem parte da etapa 4/8. |
| RN-002 | Idade, altura e peso devem ser validados por faixa aceitável. |
| RN-003 | Sexo biológico deve ser selecionado entre as opções pré-definidas: homem, mulher ou prefiro não informar. |
| RN-004 | Dados físicos não devem aparecer no card compartilhável. |
| RN-005 | O app deve deixar claro que não substitui orientação profissional. |
| RN-006 | Os dados poderão ser editados depois, conforme US-034. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode informar. |
| Usuário em Trial | Pode informar. |
| Premium Mensal | Pode informar. |
| Premium Anual | Pode informar. |
| Trial expirado | Não pode alterar durante bloqueio. |
| Assinatura expirada | Não pode alterar durante bloqueio. |

---

## 8. Fluxo principal

1. Usuário acessa etapa 4/8.
2. Informa idade, altura, peso e sexo biológico.
3. App valida os campos.
4. Dados são salvos no perfil.
5. Usuário avança para etapa 5/8.

---

## 9. Fluxos alternativos

### 9.1. Valor fora da faixa

O app deve exibir mensagem de validação e impedir avanço.

### 9.2. Seleção de sexo biológico

O app deve exigir a seleção de uma das opções pré-definidas (homem, mulher ou prefiro não informar) antes de avançar.

---

## 10. Estados esperados

- preenchendo;
- válido;
- erro de validação;
- salvando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Formulário de dados físicos.
- Inputs numéricos.
- Botões de seleção para sexo biológico (homem, mulher, prefiro não informar).
- Validações locais.
- Textos de privacidade localizados.

---

## 12. Impacto no Backend

- Validar dados recebidos.
- Salvar no UserProfile.
- Proteger exposição indevida.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campos:

- age;
- heightCm;
- weightKg;
- biologicalSex.

---

## 14. Impacto em Gamificação

- Pode influenciar recomendações iniciais.
- Não altera XP, rank ou streak diretamente.

---

## 15. Impacto em Monetização

- Reforça percepção de personalização durante o trial.
- Não altera assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels, unidades e validações. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
PATCH /api/users/me/profile/onboarding
```

Request:

```json
{
  "age": 28,
  "heightCm": 175,
  "weightKg": 82,
  "biologicalSex": "masculino"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando etapa 4/8 é salva. |

---

## 19. Critérios de aceite

### CA-001 — Dados válidos

Dado que o usuário informa dados válidos,

Quando avançar,

Então os dados devem ser salvos.

### CA-002 — Seleção pré-definida

Dado que o usuário seleciona uma das opções pré-definidas de sexo biológico,

Quando avançar,

Então a opção selecionada deve ser salva.

### CA-003 — Dados sensíveis protegidos

Dado que o card compartilhável é gerado,

Quando o usuário compartilhar,

Então dados físicos não devem aparecer.

---

## 20. Critérios de teste para QA

- dados válidos;
- idade inválida;
- altura inválida;
- peso inválido;
- seleção de sexo biológico entre as opções pré-definidas;
- falha de conexão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Dados físicos básicos compõem a etapa 4/8 e devem melhorar personalização, com sexo biológico selecionado entre opções pré-definidas e sem exposição pública no card.
