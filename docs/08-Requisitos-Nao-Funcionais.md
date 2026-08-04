# 08 — Requisitos Não Funcionais
## Ecossistema Nexora

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Requisitos Não Funcionais |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |

> Todo RNF aqui é **verificável**. Requisito não funcional sem número e sem forma de medir é opinião, não requisito.

---

## 1. Desempenho — RNF-PER

| ID | Requisito | Alvo | Como medir |
|---|---|---|---|
| RNF-PER-01 | Pedido confirmado até aparecer no KDS | < 2 s (p95) | Telemetria fim a fim: `order.placed` → render no KDS |
| RNF-PER-02 | Toque no KDS até retorno visual | < 300 ms (p95) | Instrumentação do cliente |
| RNF-PER-03 | Carregamento do cardápio na mesa | < 2 s em 4G (p75) | Lighthouse / RUM |
| RNF-PER-04 | Consulta ao painel do dono | < 3 s (p95) | APM do endpoint |
| RNF-PER-05 | Mapa de mesas | < 1 s (p95) | APM |
| RNF-PER-06 | Fila do KDS com 50 itens | < 500 ms | Teste de carga |
| RNF-PER-07 | Sincronização de 1.000 eventos | < 10 s | Teste de sync |
| RNF-PER-08 | Recuperação de 6 h offline (≈4.000 eventos) | < 5 min | Teste de caos |
| RNF-PER-09 | Fechamento de conta | < 2 s (p95) | APM |
| RNF-PER-10 | Peso inicial do PWA público | < 250 KB gzip | Análise de bundle no CI |

### 1.1 Capacidade

| Dimensão | Alvo por loja |
|---|---|
| Pedidos simultâneos em produção | 60 |
| Dispositivos conectados por WebSocket | 30 |
| Pedidos por hora no pico | 120 |
| Itens na fila do KDS | 100 sem degradação |
| Usuários simultâneos no cardápio | 80 |

---

## 2. Disponibilidade e resiliência — RNF-DIS

| ID | Requisito | Alvo |
|---|---|---|
| RNF-DIS-01 | Disponibilidade do servidor local em horário de operação | ≥ 99,9% |
| RNF-DIS-02 | Disponibilidade da nuvem | ≥ 99,5% |
| RNF-DIS-03 | Operação local independe totalmente da internet | 100% das funções operacionais |
| RNF-DIS-04 | Tempo de recuperação após falha do servidor local (RTO) | < 30 min com equipamento reserva |
| RNF-DIS-05 | Perda máxima de dados aceitável (RPO) | 0 para eventos confirmados localmente |
| RNF-DIS-06 | Falha de um dispositivo não afeta os demais | Verificado em teste |
| RNF-DIS-07 | Queda do WebSocket degrada para polling | ≤ 5 s de latência |
| RNF-DIS-08 | Indisponibilidade do adquirente não bloqueia a venda | Registro manual disponível |

### 2.1 Matriz de degradação

| Falha | Continua funcionando | Para de funcionar | Ação |
|---|---|---|---|
| Internet | Mesa, KDS, caixa, comanda, fechamento | Delivery, pagamento online, painel remoto | Aviso discreto; fila acumula |
| Servidor local | — (contingência limitada nos dispositivos) | Operação central | Runbook de contingência + standby |
| Um dispositivo | Todo o restante | Aquele posto | Abrir em outro aparelho |
| WebSocket | Tudo, via polling | Tempo real | Automático, com indicação visual |
| Nuvem | Toda a operação local | Painel, financeiro, delivery | Sync retoma sozinho |
| Adquirente | Toda a operação | Cartão integrado | Forma manual + conciliação |

---

## 3. Operação offline — RNF-OFF

