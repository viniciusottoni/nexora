---
title: US-021 — Exibir paywall próprio, honesto e previsível
sidebar_position: 21
---

# US-021 — Exibir paywall próprio, honesto e previsível

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-021 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários impactados por monetização |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UX de monetização, status de acesso, RevenueCat SDK e design system AWAKEN |
| Modelo de UI | Tela própria Flutter dentro do AWAKEN |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **que a tela de assinatura seja honesta, previsível e integrada ao sistema**,

para **não sentir que fui enganado depois de investir tempo no app**.

---

## 3. Contexto

O AWAKEN deve evitar dark patterns. O usuário precisa saber desde o início que o trial dura 7 dias. Quando o paywall aparecer, ele deve parecer consequência natural da regra já comunicada, não surpresa.

A tela de assinatura deve ser criada dentro do próprio AWAKEN, seguindo o design system do sistema. A RevenueCat deve ser usada para infraestrutura de assinatura, produtos, packages, preços, entitlement, compra e restauração, mas a experiência visual e textual deve ser controlada pelo app.

A escolha do plano já deve ter sido feita na pricing e salva antes da criação da conta. O paywall não é o lugar para trocar de plano; ele só confirma a escolha e conduz a compra vinculada à conta.

---

## 4. Objetivo

Definir diretrizes funcionais, éticas e visuais para que o paywall próprio do AWAKEN comunique valor, bloqueio, preço, progresso preservado e próximos passos de forma clara.

---

## 5. Escopo

### Entra nesta US

- Mensagem clara de motivo do paywall.
- Reforço de que o trial acabou ou assinatura expirou.
- Confirmação do plano salvo na pricing.
- CTA direto para assinar.
- Link para termos, política de privacidade, suporte e conta quando aplicável.
- Linguagem sem pressão enganosa.
- Tela própria Flutter do AWAKEN.
- Uso do design system do AWAKEN.
- Uso do RevenueCat SDK para obter packages, preços e estado de entitlement.
- Exibição de progresso preservado.
- Estado de preço indisponível.
- Estado de restauração de compra.
- Compatibilidade com `test_store`, `google_sandbox` e `production` por configuração.

### Fora desta US

- Design final do checkout nativo da loja.
- Compra efetiva detalhada, coberta por US-118 e US-119.
- Ofertas promocionais.
- Testes A/B.
- Dependência obrigatória do RevenueCat Paywall Builder.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Paywall não deve aparecer antes de comunicar o trial ao usuário. |
| RN-002 | Paywall deve explicar por que o acesso está bloqueado. |
| RN-003 | O paywall deve confirmar o plano salvo na pricing e não permitir nova escolha fora dela. |
| RN-004 | Paywall não deve esconder opção de fechar quando houver rota permitida, como conta, termos, privacidade ou suporte. |
| RN-005 | Paywall não deve usar contagem falsa, urgência falsa ou promessa enganosa. |
| RN-006 | Usuário assinante ativo não deve ver paywall obrigatório. |
| RN-007 | Paywall não deve ameaçar apagar XP, rank, histórico, conquistas ou itens armazenados. |
| RN-008 | A tela deve informar que o progresso permanece salvo. |
| RN-009 | Preços e dados comerciais devem vir do RevenueCat SDK / loja sempre que disponíveis, para confirmar o plano salvo. |
| RN-010 | Caso preço não esteja disponível, a tela deve informar indisponibilidade temporária e permitir nova tentativa. |
| RN-011 | O usuário deve conseguir restaurar compras. |
| RN-012 | O modelo visual final deve ser uma tela própria do AWAKEN. |
| RN-013 | O RevenueCat Paywall Builder pode ser usado apenas como referência/protótipo, mas não é requisito para a UI final do app. |
| RN-014 | A alternância sandbox/produção deve ocorrer por configuração, sem alterar copy, layout ou regra de negócio principal. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Vê comunicação de trial antes do onboarding. |
| Usuário em Trial | Pode ver planos, mas não paywall obrigatório. |
| Premium Mensal | Não deve ver paywall obrigatório. |
| Premium Anual | Não deve ver paywall obrigatório. |
| Trial expirado | Deve ver paywall obrigatório próprio do AWAKEN. |
| Assinatura expirada | Deve ver paywall obrigatório próprio do AWAKEN. |

---

## 8. Diretrizes de UX e copy

### 8.1. Mensagem obrigatória

A tela deve responder claramente:

```txt
Por que estou vendo isso?
O que acontece com meu progresso?
Quais opções tenho agora?
Como posso continuar?
Onde vejo termos, privacidade, suporte e conta?
```

