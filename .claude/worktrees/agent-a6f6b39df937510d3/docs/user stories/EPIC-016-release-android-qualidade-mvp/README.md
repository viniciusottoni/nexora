---
title: EPIC-016 — Release Android e Qualidade MVP
sidebar_position: 16
---

# EPIC-016 — Release Android e Qualidade MVP

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-016 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Engenharia, QA e Produto |
| Planos impactados | Trial, Mensal e Anual |
| Plataforma | Android |
| Status | Planejado |

## 2. Objetivo

Preparar o AWAKEN para testes internos, validação de qualidade e publicação inicial Android, garantindo que os fluxos críticos do MVP estejam estáveis antes do lançamento.

## 3. Contexto de produto

O MVP precisa competir em confiança. Como apps concorrentes recebem críticas por bugs, o AWAKEN deve priorizar estabilidade sobre excesso de funcionalidades. O lançamento só deve ocorrer quando cadastro, trial, onboarding, quest, gamificação e assinatura funcionarem sem bloqueios críticos.

## 4. Escopo

### Entra neste épico

- Ambientes de desenvolvimento, homologação e produção.
- Build Android de teste interno.
- Smoke test.
- Testes dos fluxos críticos (incluindo dungeon e itens).
- Testes de trial, assinatura mensal, assinatura anual e acesso expirado.
- Testes de gamificação: 6 atributos, progressão de atributo (pontos internos → level up), Sabedoria e penalidade de XP.
- Feature flags simples como P1.
- Publicação em teste aberto na Google Play.

### Fora deste épico

- Publicação iOS.
- Pipeline avançado de release train.
- Testes automatizados completos de ponta a ponta.
- Monitoramento corporativo avançado.

## 5. User Stories relacionadas

| ID | Título | Prioridade |
|---|---|---|
| US-109 | Configurar ambientes | P0 |
| US-110 | Gerar build Android de teste interno | P0 |
| US-111 | Executar checklist de smoke test | P0 |
| US-112 | Testar onboarding, quest diária, dungeon, edição, conclusão, itens e gamificação completa (6 atributos, XP, penalidade, level up de atributo) | P0 |
| US-113 | Testar trial, planos e assinatura expirada | P0 |
| US-114 | Usar feature flags simples | P1 |
| US-115 | Publicar teste aberto na Google Play | P0 |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-016-001 | Nenhum bug crítico pode impedir cadastro, login, trial, onboarding, quest (diária ou dungeon), conclusão, gamificação, assinatura ou reativação. |
| RN-EPIC-016-002 | Build interno deve ser validado antes de teste aberto. |
| RN-EPIC-016-003 | Cenários comerciais precisam ser testados antes do lançamento. |
| RN-EPIC-016-004 | Crashlytics deve estar ativo antes do teste aberto. |
| RN-EPIC-016-005 | Smoke test deve cobrir os fluxos P0. |

## 7. Impactos técnicos

### Flutter

- Configuração de flavors ou ambientes.
- Build Android.
- Configuração de Firebase.
- Feature flags simples, se aplicável.

### Backend

- Ambientes separados.
- Configurações de API por ambiente.
- Logs e tratamento de erro.
- Integrações configuradas para homologação e produção.

### Banco de dados

- Banco por ambiente.
- Migrations aplicadas.
- Seeds mínimos de exercícios.

### Analytics

- Eventos críticos ativos.
- Crashlytics ativo.
- Separação de ambiente, quando possível.

### QA

- Smoke test.
- Teste de regressão dos fluxos P0.
- Teste em dispositivo Android mínimo.
- Teste de assinatura e acesso expirado.
- Teste de quest diária completa (geração → edição → execução → recompensa → XP e atributos).
- Teste de dungeon completa (ativação → execução → recompensa com item → inventário).
- Teste de penalidade de XP ao perder quest diária.
- Teste de progressão de atributo (pontos internos → level up, incluindo Sabedoria).
- Teste de idiomas.

## 8. Dependências

- Todos os épicos P0 implementados.
- Firebase configurado.
- RevenueCat configurado.
- Backend publicado em ambiente de homologação.
- EPIC-017 com site admin e monitoramento operacional mínimo validado.
- Conta Google Play pronta.

## 9. Critérios de aceite do épico

- Build Android interno é gerado.
- Smoke test passa.
- Fluxos críticos funcionam (quest diária, dungeon, gamificação completa).
- Crashlytics captura falhas.
- Trial e assinatura são testados.
- Penalidade de XP, progressão de atributo e Sabedoria são validados.
- Site admin mínimo está disponível para acompanhar tickets, bugs, backend e alertas de segurança.
- App está pronto para teste aberto na Google Play.

## 10. Decisão registrada

O MVP só deve ser publicado quando estabilidade, trial, onboarding, quest diária, dungeon, sistema de itens, gamificação completa (6 atributos, Sabedoria, penalidade de XP), assinatura, reativação de acesso e monitoramento operacional via site admin estiverem validados.