| ID | Requisito |
|---|---|
| RNF-OFF-01 | Abertura de mesa, pedido, produção, fechamento e pagamento operam 100% offline |
| RNF-OFF-02 | Nenhum evento gerado offline pode ser perdido |
| RNF-OFF-03 | Reenvio nunca duplica registro (idempotência por `event.id`) |
| RNF-OFF-04 | `occurredAt` sempre preservado; métricas nunca usam horário de sincronização |
| RNF-OFF-05 | Sistema opera offline por ao menos 72 h sem degradação |
| RNF-OFF-06 | Estado de conexão sempre visível ao operador |
| RNF-OFF-07 | Atraso de sincronização acima de 5 min gera alerta ao gestor e à plataforma |
| RNF-OFF-08 | Conflitos registrados e revisáveis |
| RNF-OFF-09 | Painel do dono indica explicitamente a defasagem dos dados |

---

## 4. Segurança — RNF-SEG

| ID | Requisito |
|---|---|
| RNF-SEG-01 | TLS obrigatório na nuvem; TLS com certificado local na LAN |
| RNF-SEG-02 | Senhas com Argon2id; PINs com hash e salt |
| RNF-SEG-03 | Access token de 15 min; refresh rotativo com detecção de reuso |
| RNF-SEG-04 | PIN válido apenas em dispositivo registrado, na rede local |
| RNF-SEG-05 | Bloqueio após 5 tentativas de PIN por 15 min, com alerta |
| RNF-SEG-06 | Rotação obrigatória de PIN a cada 90 dias |
| RNF-SEG-07 | RLS habilitado e forçado em todas as tabelas com `tenant_id` |
| RNF-SEG-08 | Teste automatizado de isolamento entre tenants em todo PR |
| RNF-SEG-09 | Auditoria sem permissão de UPDATE/DELETE no banco |
| RNF-SEG-10 | Segredos fora do repositório; chaves de pagamento nunca no cliente |
| RNF-SEG-11 | Rate limit por IP, sessão e instalação |
| RNF-SEG-12 | Requisições de sync assinadas por HMAC |
| RNF-SEG-13 | Acesso de suporte com token de escopo curto, auditado e visível ao cliente |
| RNF-SEG-14 | Dependências verificadas em CI (SCA); vulnerabilidade crítica bloqueia deploy |
| RNF-SEG-15 | Logs não podem conter dado pessoal, token ou senha |
| RNF-SEG-16 | Servidor local sem porta exposta à internet; sync sempre por conexão de saída |

### 4.1 Modelo de ameaças resumido

| Ameaça | Vetor | Mitigação |
|---|---|---|
| Vazamento entre tenants | Query sem filtro | RLS + teste automatizado |
| Fraude de operador | Cancelamento/desconto indevido | Autorização de perfil superior + auditoria + métrica de anomalia |
| Acesso físico ao servidor | Rede da loja | Disco criptografado; sem porta exposta |
| Roubo de token | Rede insegura | TLS + expiração curta + vínculo a dispositivo |
| Pedido falso pelo QR Code | Token de mesa exposto | Token rotativo por sessão + rate limit |
| Adulteração de auditoria | Acesso ao banco | Permissões revogadas + hash encadeado (Fase 2) |

---

## 5. Privacidade e LGPD — RNF-LGP

| ID | Requisito |
|---|---|
| RNF-LGP-01 | Coleta mínima: nome, telefone e endereço apenas quando há entrega |
| RNF-LGP-02 | Base legal declarada (execução de contrato / legítimo interesse) |
| RNF-LGP-03 | Exportação dos dados do titular sob solicitação |
| RNF-LGP-04 | Exclusão/anonimização sob solicitação, preservando dado fiscal e agregado |
| RNF-LGP-05 | Anonimização automática após 24 meses sem novo pedido |
| RNF-LGP-06 | Registro de acesso a dados pessoais |
| RNF-LGP-07 | Contrato define o estabelecimento como controlador e a Replay como operadora |
| RNF-LGP-08 | Política de privacidade personalizável por estabelecimento |
| RNF-LGP-09 | Consentimento explícito para comunicação de marketing (separado do operacional) |

