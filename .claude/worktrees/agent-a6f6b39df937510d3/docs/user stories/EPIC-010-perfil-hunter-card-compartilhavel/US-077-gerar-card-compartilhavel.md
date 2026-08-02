---
title: US-077 — Gerar card compartilhável
sidebar_position: 77
---

# US-077 — Gerar card compartilhável

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-077 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProfileCard |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **gerar um card compartilhável com meu progresso**,

para **mostrar minha evolução de forma visual e motivadora**.

---

## 3. Contexto

O card compartilhável é um mecanismo de motivação pessoal e crescimento orgânico. Ele deve ser bonito, legível e seguro, sem expor dados sensíveis.

---

## 4. Objetivo

Gerar uma imagem do card do Hunter contendo dados públicos de progresso e identidade visual do AWAKEN.

---

## 5. Escopo

### Entra nesta US

- Renderização do card na tela.
- Captura do card como imagem.
- Exibição de rank, level, XP, streak e atributos resumidos.
- Marca AWAKEN discreta.
- Proteção contra dados sensíveis.

### Fora desta US

- Compartilhamento externo, tratado na US-078.
- Card animado premium completo.
- Editor de layout.
- Ranking social.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Card deve conter dados reais do progresso. |
| RN-002 | Card não pode expor idade, peso, altura, sexo biológico, limitações ou dores. |
| RN-003 | Card deve usar identidade visual do AWAKEN. |
| RN-004 | Card deve ser gerado como imagem. |
| RN-005 | Acesso expirado não deve gerar card completo. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode gerar card. |
| Usuário em Trial | Pode gerar card funcional. |
| Premium Mensal | Pode gerar card completo. |
| Premium Anual | Pode gerar card completo. |
| Trial expirado | Não pode gerar card completo. |
| Assinatura expirada | Não pode gerar card completo. |

---

## 8. Fluxo principal

1. Usuário acessa Perfil Hunter.
2. Toca em gerar card.
3. App monta card com dados atuais.
4. App captura o card como imagem.
5. App disponibiliza imagem para compartilhamento ou prévia.

---

## 9. Fluxos alternativos

### 9.1. Erro ao capturar imagem

App deve exibir erro e permitir tentar novamente.

### 9.2. Acesso expirado

App deve exibir estado limitado com CTA de assinatura.

---

## 10. Estados esperados

- montando card;
- card pronto;
- gerando imagem;
- imagem gerada;
- erro de captura;
- acesso limitado.

---

## 11. Impacto no Frontend Flutter

- Componente de card.
- Captura de widget como imagem.
- Preview do card.
- Tratamento de erro.
- Layout responsivo.

---

## 12. Impacto no Backend

- Fornecer dados agregados do perfil.
- Garantir status de acesso.
- Não precisa gerar imagem no backend no MVP.

---

## 13. Impacto no Banco de Dados

Sem entidade nova obrigatória.

Usa dados de:

- HunterProgress;
- HunterAttributes;
- UserProfile;
- Subscription.

---

## 14. Impacto em Gamificação

- Transforma progresso em recompensa visual.
- Aumenta motivação e desejo de continuidade.

---

## 15. Impacto em Monetização

- Card funcional no trial.
- Visual premium pode ser aplicado para assinantes em US-080.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels do card. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/card-data
```

Response conceitual:

```json
{
  "displayName": "Vinícius",
  "rank": "E",
  "level": 3,
  "streakDays": 4
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_card_generated | Quando card é gerado como imagem. |

---

## 19. Critérios de aceite

### CA-001 — Card gerado

Dado que o usuário possui acesso ativo,

Quando gerar o card,

Então o app deve criar uma imagem compartilhável.

### CA-002 — Dados sensíveis ocultos

Dado que o card foi gerado,

Quando revisado,

Então não deve conter dados físicos ou limitações.

---

## 20. Critérios de teste para QA

- gerar card no trial;
- gerar card como assinante;
- validar ausência de dados sensíveis;
- erro de captura;
- preview em tela pequena;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O card compartilhável deve transformar o progresso real do usuário em uma imagem segura, motivadora e alinhada à identidade AWAKEN.
