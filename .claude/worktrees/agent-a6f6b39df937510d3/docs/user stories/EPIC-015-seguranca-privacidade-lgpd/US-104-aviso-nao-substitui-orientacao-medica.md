---
title: US-104 — Entender que o app não substitui orientação médica
sidebar_position: 104
---

# US-104 — Entender que o app não substitui orientação médica

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-104 |
| Épico | EPIC-015 — Segurança, Privacidade e LGPD |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **ser informado de que o app não substitui orientação médica ou profissional**,

para **usar os treinos de forma consciente e responsável**.

---

## 3. Contexto

O AWAKEN personaliza treinos usando dados físicos, objetivo, dores e limitações informadas. Mesmo assim, o app não deve se apresentar como médico, nutricionista, fisioterapeuta ou educador físico pessoal.

---

## 4. Objetivo

Exibir aviso claro de responsabilidade antes do uso principal e em pontos sensíveis do produto.

---

## 5. Escopo

### Entra nesta US

- Aviso no fluxo inicial/aceite.
- Aviso em onboarding quando coletar limitações e dores.
- Aviso em telas de treino quando aplicável.
- Texto claro orientando procurar profissional em caso de dor, condição prévia ou dúvida.
- Registro de que o usuário visualizou/aceitou o aviso junto aos termos quando aplicável.

### Fora desta US

- Diagnóstico médico.
- Triagem clínica avançada.
- Prescrição fisioterapêutica.
- Prontuário de saúde.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O app deve informar que não substitui médico, nutricionista, educador físico ou fisioterapeuta. |
| RN-002 | Aviso deve aparecer antes do usuário executar treinos. |
| RN-003 | Aviso deve usar linguagem simples e objetiva. |
| RN-004 | Aviso não deve assustar o usuário, mas precisa ser claro. |
| RN-005 | Caso usuário informe dor/limitação relevante, o app deve reforçar uso responsável. |
| RN-006 | O app não deve prometer cura, diagnóstico ou tratamento. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode visualizar aviso. |
| Usuário em Trial | Deve visualizar antes de usar treinos. |
| Premium Mensal | Deve visualizar antes de usar treinos. |
| Premium Anual | Deve visualizar antes de usar treinos. |

---

## 8. Fluxo principal

1. Usuário acessa fluxo inicial.
2. App exibe aviso de responsabilidade junto ao aceite ou antes do onboarding.
3. Usuário confirma ciência.
4. App registra ciência quando aplicável.
5. Usuário segue para onboarding/treino.

---

## 9. Fluxos alternativos

### 9.1. Usuário informa dor ou limitação

App reforça que o treino será adaptado, mas orienta procurar profissional se houver dor persistente, lesão ou condição relevante.

### 9.2. Usuário não confirma ciência

App não libera execução de treinos.

---

## 10. Estados esperados

- aviso pendente;
- aviso exibido;
- ciência confirmada;
- aviso reforçado;
- bloqueado sem ciência.

---

## 11. Impacto Flutter

- Componente de aviso de responsabilidade.
- Exibição no fluxo inicial.
- Exibição contextual no onboarding e pré-treino quando necessário.
- Textos localizados PT-BR, EN e ES.

---

## 12. Impacto Backend

- Campo opcional para registrar ciência do aviso.
- Guard para execução de treino se a regra exigir confirmação.
- Log da confirmação, sem dados sensíveis desnecessários.

---

## 13. Impacto DB

Campos sugeridos:

- responsibilityNoticeAcceptedAt;
- responsibilityNoticeVersion.

---

## 14. Impacto Gamificação

- Sem impacto em XP.
- Pode bloquear execução de quest até ciência do aviso.

---

## 15. Impacto Monetização

- Usuário deve entender o limite do produto antes de iniciar trial/treinos.

---

## 16. Contrato API sugerido

```txt
POST /api/users/me/responsibility-notice
```

Request conceitual:

```json
{
  "noticeVersion": "1.0.0",
  "accepted": true
}
```

---

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| responsibility_notice_viewed | Quando aviso é exibido. |
| responsibility_notice_accepted | Quando usuário confirma ciência. |

---

## 18. Critérios de aceite

### CA-001 — Aviso exibido

Dado que o usuário ainda não confirmou ciência,
Quando acessar o fluxo principal,
Então deve ver aviso de que o app não substitui orientação profissional.

### CA-002 — Execução bloqueada sem ciência

Dado que o usuário não confirmou ciência,
Quando tentar iniciar treino,
Então o app deve bloquear até confirmação.

---

## 19. Critérios de teste QA

- aviso inicial;
- confirmação do aviso;
- tentativa de iniciar treino sem ciência;
- usuário com limitação/dor informada;
- textos PT-BR, EN e ES;
- ausência de promessas médicas indevidas.

---

## 20. Decisão registrada

> O AWAKEN deve deixar claro que apoia treinos e hábitos, mas não substitui orientação profissional de saúde ou educação física.
