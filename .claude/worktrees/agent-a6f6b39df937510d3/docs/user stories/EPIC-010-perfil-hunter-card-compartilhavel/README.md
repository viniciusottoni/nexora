---
title: EPIC-010 — Perfil do Hunter e Card Compartilhável
sidebar_position: 10
---

# EPIC-010 — Perfil do Hunter e Card Compartilhável

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-010 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Exibir a identidade gamificada do usuário por meio do Perfil do Hunter e permitir o compartilhamento do progresso em formato de card.

## 3. Contexto de produto

O perfil é a materialização da evolução do usuário. O card compartilhável é importante para motivação pessoal e crescimento orgânico do produto em redes sociais e mensageiros.

## 4. Escopo

### Entra neste épico

- Perfil Hunter.
- Exibição de rank, level, XP, streak e dos 6 atributos (Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria).
- Classe inicial do usuário como P1.
- Avatar básico ou imagem de perfil como P1.
- Geração de card compartilhável.
- Compartilhamento por apps externos.
- Visual funcional durante trial.
- Visual premium para assinantes como P1.

### Fora deste épico

- Avatar 3D.
- Customização avançada.
- Ranking social.
- Feed interno.
- Card animado premium completo.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-073 | Visualizar perfil Hunter | P0 | [Abrir](./US-073-visualizar-perfil-hunter.md) |
| US-074 | Ver rank, level, XP, streak e os 6 atributos com seus respectivos Levels | P0 | [Abrir](./US-074-ver-rank-level-xp-streak-atributos.md) |
| US-075 | Definir classe inicial | P1 | [Abrir](./US-075-definir-classe-inicial.md) |
| US-076 | Usar avatar básico ou imagem de perfil | P1 | [Abrir](./US-076-usar-avatar-basico-imagem-perfil.md) |
| US-077 | Gerar card compartilhável | P0 | [Abrir](./US-077-gerar-card-compartilhavel.md) |
| US-078 | Compartilhar card | P0 | [Abrir](./US-078-compartilhar-card.md) |
| US-079 | Ter card funcional durante trial | P0 | [Abrir](./US-079-card-funcional-durante-trial.md) |
| US-080 | Ter visual premium no card | P1 | [Abrir](./US-080-visual-premium-card.md) |
| US-157 | Exibir nome narrativo e desbloqueios por Rank | P1 | [Abrir](./US-157-nome-narrativo-desbloqueios-rank.md) |

> A US-157 adiciona os nomes narrativos opcionais por Rank (Desperto, Aprendiz, Caçador, Elite, Ascendente, Despertado, Monarca, Lenda Viva) e os desbloqueios cosméticos por Rank. Desbloqueios ligados a Master Quests, card animado e eventos avançados são Pós-MVP.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-010-001 | Perfil completo só deve ser exibido para usuário com acesso ativo. |
| RN-EPIC-010-002 | Trial expirado ou assinatura expirada pode ver estado limitado com CTA de assinatura. |
| RN-EPIC-010-003 | Card deve conter informações reais do progresso. |
| RN-EPIC-010-004 | Card não pode expor dados sensíveis como peso, idade ou limitações. |
| RN-EPIC-010-005 | Compartilhamento deve usar imagem gerada a partir do card. |
| RN-EPIC-010-006 | O perfil deve exibir o Rank atual e o progresso de RankScore até o próximo Rank (fonte: EPIC-009). |
| RN-EPIC-010-007 | Cada Rank pode ter um nome narrativo opcional, mas o Rank principal (E→SSS) deve permanecer claro. |
| RN-EPIC-010-008 | Desbloqueios cosméticos por Rank não podem expor dados sensíveis nem alterar a segurança do treino. |

## 7. Impactos técnicos

### Flutter

- Tela de perfil Hunter.
- Componentes de rank, XP, level, streak e dos 6 atributos (Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria) com seus Levels.
- Captura do card como imagem.
- Integração com compartilhamento nativo.

### Backend

- Endpoint para consultar progresso.
- Endpoint para dados agregados do perfil.
- Regras de acesso conforme trial ou assinatura.

### Banco de dados

Entidades principais:

- User.
- UserProfile.
- HunterProgress.
- HunterAttributes.
- Subscription.

### Analytics

- `hunter_profile_viewed`.
- `hunter_card_shared`.

### QA

- Exibir perfil com dados corretos.
- Gerar card.
- Compartilhar card.
- Verificar bloqueio após expiração.
- Garantir que dados sensíveis não aparecem no card.

## 8. Dependências

- EPIC-003 para status de acesso.
- EPIC-009 para progresso.
- EPIC-011 para histórico resumido.

## 9. Critérios de aceite do épico

- Perfil mostra evolução atual.
- Card é gerado corretamente.
- Card pode ser compartilhado.
- Acesso expirado mostra estado limitado.
- Informações sensíveis não aparecem no card.

## 10. Decisão registrada

O perfil Hunter e o card compartilhável são essenciais para reforçar identidade, recompensa visual e viralização orgânica do AWAKEN.