---

## 6. Usabilidade e acessibilidade — RNF-USA

| ID | Requisito | Alvo |
|---|---|---|
| RNF-USA-01 | Lançar um pedido simples pelo garçom | ≤ 5 toques |
| RNF-USA-02 | Avançar item no KDS | 1 toque |
| RNF-USA-03 | Alvos de toque em telas operacionais | ≥ 48 × 48 px |
| RNF-USA-04 | Contraste do KDS | ≥ 7:1 (WCAG AAA) |
| RNF-USA-05 | Legibilidade do KDS | Nome do produto legível a 1,5 m |
| RNF-USA-06 | Contraste geral | ≥ 4,5:1 (WCAG AA) |
| RNF-USA-07 | Navegação por teclado nas telas administrativas | 100% |
| RNF-USA-08 | Rótulos ARIA no cardápio público | Compatível com leitor de tela |
| RNF-USA-09 | Cardápio funcional em Android 8+ e iOS 14+ | Verificado |
| RNF-USA-10 | Nenhuma ação crítica depende só de cor | Ícone ou texto de apoio |
| RNF-USA-11 | Tempo de treinamento de um garçom novo | ≤ 15 min |
| RNF-USA-12 | Toda ação destrutiva é confirmável ou reversível | Verificado |
| RNF-USA-13 | Toda tela (entrada, hover/press, transição de estado, atualização realtime) usa os tokens de motion compartilhados | `packages/ui/src/tokens/motion.css` |

### 6.1 Motion design

Nenhuma interface é entregue "seca". Toda página, card, lista, diálogo, toggle, badge ou notificação — nos 5 apps do frontend (`web-admin`, `web-kds`, `web-menu`, `web-pos`, `web-platform`) — nasce com animação de entrada, resposta a hover/press e transição suave de qualquer mudança de estado, inclusive as que chegam por WebSocket/SignalR sem remontar o componente. A fonte única dos tokens é `packages/ui/src/tokens/motion.css` (durações `--dur-instant/fast/base/slow/slower`, curvas `--ease-standard/out/in-out`) e os utilitários prontos de `packages/ui/src/components/motion.css` (entrada padrão, entrada de diálogo, entrada de toast, flash de atualização, entrada em cascata de lista, skeleton, spinner) — nunca duração/easing declarados "solto" no CSS, e nenhuma biblioteca externa de animação (é CSS nativo, para não pesar no hardware do edge server nem competir com o orçamento de latência pedido→KDS de RNF-PER). `prefers-reduced-motion: reduce` é respeitado automaticamente pelos tokens.

---

## 7. Observabilidade — RNF-OBS

| ID | Requisito |
|---|---|
| RNF-OBS-01 | Logs estruturados em JSON com `traceId`, `tenantId`, `userId` |
| RNF-OBS-02 | Tracing distribuído (OpenTelemetry) do pedido ao KDS |
| RNF-OBS-03 | Métricas técnicas expostas em `/metrics` |
| RNF-OBS-04 | Erros capturados com contexto (sem dado pessoal) |
| RNF-OBS-05 | Health check por instalação, reportado à nuvem a cada 60 s |
| RNF-OBS-06 | Painel de saúde do parque com versão, atraso de sync e último contato |
| RNF-OBS-07 | Alerta à Replay quando uma instalação some por mais de 10 min em horário de operação |
| RNF-OBS-08 | Verificação diária de integridade dos eventos (cobertura de instrumentação = 100%) |
| RNF-OBS-09 | Retenção de logs: 30 dias quente, 12 meses frio |

### 7.1 Alertas técnicos

| Alerta | Limiar | Destino |
|---|---|---|
| Instalação sem contato | > 10 min em operação | Replay |
| Atraso de sincronização | > 5 min | Gestor + Replay |
| Fila de outbox | > 500 eventos | Replay |
| Taxa de erro da API | > 1% em 5 min | Replay |
| Latência p95 pedido→KDS | > 3 s | Replay |
| Disco do servidor local | > 80% | Replay |
| Worker de métricas parado | > 15 min | Replay |
| Falha de backup | qualquer | Replay |

