---
title: US-006 — Garantir experiência estável em celulares Android mínimos
sidebar_position: 6
---

# US-006 — Garantir experiência estável em celulares Android mínimos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-006 |
| Épico | EPIC-001 — Fundação Mobile e Experiência Base |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários do app mobile Android |
| Plano | Visitante, Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Requisitos mínimos Android e performance Flutter |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário Android**,

quero **que o app funcione bem no meu celular**,

para **conseguir iniciar o AWAKEN, navegar e usar os fluxos principais sem travamentos críticos**.

---

## 3. Contexto

O MVP precisa priorizar estabilidade. Uma das críticas mais fortes a concorrentes é bug crítico, falha ao abrir, loop de login e travamentos. O AWAKEN deve nascer com requisitos mínimos claros e comportamento previsível em aparelhos Android compatíveis.

---

## 4. Objetivo

Definir e validar a base mínima de compatibilidade e estabilidade Android para o MVP.

---

## 5. Escopo

### Entra nesta US

- Definir versão mínima de Android.
- Definir requisitos mínimos aproximados de memória e armazenamento.
- Validar abertura do app.
- Validar navegação base.
- Validar tema e componentes base.
- Validar comportamento sem travamentos críticos.
- Garantir integração com Crashlytics no ciclo de qualidade.

### Fora desta US

- Otimização avançada para todos os modelos Android existentes.
- Testes em tablets.
- Publicação iOS.
- Benchmark gráfico avançado.
- Suporte offline completo.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O app deve definir explicitamente versão mínima de Android suportada. |
| RN-002 | Fluxos P0 não podem travar em dispositivo mínimo definido. |
| RN-003 | O app deve capturar crashes em ambiente de teste e produção. |
| RN-004 | Animações do MVP não devem prejudicar abertura e navegação. |
| RN-005 | O app deve exibir erro controlado quando não conseguir carregar dados essenciais. |
| RN-006 | Build de teste interno deve validar dispositivos mínimos antes do lançamento aberto. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Deve ter abertura e navegação pública estável. |
| Usuário em Trial | Deve ter fluxos protegidos estáveis. |
| Premium Mensal | Deve ter fluxos protegidos estáveis. |
| Premium Anual | Deve ter fluxos protegidos estáveis. |
| Trial expirado | Deve ter telas limitadas e paywall estáveis. |
| Assinatura expirada | Deve ter telas limitadas e paywall estáveis. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. QA instala o app em dispositivo Android mínimo definido.
2. QA abre o app.
3. QA valida splash, navegação base e tema.
4. QA executa smoke test dos fluxos disponíveis.
5. Falhas críticas são registradas e corrigidas antes do lançamento.

---

## 9. Fluxos alternativos

### 9.1. Dispositivo abaixo do mínimo

Se o dispositivo não atender os requisitos mínimos, o app pode não ser suportado oficialmente.

### 9.2. Crash em teste interno

Se ocorrer crash, ele deve ser capturado, analisado e tratado antes do lançamento aberto.

### 9.3. Lentidão visual

Se animação ou componente causar lentidão perceptível, deve ser simplificado para preservar estabilidade.

---

## 10. Estados de tela ou estados esperados

- app instalado;
- app aberto;
- app em carregamento;
- app navegável;
- erro controlado;
- crash capturado;
- dispositivo não suportado.

---

## 11. Impacto no Frontend Flutter

- Definir minSdkVersion.
- Validar performance de splash e tema.
- Evitar animações pesadas no MVP.
- Integrar Crashlytics.
- Validar responsividade em tela mínima.
- Garantir tratamento de erro visual.

---

## 12. Impacto no Backend

Não há endpoint exclusivo desta US.

O backend deve retornar erros controláveis para que o app não trave em falhas de comunicação.

---

## 13. Impacto no Banco de Dados

Não há impacto direto em banco de dados.

---

## 14. Impacto em Gamificação

- Estabilidade garante que o usuário consiga chegar aos fluxos de quest, XP e streak.
- Não altera regras de XP, rank ou atributos.

---

## 15. Impacto em Monetização

- Estabilidade é requisito para conversão após trial.
- O app não deve travar em telas de trial, paywall ou assinatura.
- Não altera regras comerciais.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de erro devem estar localizadas. |
| EN | Preparar fallback equivalente. |
| ES | Preparar fallback equivalente. |

---

## 17. Contrato de API sugerido

Não aplicável diretamente.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| app_opened | Quando o app abrir. |
| crash_detected | Quando Crashlytics capturar falha. |

---

## 19. Critérios de aceite

### CA-001 — App abre no Android mínimo

Dado que o dispositivo atende o mínimo suportado,

Quando o usuário abrir o app,

Então o app deve iniciar sem crash.

### CA-002 — Navegação base estável

Dado que o app abriu,

Quando o usuário navegar pelas telas base,

Então não deve ocorrer travamento crítico.

### CA-003 — Crash capturado

Dado que ocorre falha em ambiente monitorado,

Quando o Crashlytics estiver ativo,

Então a falha deve ser registrada.

### CA-004 — Animações leves

Dado que uma tela usa animação,

Quando for executada em dispositivo mínimo,

Então não deve comprometer a usabilidade.

---

## 20. Critérios de teste para QA

- instalar em Android mínimo;
- abrir app;
- validar splash;
- validar navegação base;
- validar tema;
- validar tela pequena;
- validar sem internet;
- validar captura de crash;
- executar smoke test P0.

---

## ✅ Decisão registrada

> O AWAKEN deve priorizar estabilidade acima de efeitos visuais complexos no MVP. O app precisa abrir, navegar e tratar erros corretamente em Android compatível.