### 8.2. Copy recomendada em PT-BR

Título:

```txt
Continue sua evolução com clareza
```

Subtítulo:

```txt
O AWAKEN oferece 7 dias gratuitos. Depois disso, o acesso aos recursos protegidos continua com uma assinatura mensal ou anual.
```

Motivo do paywall:

```txt
Este paywall aparece porque seu acesso gratuito ou sua assinatura não está mais ativa.
```

Progresso preservado:

```txt
Seu progresso permanece salvo: XP, rank, histórico, conquistas e itens armazenados não serão apagados.
```

CTA principal:

```txt
Confirmar plano
```

Links mínimos:

```txt
Minha conta
Termos
Política de privacidade
Suporte
Restaurar compra
```

Rodapé ético:

```txt
Sem urgência falsa. Sem perda de progresso. Você pode consultar termos, privacidade e suporte a qualquer momento.
```

### 8.3. Frases proibidas

A tela não deve usar frases como:

```txt
Última chance
Você perderá tudo
Oferta acaba em instantes
Assine agora ou seu progresso será apagado
Apenas hoje
Vagas limitadas
```

---

## 9. Diretrizes visuais

A tela deve seguir o design system do AWAKEN e manter coerência com a experiência gamificada fitness.

Diretrizes esperadas:

- Visual dark, premium e épico.
- Fundo escuro, cinematográfico e discreto.
- Cards de plano com contraste claro.
- Destaque visual controlado para plano anual.
- Feedback claro de loading e erro.
- Elementos de gamificação sem poluir a decisão de compra.
- Mensagem de progresso preservado em destaque positivo.
- Linguagem visual motivacional, não ameaçadora.
- Botões acessíveis, legíveis e com área de toque adequada.

A tela pode usar referências visuais do universo AWAKEN, como energia, despertar, evolução, XP, rank e dungeons, desde que não esconda preço, condição de assinatura ou links legais.

---

## 10. Modelo de ambientes

A mesma tela deve funcionar para sandbox e produção.

| Ambiente | Comportamento da UI | Dados comerciais | Observação |
|---|---|---|---|
| `test_store` | Tela própria normal; pode exibir marcação interna em build de QA | RevenueCat Test Store | Smoke test sem Play Store |
| `google_sandbox` | Tela própria normal; pode exibir marcação interna em build de QA | Google Play sandbox via RevenueCat | Teste real Android |
| `production` | Tela própria normal, sem marcação técnica | Google Play produção via RevenueCat | Usuário final |

Regras:

- A UI não deve ter uma versão separada para produção.
- O ambiente deve ser resolvido em configuração.
- Builds de produção não devem exibir textos técnicos de sandbox.
- Eventos de analytics devem carregar o ambiente para facilitar QA e auditoria.

---

## 11. Fluxo principal

1. Usuário tenta acessar recurso protegido sem acesso ativo.
2. App identifica status expirado pelo backend.
3. App carrega configuração de assinatura.
4. App busca offering/packages pelo RevenueCat SDK.
5. App exibe tela própria com motivo claro.
6. Usuário vê a confirmação do plano salvo na pricing.
7. Usuário vê que progresso está salvo.
8. Usuário pode seguir para assinatura ou rotas permitidas.
9. App inicia fluxo de compra conforme US-118 ou US-119.
10. Após compra/restauração, app atualiza status local e solicita sincronização com backend.

---

## 12. Fluxos alternativos

### 12.1. Usuário já assinante

Se o status for assinatura ativa, o paywall não deve aparecer.

### 12.2. Erro ao carregar preço

Paywall deve manter mensagem de bloqueio, informar que os preços não carregaram e permitir nova tentativa.

### 12.3. Usuário cancela compra

Tela deve permanecer no paywall sem mensagem agressiva.

### 12.4. Usuário quer restaurar compra

Tela deve oferecer ação de restaurar compra e refletir o resultado de forma clara.

### 12.5. Ambiente de teste

Em builds internos, QA pode visualizar o modo ativo (`test_store`, `google_sandbox` ou `production`) em área discreta. Essa informação não deve aparecer em produção.

---

## 13. Estados de tela ou estados esperados

- paywall carregado;
- planos carregados;
- preço indisponível;
- usuário assinante detectado;
- erro de conexão;
- carregando RevenueCat;
- restaurando compras;
- compra cancelada;
- compra concluída;
- sincronização pendente;
- ambiente de teste identificado para QA.

---

## 14. Impacto no Frontend Flutter

