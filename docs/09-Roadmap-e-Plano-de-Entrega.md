# 09 — Roadmap e Plano de Entrega
## Ecossistema Nexora

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Roadmap e Plano de Entrega |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Depende de** | `07-Backlog-Epicos-e-User-Stories.md` |

> **Aviso.** Prazo, orçamento e priorização **não foram informados pelo cliente** (bloco 09 do briefing em branco). Este plano é a proposta da Replay e precisa ser validado antes de virar compromisso contratual.

---

## 1. Visão do roadmap

```
2026                                                                        2027
 Ago    Set    Out    Nov    Dez    Jan    Fev    Mar    Abr    Mai    Jun    Jul
  │      │      │      │      │      │      │      │      │      │      │      │
  ├─ F0 ─┤
  │ Fundação
  │      ├──────── FASE 1 · MVP OPERACIONAL ────────┤
  │      │  pedido · KDS · caixa · sync · painel v1
  │      │                                    ├───── FASE 2 ─────┤
  │      │                                    │ estoque · custo · CMV
  │      │                                    │        ├──── FASE 3 ────┤
  │      │                                    │        │ financeiro
  │      │                                    │        │      ├─ FASE 4 ─┤
  │      │                                    │        │      │ delivery
  │      │                                    │        │      │    ├──── FASE 5 · ESCALA ────┤
  │      │                                    │        │      │    │ E-14 + E-15
  ▼      ▼                                    ▼        ▼      ▼                          ▼
 M0     M1                                   M2       M3     M4                         M5
```

| Marco | Nome | Data alvo | Critério de conclusão |
|---|---|---|---|
| **M0** | Fundação pronta | Set/2026 | Multi-tenant, auth, CI, edge instalável |
| **M1** | MVP em piloto | Dez/2026 | Dona Betinha operando 100% no sistema por 14 dias |
| **M2** | Custo sob controle | Fev/2027 | 100% dos produtos com ficha técnica; CMV apurado |
| **M3** | Gestão financeira | Mar/2027 | Resultado do mês fechado no sistema |
| **M4** | Delivery próprio | Abr/2027 | Primeiro pedido pago pelo canal próprio entregue |
| **M5** | Plataforma pronta para escala | Jul/2027* | Nova loja implantada em ≤ 5 dias úteis e administrável integralmente pelo painel |

> Datas assumem início em agosto/2026 e a equipe descrita na seção 5. São **estimativas**, não compromissos — dependem das pendências da seção 8.

---

## 2. Fase 0 — Fundação (4 semanas)

**Objetivo:** deixar pronto o que não pode ser feito depois.

| Sprint | Entrega |
|---|---|
| **S0.1** | Solution .NET (Clean Architecture), CI/CD, EF Core, schema base, RLS, teste de isolamento |
| **S0.2** | Autenticação (senha + PIN), papéis, permissões, dispositivos |
| **S0.3** | Multi-tenant completo, configuração e branding em runtime |
| **S0.4** | Docker Compose do edge, script de instalação, registro na nuvem |

**Critérios de saída**

- [ ] Dois tenants criados com isolamento verificado por teste automatizado
- [ ] Login por senha e por PIN funcionando
- [ ] Instalação do edge em máquina limpa em < 30 min
- [ ] Pipeline bloqueando código condicional por tenant
- [ ] Branding aplicado em runtime, sem build por cliente

---

## 3. Fase 1 — MVP operacional (12 semanas)

**Objetivo:** *"o pedido chega na cozinha e o dono enxerga"*.

| Sprint | Foco | Épicos |
|---|---|---|
| **S1.1** | Catálogo, produtos, variações, modificadores, meio a meio | E-01 |
| **S1.2** | Domínio de pedido, máquina de estados, event store | E-03 |
| **S1.3** | Mesa, comanda, QR Code, cardápio público | E-02 |
| **S1.4** | PWA do garçom, mapa de mesas, lançamento | E-02, E-03 |
| **S1.5** | KDS: fila, cronômetro, teclado numérico | E-04 |
| **S1.6** | KDS: praças, all-day, indisponibilidade, alertas sonoros | E-04, E-08 |
| **S1.7** | Caixa: conta, formas de pagamento, divisão | E-05 |
| **S1.8** | Caixa: abertura/fechamento, desconto, auditoria | E-05, E-09 |
| **S1.9** | Sincronização: outbox, worker, recepção idempotente | E-06 |
| **S1.10** | Sincronização: pull, conflitos, indicadores, recuperação | E-06 |
| **S1.11** | Métricas: agregados, painel do dono v1, drill-down | E-07 |
| **S1.12** | Endurecimento, testes de carga e caos, preparação do piloto | — |

### 3.1 Por que a sincronização vem no fim

É o componente mais arriscado. Construí-lo com o fluxo operacional já estável significa ter **eventos reais para sincronizar** em vez de casos hipotéticos — e permite fatiar o escopo se a estimativa estourar (Fase 1 sincroniza apenas pedido, pagamento e caixa; estoque e demais domínios entram na Fase 2).

### 3.2 Critérios de saída da Fase 1