---

## 8. Manutenibilidade — RNF-MAN

| ID | Requisito |
|---|---|
| RNF-MAN-01 | Cobertura de testes ≥ 70% global e ≥ 90% em `packages/domain` |
| RNF-MAN-02 | Nenhum código condicional por tenant (ADR-013), verificado no CI |
| RNF-MAN-03 | Regras de negócio em `packages/domain`, sem dependência de framework |
| RNF-MAN-04 | Toda alteração de schema por migration versionada |
| RNF-MAN-05 | Migrations compatíveis para trás por uma versão |
| RNF-MAN-06 | Contrato de API versionado; quebra exige nova versão |
| RNF-MAN-07 | Eventos versionados; consumidores toleram versões antigas |
| RNF-MAN-08 | Lint e formatação obrigatórios no CI |
| RNF-MAN-09 | Dependências atualizadas trimestralmente |
| RNF-MAN-10 | Documentação atualizada na mesma PR da mudança |

---

## 9. Portabilidade e implantação — RNF-IMP

| ID | Requisito |
|---|---|
| RNF-IMP-01 | Instalação do servidor local por script único, < 30 min |
| RNF-IMP-02 | Instalação sem intervenção manual em banco ou configuração |
| RNF-IMP-03 | Atualização do parque com janela configurável e rollback automático |
| RNF-IMP-04 | Deploy da nuvem sem downtime |
| RNF-IMP-05 | Rollback da nuvem em < 5 min |
| RNF-IMP-06 | Backup diário automático (nuvem e loja) |
| RNF-IMP-07 | Restauração testada trimestralmente |
| RNF-IMP-08 | Provisionar novo estabelecimento em ≤ 5 dias úteis (incluindo carga de dados) |

---

## 10. Compatibilidade — RNF-CMP

| Alvo | Suporte |
|---|---|
| Navegadores | Chrome/Edge 110+, Safari 15+, Firefox 110+ |
| Android | 8.0+ |
| iOS | 14+ |
| Resolução mínima (mesa/garçom) | 360 × 640 |
| Resolução mínima (KDS) | 1280 × 720 |
| Resolução mínima (caixa) | 1366 × 768 |
| Servidor local | Linux x86-64, Docker 24+ |
| Entrada do KDS | Teclado numérico USB ou bump bar |

---

## 11. Restrições

| # | Restrição | Origem |
|---|---|---|
| C1 | A operação não pode depender de internet | Cliente [FATO] |
| C2 | Nenhum código específico por cliente | Diretriz de produto |
| C3 | Cliente do salão não pode ser obrigado a instalar app | Adoção |
| C4 | Cozinha não pode depender de digitação livre | Ambiente |
| C5 | Dados hospedados no Brasil | LGPD e latência |
| C6 | Emissão fiscal **[PENDÊNCIA]** | Cliente + contador |
| C7 | Orçamento e prazo **[PENDÊNCIA]** | Cliente |

---

## 12. Rastreabilidade RNF → verificação

| Grupo | Como é verificado | Onde |
|---|---|---|
| Desempenho | Teste de carga k6 + RUM | Doc. 10, §5 |
| Offline | Teste de caos (corte de rede) | Doc. 10, §6 |
| Segurança | Teste de isolamento + SCA + revisão | Doc. 10, §7 |
| Usabilidade | Teste com usuário real no piloto | Doc. 10, §8 |
| Observabilidade | Verificação de instrumentação em cada PR | Doc. 10, §3 |
| Implantação | Ensaio de instalação em ambiente limpo | Doc. 09, marco M4 |

---

*Documento 08 do pacote 004_DonaBetinha. Replay Studio.*