- Componentes de paywall reutilizáveis.
- Mensagens claras e localizadas.
- Cards mensal/anual como confirmação do plano salvo.
- CTA de assinatura.
- Rotas permitidas para conta, termos, política de privacidade e suporte.
- Botão de restaurar compras.
- Integração com RevenueCat SDK.
- Configuração por ambiente.
- Tratamento de preço indisponível.
- Tratamento de compra cancelada sem culpa/pressão.
- Garantia de que assinante ativo não veja paywall obrigatório.

---

## 15. Impacto no Backend

- Retornar status de acesso correto.
- Não permitir endpoints protegidos com acesso expirado.
- Sincronizar status recebido por webhook.
- Permitir que o app reconsulte status após compra/restauração.
- Registrar ambiente do evento para auditoria.

---

## 16. Impacto no Banco de Dados

Sem impacto exclusivo além de Subscription, AccessStatus e auditoria comercial.

Campos úteis:

- accessStatus;
- plan;
- status;
- expiresAt;
- trialEndsAt;
- revenueCatEnvironment;
- lastSyncedAt;
- lastRevenueCatEventAt.

---

## 17. Impacto em Gamificação

- Deve reforçar que progresso está salvo.
- Não deve ameaçar apagar XP, rank ou histórico.
- Itens armazenados durante o trial devem ser tratados como preservados.
- Bloqueio deve ser apresentado como pausa de evolução, não punição.

---

## 18. Impacto em Monetização

- Define padrão ético de paywall.
- Ajuda confiança e conversão sem dark pattern.
- Mantém controle visual dentro do AWAKEN.
- Evita dependência de uma tela genérica externa.
- Permite testar sandbox e produção com a mesma experiência de usuário.

---

## 19. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de paywall claras. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 20. Contrato de API sugerido

Não há endpoint exclusivo. Usa status de assinatura, configuração de planos e endpoints já definidos para assinatura.

Referências conceituais:

```txt
GET /api/subscriptions/status
POST /api/subscriptions/sync
```

---

## 21. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| paywall_viewed | Quando paywall é exibido. |
| paywall_custom_viewed | Quando a tela própria do AWAKEN é exibida. |
| paywall_after_trial_viewed | Quando paywall aparece após trial. |
| paywall_terms_clicked | Quando usuário abre termos. |
| paywall_privacy_clicked | Quando usuário abre política de privacidade. |
| paywall_support_clicked | Quando usuário abre suporte. |
| restore_purchase_clicked | Quando usuário tenta restaurar compra. |
| paywall_price_unavailable | Quando preço não carrega. |

---

## 22. Critérios de aceite

### CA-001 — Motivo claro

Dado que o paywall é exibido,

Quando o usuário ler a tela,

Então deve entender por que o acesso está bloqueado.

### CA-002 — Sem dark pattern

Dado que o paywall está visível,

Quando for revisado pelo QA,

Então não deve conter urgência falsa, preço inventado ou promessa enganosa.

### CA-003 — Progresso preservado

Dado que o usuário tem progresso salvo,

Quando visualizar o paywall,

Então deve entender que XP, rank, histórico, conquistas e itens armazenados não serão apagados.

### CA-004 — Tela própria do AWAKEN

Dado que o paywall está visível,

Quando o QA validar a tela,

Então a experiência visual deve estar dentro do sistema AWAKEN e não depender do RevenueCat Paywall Builder.

### CA-005 — Sandbox e produção

Dado que o ambiente foi alterado por configuração,

Quando o app carregar o paywall,

Então a mesma tela deve funcionar em sandbox e produção, mudando apenas origem dos produtos/eventos.

---

## 23. Critérios de teste para QA

- trial expirado;
- assinatura expirada;
- usuário assinante ativo;
- preço indisponível;
- rotas permitidas;
- textos em PT-BR, EN e ES;
- tela própria do AWAKEN;
- ausência de RevenueCat Paywall Builder como UI obrigatória;
- ausência de urgência falsa;
- ausência de ameaça de perda de progresso;
- restauração de compra;
- ambiente `test_store`;
- ambiente `google_sandbox`;
- ambiente `production`;
- chaveamento entre ambientes sem alterar regra de negócio.

---

## ✅ Decisão registrada

> O paywall do AWAKEN deve ser obrigatório quando o acesso expirar, mas sempre transparente, previsível e sem dark patterns. A experiência visual deve ser própria do sistema em Flutter, usando RevenueCat SDK como infraestrutura de produtos, preços, compras, entitlement e restauração. O fluxo deve ser testável em sandbox e chaveável para produção por configuração.