- [ ] Um pedido completo percorre mesa → cozinha → caixa sem papel
- [ ] Tempo pedido→KDS abaixo de 2 s (p95) medido
- [ ] Operação de 4 h com internet cortada, sem perda de dado
- [ ] Recuperação de 6 h offline em menos de 5 min
- [ ] Painel do dono exibindo tempos, faturamento e alertas
- [ ] Drill-down de qualquer indicador até o pedido em ≤ 3 toques
- [ ] Auditoria registrando todas as ações sensíveis
- [ ] Piloto: 14 dias com ≥ 98% dos pedidos processados no sistema

---

## 4. Fases 2 a 5 (resumo)

| Fase | Duração | Entrega central | Marco |
|---|---|---|---|
| **2 · Custo e controle** | 8 sem | Ficha técnica, baixa automática, CMV teórico × real, custo e margem por produto, engenharia de cardápio, inteligência de fluxo (fire time, prioridade, prazo dinâmico) | M2 |
| **3 · Financeiro** | 6 sem | Folha, custos fixos, prime cost, ponto de equilíbrio, fluxo de caixa, resultado, exportação contábil | M3 |
| **4 · Delivery** | 8 sem | Canal próprio com marca, zonas e taxa, pagamento online, entregador, rastreio | M4 |
| **5 · Escala** | 12 sem* | E-14: saúde, autoatendimento, modelos, domínio e atualização; E-15: raiz administrativa, diretório, detalhe, ciclo de vida, planos, responsáveis, recuperação e central operacional | M5 |

> \* A inclusão da E-15 acrescenta 58 pontos e exige revalidação formal do cronograma. A projeção de julho/2027 assume a mesma velocidade usada neste documento.

> **Recomendação forte:** a Fase 2 não deve ser adiada. É onde está o maior retorno financeiro do projeto — sem ficha técnica não existe custo, margem, CMV nem decisão de cardápio.

---

## 5. Equipe sugerida

| Papel | Alocação | Fases |
|---|---|---|
| Product Owner | 50% | Todas |
| Tech Lead / Arquiteto | 100% | Todas |
| Dev Backend Sênior | 100% | Todas |
| Dev Frontend Sênior | 100% | Todas |
| Dev Fullstack Pleno | 100% | A partir da Fase 1 |
| UX/UI Designer | 60% | Fases 0–2 |
| QA | 50% | A partir da S1.5 |
| DevOps / Infra | 30% | Fases 0–1, depois sob demanda |

**Time mínimo viável:** Tech Lead + 2 devs + PO parcial. Nessa configuração, a Fase 1 se estende para ~18 semanas.

---

## 6. Cerimônias

| Cerimônia | Frequência | Duração |
|---|---|---|
| Planejamento de sprint | Quinzenal | 2 h |
| Daily | Diária | 15 min |
| Refinamento | Semanal | 1 h |
| Review com o cliente | Quinzenal | 1 h |
| Retrospectiva | Quinzenal | 1 h |
| Revisão de arquitetura | Mensal | 2 h |

**Sprint de 2 semanas.** Review com o cliente é obrigatória — como não existe processo definido no estabelecimento, a validação frequente é o que impede o time de construir sobre suposição.

---

## 7. Estratégia de piloto

O piloto é a fase mais crítica do projeto. O sistema **cria** o processo do estabelecimento, então a adoção não é automática.

### 7.1 Etapas

| Etapa | Duração | Escopo | Critério de avanço |
|---|---|---|---|
| **P1 · Ensaio** | 3 dias | Loja fechada, pedidos simulados | Equipe consegue operar sem apoio |
| **P2 · Paralelo** | 5 dias | Sistema + papel simultâneos, horário de menor movimento | Zero divergência entre os dois |
| **P3 · Assistido** | 7 dias | Só sistema, com Replay presente no pico | ≥ 95% dos pedidos no sistema |
| **P4 · Autônomo** | 14 dias | Só sistema, suporte remoto | ≥ 98%, sem incidente crítico |

### 7.2 Treinamento

| Perfil | Duração | Formato |
|---|---|---|
| Garçom | 30 min | Prático, na mesa real |
| Cozinha | 45 min | Prático, no KDS real, com pedidos simulados |
| Caixa | 60 min | Prático + fechamento assistido |
| Gestor | 2 h | Painel, indicadores e interpretação |

**Material:** vídeos curtos por tarefa (≤ 90 s) e cartão de referência plastificado por posto.

### 7.3 Critério de rollback do piloto

Voltar ao papel se ocorrer: perda de pedido atribuível ao sistema, indisponibilidade > 15 min no pico, ou divergência de caixa causada pelo sistema. **O rollback precisa ser ensaiado antes do P3** — a equipe deve saber exatamente o que fazer.

---

## 8. Dependências e bloqueios

### 8.1 Bloqueios para iniciar

| # | Bloqueio | Responsável | Impacto se não resolver |
|---|---|---|---|
| B1 | Aprovação de escopo e orçamento | Cliente | Projeto não inicia |
| B2 | Definição fiscal (NFC-e/SAT) | Cliente + contador | Sistema não pode operar legalmente em produção |
| B3 | Propriedade do produto e modelo comercial | Cliente + Replay | Risco contratual sobre o ativo |
| B4 | Identidade visual da marca | Cliente | Bloqueia personalização |

### 8.2 Bloqueios por fase

| Fase | Dependência | Prazo necessário |
|---|---|---|
| 1 | Hardware do servidor local e rede da loja | Até S1.8 |
| 1 | Cardápio completo com preços | Até S1.1 |
| 1 | Teclado numérico / monitor do KDS | Até S1.5 |
| 2 | **Fichas técnicas de todos os produtos** | Iniciar já na Fase 1 |
| 2 | Lista de insumos e fornecedores | Até início da Fase 2 |
| 3 | Dados de folha e custos fixos | Até início da Fase 3 |
| 4 | Credenciais Mercado Pago | Até início da Fase 4 |

> **A carga de fichas técnicas é o item que mais costuma atrasar projetos deste tipo.** Depende inteiramente do cliente e é trabalhosa. Deve começar em paralelo à Fase 1, não quando a Fase 2 iniciar.

---

## 9. Riscos de execução

| # | Risco | Prob. | Impacto | Mitigação | Dono |
|---|---|:-:|:-:|---|---|
| R1 | Cliente não fornece fichas técnicas a tempo | **Alta** | Alto | Iniciar na Fase 1; oferecer apoio assistido; usar as 20 mais vendidas primeiro | PO |
| R2 | Sincronização estoura a estimativa | Alta | Alto | Fatiar por domínio; MVP sincroniza só pedido/pagamento | Tech Lead |
| R3 | Equipe da loja rejeita o sistema | Média | **Crítico** | Envolver garçons e cozinha no desenho; piloto assistido; treinamento prático | PO |
| R4 | Escopo cresce durante o piloto | **Alta** | Alto | Governança do ADR-013; toda demanda vira item de backlog priorizado | PO |
| R5 | Falha de hardware no piloto | Média | Alto | Equipamento reserva pré-configurado; runbook ensaiado | DevOps |
| R6 | Definição fiscal atrasa o go-live | Média | **Crítico** | Escalar B2 imediatamente; avaliar solução de terceiro | PO |
| R7 | Rede Wi-Fi instável na loja | Alta | Alto | Cabear KDS e caixa; VLAN dedicada; avaliar na visita técnica | DevOps |
| R8 | Indisponibilidade do cliente para review | Média | Médio | Agenda fixa quinzenal acordada no início | PO |
| R9 | Rotatividade de equipe na loja | Alta | Médio | Material de treinamento reutilizável; interface simples | PO |
| R10 | Concorrente lança antes (se virar produto) | Média | Médio | Priorizar diferencial de offline-first e métrica | Gestão |

---

## 10. Governança

### 10.1 Controle de mudança

Toda demanda nova durante a execução segue:

```
Demanda → é configuração existente?  ─ sim → configurar, sem custo
                 │ não
                 ▼
          vira configuração do produto? ─ sim → item de backlog, priorizado
                 │ não
                 ▼
          registrar recusa com justificativa (ADR-013)
```

### 10.2 Comunicação

| O quê | Quando | Para quem |
|---|---|---|
| Review de sprint | Quinzenal | Cliente |
| Relatório de progresso | Semanal | Cliente |
| Alerta de risco | Imediato | Cliente |
| Reunião de fase | Fim de cada fase | Cliente + gestão |

### 10.3 Critérios de aceite de fase

Cada fase só é aceita com: critérios de saída atendidos, testes de regressão verdes, RNFs verificados, documentação atualizada e homologação do cliente registrada.

---

## 11. Estimativa de esforço

| Fase | Pontos | Sprints | Semanas |
|---|---:|---:|---:|
| 0 · Fundação | 55 | 2 | 4 |
| 1 · MVP | 376 | 6 | 12 |
| 2 · Custo | 126 | 4 | 8 |
| 3 · Financeiro | 55 | 3 | 6 |
| 4 · Delivery | 68 | 4 | 8 |
| 5 · Escala | 105 | 6 | 12 |
| **Total** | **785** | **25** | **50** |

**Premissa de velocidade:** 60 pontos por sprint com a equipe da seção 5. Com o time mínimo (3 pessoas), ~40 pontos/sprint → ~62 semanas no total.

> Estimativa de esforço não é preço. A precificação depende do modelo comercial definido no bloqueio B3.

---

## 12. Estratégia de lançamento

| Fase | Público | Comunicação |
|---|---|---|
| Piloto | Só Dona Betinha | Interna |
| Estabilização | Dona Betinha por 60 dias | Interna |
| Segundo cliente | 1 estabelecimento amigo | Referência controlada |
| Comercialização | Mercado local | Depende da decisão de B3 |

**Critério para vender o produto a terceiros:** Dona Betinha operando estável por 60 dias, tempo de implantação verificado ≤ 5 dias úteis e material de treinamento pronto.

---

*Documento 09 do pacote 004_DonaBetinha. Replay Studio.*
