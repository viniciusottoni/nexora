# 02 — Arquitetura Técnica
## Ecossistema Dona Betinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Arquitetura Técnica |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Depende de** | `01-PRD-Especificacao-Funcional.md` |
| **Decisões formais em** | `06-ADRs-Decisoes-Arquiteturais.md` |

---

## 1. Forças que moldam a arquitetura

Cinco requisitos, todos confirmados na descoberta, determinam praticamente todas as decisões técnicas:

| # | Força | Consequência arquitetural |
|---|---|---|
| **F1** | A operação não pode parar sem internet | **Local-first**: servidor na loja é autoridade operacional |
| **F2** | Tudo precisa ser medido, com rastreabilidade | **Event sourcing seletivo**: log de eventos append-only como fonte das métricas |
| **F3** | Produto replicável em N estabelecimentos | **Multi-tenant** com isolamento forte desde o dia 1 |
| **F4** | Toda camada web personalizável | **Theming e configuração em runtime**, nunca build por cliente |
| **F5** | Pedido precisa chegar ao KDS em < 2s | **Realtime local** via WebSocket, sem round-trip à nuvem |

> As forças F1 e F3 são as caras. Ambas precisam estar na fundação — retrofit de local-first ou de multi-tenancy significa reescrever o núcleo.

---

## 2. Visão geral da topologia

```
                          ┌───────────────────────────────────────┐
                          │              NUVEM                    │
                          │  ┌─────────────────────────────────┐  │
   Internet               │  │  API Cloud (NestJS)             │  │
   ─────────────────────► │  │  · Sync gateway                 │  │
   Cliente delivery       │  │  · Painel do dono / BI          │  │
   Gestor (celular)       │  │  · Financeiro · Estoque         │  │
   Admin plataforma       │  │  · Admin plataforma             │  │
                          │  └────────────┬────────────────────┘  │
                          │  ┌────────────▼────────────────────┐  │
                          │  │  PostgreSQL (multi-tenant, RLS) │  │
                          │  │  Redis · Object Storage         │  │
                          │  └─────────────────────────────────┘  │
                          └───────────────▲───────────────────────┘
                                          │  HTTPS · sync bidirecional
                                          │  fila de eventos idempotente
        ══════════════════════════════════╪═══════════════════════════
                                          │
                          ┌───────────────┴───────────────────────┐
                          │        LOJA (rede local)              │
                          │  ┌─────────────────────────────────┐  │
                          │  │  Edge Server (mini-PC + Docker) │  │
                          │  │  · API Local (NestJS)           │  │
                          │  │  · WebSocket Gateway            │  │
                          │  │  · PostgreSQL local             │  │
                          │  │  · Sync Worker (outbox/inbox)   │  │
                          │  └───────┬─────────────────────────┘  │
                          │          │ LAN / Wi-Fi                │
                          │  ┌───────┼───────┬────────┬────────┐  │
                          │  ▼       ▼       ▼        ▼        │  │
                          │ Mesa   Garçom   KDS     Caixa      │  │
                          │ (PWA)  (PWA)  (kiosk)  (desktop)   │  │
                          └───────────────────────────────────────┘
```

### 2.1 Divisão de responsabilidade

| Domínio | Onde roda | Autoridade | Justificativa |
|---|---|---|---|
| Pedido, mesa, comanda, KDS, caixa | **Local** | Local | Não pode parar (F1); latência (F5) |
| Cardápio e configuração | Nuvem (origem) → replicado no local | Nuvem | Editado pela gestão, lido pela operação |
| Estoque e ficha técnica | Nuvem (origem) → saldo espelhado local | Nuvem | Baixa gerada localmente, consolidada na nuvem |
| Financeiro | **Nuvem** | Nuvem | Não é operação crítica de tempo real |
| Painel do dono / BI | **Nuvem** | Nuvem | Consolidação multi-período |
| Delivery e pagamento online | **Nuvem** | Nuvem | Depende de internet por natureza |
| Plataforma e provisionamento | **Nuvem** | Nuvem | — |

> **Regra de ouro da autoridade:** um dado tem **um único dono**. Cardápio é editado na nuvem e apenas lido no local; pedido é criado no local e apenas lido na nuvem. Onde isso não é possível (saldo de estoque), aplica-se reconciliação explícita — nunca escrita concorrente livre.

---

## 3. Stack tecnológica decidida

> Decisões formalizadas em ADR-001 a ADR-014. Aqui está o resultado consolidado.

### 3.1 Visão geral

| Camada | Tecnologia | Motivo em uma linha |
|---|---|---|
| **Linguagem** | TypeScript (todo o stack) | Um só time, tipos compartilhados entre back e front |
| **Monorepo** | pnpm workspaces + Turborepo | Código compartilhado entre local, nuvem e apps |
| **Backend** | NestJS (Node 22 LTS) | Estrutura modular explícita; o mesmo código roda local e na nuvem |
| **ORM** | Prisma | Migrations versionadas, tipagem forte, produtividade alta |
| **Banco** | PostgreSQL 16 | RLS nativo para multi-tenant; JSONB para configuração; robusto offline |
| **Cache/Fila** | Redis (BullMQ) | Jobs de sync, agendamentos, rate limit |
| **Realtime** | Socket.IO sobre WebSocket | Reconexão automática, salas por tenant/praça |
| **Frontend** | React 18 + Vite + TypeScript | PWA maduro, ecossistema, curva de time |
| **Estado servidor** | TanStack Query | Cache, revalidação, offline-friendly |
| **Estado local** | Zustand | Simples, sem boilerplate |
| **Offline no cliente** | Dexie (IndexedDB) + Service Worker (Workbox) | Fila de ações e cache de cardápio |
| **UI** | Tailwind CSS + design tokens por tenant | Theming em runtime via CSS custom properties |
| **Auth** | JWT (access curto + refresh) + PIN para operação | Perfis distintos exigem métodos distintos |
| **Armazenamento** | Object storage S3-compatível | Fotos de produto, logos, exportações |
| **Containers** | Docker + Docker Compose (loja) | Instalação padronizada e replicável |
| **Observabilidade** | OpenTelemetry + Sentry + logs estruturados (pino) | Suporte remoto a N instalações |
| **Testes** | Vitest, Supertest, Playwright, k6 | Pirâmide completa (doc. 10) |
| **CI/CD** | GitHub Actions | Build, teste, publicação de imagens, migrations |

### 3.2 Estrutura do monorepo

```
dona-betinha/
├── apps/
│   ├── api-cloud/          NestJS — nuvem (sync, BI, financeiro, admin)
│   ├── api-edge/           NestJS — servidor da loja (pedido, KDS, caixa)
│   ├── web-admin/          React — gestão, financeiro, painel do dono
│   ├── web-pos/            React — caixa e garçom (PWA)
│   ├── web-kds/            React — cozinha (modo quiosque)
│   ├── web-menu/           React — cardápio da mesa e delivery (PWA público)
│   └── web-platform/       React — painel da Replay (tenants, monitoramento)
├── packages/
│   ├── domain/             Entidades, máquinas de estado, regras puras
│   ├── events/             Catálogo de eventos, schemas, validação (Zod)
│   ├── db/                 Prisma schema, migrations, seeds
│   ├── sync/               Motor de sincronização (outbox/inbox, idempotência)
│   ├── contracts/          Tipos e DTOs compartilhados API↔front
│   ├── ui/                 Design system, theming por tenant
│   ├── metrics/            Derivação de indicadores a partir de eventos
│   └── config/             ESLint, TS, Tailwind, presets
├── infra/
│   ├── edge/               docker-compose da loja, scripts de instalação
│   ├── cloud/              IaC, manifests, migrations
│   └── scripts/            Provisionamento de tenant, carga de cardápio
└── docs/                   Este pacote
```

> **Ponto central:** `packages/domain` contém as regras de negócio puras, sem dependência de framework, e é usado **igualmente** pela API local e pela nuvem. É isso que garante que o comportamento offline seja idêntico ao online.

---

## 4. Multi-tenancy

### 4.1 Modelo escolhido: banco único, schema compartilhado, isolamento por RLS

Todas as tabelas de negócio carregam `tenant_id`. O isolamento é imposto pelo **PostgreSQL Row Level Security**, não pela aplicação.

```sql
ALTER TABLE "order" ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON "order"
  USING (tenant_id = current_setting('app.tenant_id')::uuid);
```

Cada requisição abre a conexão definindo o tenant do token:

```sql
SET LOCAL app.tenant_id = '<uuid do tenant autenticado>';
```

**Por que RLS e não filtro na aplicação:** um `WHERE tenant_id` esquecido em uma query vaza dados entre estabelecimentos. Com RLS, o banco recusa — o erro passa a ser impossível por construção, não por disciplina.

**Por que não um banco por tenant:** custo operacional de migrations, backup e monitoramento cresce linearmente e inviabiliza a Fase 5. Revisar apenas se um cliente exigir isolamento físico contratual.

### 4.2 No servidor local

O edge server é **single-tenant por instalação** — a loja só tem os próprios dados. Mesmo assim, o `tenant_id` é mantido em todas as tabelas para que os eventos sincronizem sem transformação e o código seja idêntico nos dois lados.

### 4.3 Resolução do tenant

| Contexto | Como resolve |
|---|---|
| Web público (cardápio/delivery) | Domínio ou subdomínio → tenant |
| Aplicações internas | Claim `tenant_id` no JWT |
| Edge server | Variável de ambiente fixada na instalação |
| Admin de plataforma | Token com escopo especial + registro de auditoria obrigatório |

---

## 5. Personalização e white-label

**Princípio: nunca gerar build por cliente.** Um único artefato serve todos os estabelecimentos; a identidade é carregada em runtime.

### 5.1 Como funciona

```
1. App carrega  →  GET /v1/public/branding?host=cardapio.donabetinha.com.br
2. Resposta     →  { cores, logo, fontes, textos, ícones, manifest }
3. App aplica   →  CSS custom properties no :root + manifest PWA dinâmico
```

```css
:root {
  --brand-primary:   #C1121F;
  --brand-secondary: #669BBC;
  --brand-surface:   #FDF0D5;
  --brand-font:      'Inter', sans-serif;
  --brand-radius:    12px;
}
```

### 5.2 O que é personalizável

| Dimensão | Itens |
|---|---|
| Marca | Logo (claro/escuro), cores, tipografia, raio de borda, favicon |
| PWA | Nome, ícone, splash, cor de tema, manifest gerado por tenant |
| Conteúdo | Nome, descrição, endereço, horários, redes sociais |
| Textos | Boas-vindas, confirmação, agradecimento, termos, política |
| Cardápio | Categorias, produtos, fotos, descrições, preços |
| QR Code | Arte com a marca do estabelecimento |
| Notificações | Modelos de mensagem ao cliente |

### 5.3 Governança

Toda solicitação de cliente recebe uma de três respostas: **(a)** já é configurável; **(b)** vira configuração nova do produto, beneficiando todos; **(c)** não entra. **Código específico por cliente é proibido** — é o que destrói produtos deste tipo (ver ADR-013).

---

## 6. Sincronização local ↔ nuvem

O componente mais delicado da arquitetura. Detalhamento de eventos no documento 04.

### 6.1 Padrão: outbox/inbox com log de eventos append-only

```
LOJA                                          NUVEM
─────────────────────────────────────────────────────────────
Transação de negócio                    
  ├─ grava estado (order, order_item)   
  └─ grava evento no outbox  ────┐      
                                 │      
Sync Worker (a cada 2s)          │      
  ├─ lê outbox por sequência ────┘      
  ├─ envia lote (HTTPS, gzip) ─────────► POST /v1/sync/push
  │                                       ├─ valida assinatura + tenant
  │                                       ├─ deduplica por event_id
  │                                       ├─ aplica em ordem de sequência
  │                                       └─ responde último aceito
  ├─ marca outbox como sincronizado ◄────┘
  │
  └─ GET /v1/sync/pull?cursor=N ────────► retorna eventos de nuvem
      (cardápio, config, preços, metas)
```

### 6.2 Garantias

| Garantia | Como é obtida |
|---|---|
| **Nada se perde** | Outbox persistido na mesma transação do estado (padrão transactional outbox) |
| **Nada duplica** | `event_id` UUID gerado na origem; nuvem faz upsert idempotente |
| **Ordem preservada** | Sequência monotônica por instalação (`device_seq`) |
| **Horário correto** | `occurred_at` (origem) separado de `recorded_at` (nuvem) |
| **Retomada automática** | Cursor persistido; reconexão continua de onde parou |
| **Sem perda em falha parcial** | Lote só é confirmado após persistência na nuvem |

### 6.3 Direção do fluxo por domínio

| Dado | Direção | Conflito possível? |
|---|---|---|
| Pedido, item, pagamento, caixa | Loja → Nuvem | Não (loja é dona) |
| Cardápio, preços, configuração, metas | Nuvem → Loja | Não (nuvem é dona) |
| Usuários e permissões | Nuvem → Loja | Não |
| Saldo de estoque | Ambos | **Sim** — ver 6.4 |

### 6.4 Resolução de conflito

O único conflito real é o saldo de estoque: a loja dá baixa offline enquanto a nuvem registra uma entrada de compra.

**Solução: não sincronizar saldo, sincronizar movimentos.** Cada baixa e cada entrada é um movimento com identidade própria. O saldo é sempre **derivado** da soma dos movimentos. Assim, não há conflito — há apenas ordem de aplicação.

Para os demais casos (edição concorrente do mesmo registro), vale RN-019: prevalece o menor `occurred_at`; empate resolve pela origem local; o descarte fica registrado para revisão do gestor.

### 6.5 Parâmetros operacionais

| Parâmetro | Valor |
|---|---|
| Intervalo de push | 2 s (imediato quando há evento novo) |
| Intervalo de pull | 30 s |
| Tamanho do lote | 500 eventos ou 1 MB |
| Retry | Backoff exponencial: 2s, 4s, 8s… teto 5 min |
| Alerta de atraso | > 5 min → gestor e plataforma |
| Retenção do outbox sincronizado | 30 dias local |

---

## 7. Comunicação em tempo real

### 7.1 WebSocket local

```
Cliente conecta  →  wss://edge.local/rt?token=<jwt>
Servidor associa →  salas: tenant:{id}, station:{id}, table:{id}, role:{papel}

Evento de negócio → emite para as salas relevantes:
  order.placed        → station:*, role:cashier, table:{id}
  order.item.ready    → role:waiter, table:{id}
  product.unavailable → todos
```

**Fallback:** se o WebSocket cair, o cliente faz polling a cada 5 s. A cozinha nunca pode depender de uma única via.

### 7.2 Push de navegador

Para o gestor fora da loja e para o cliente de delivery, via Web Push (VAPID). Enviado pela nuvem, não pelo edge.

---

## 8. Segurança

| Camada | Medida |
|---|---|
| **Transporte** | TLS obrigatório na nuvem; na LAN, TLS com certificado local (mkcert/ACME interno) |
| **Autenticação** | JWT access 15 min + refresh 30 dias; PIN de 4–6 dígitos vinculado a dispositivo registrado |
| **Autorização** | RBAC por tenant, verificado em guard do NestJS + RLS no banco |
| **Isolamento** | RLS obrigatório em todas as tabelas com `tenant_id` |
| **Segredos** | Variáveis de ambiente; chaves de pagamento nunca no cliente |
| **Auditoria** | Tabela append-only, sem UPDATE/DELETE (revogado por permissão de banco) |
| **Rate limit** | Por IP e por tenant nas rotas públicas |
| **Sync** | Cada edge server tem par de chaves; requisições assinadas (HMAC) |
| **Acesso de suporte** | Token de escopo especial, expiração curta, registro obrigatório e visível ao cliente |
| **Dados de cliente final** | Minimização — só nome, telefone e endereço; sem dado sensível |
| **Backup** | Nuvem: diário com retenção 30 dias. Loja: dump local diário + cópia na nuvem |

### 8.1 LGPD

| Requisito | Implementação |
|---|---|
| Base legal | Execução de contrato (pedido) e legítimo interesse (operação) |
| Minimização | Coleta apenas o necessário para entregar o pedido |
| Titular | Endpoints de exportação e exclusão de dados do cliente final |
| Retenção | Dados de cliente final: 24 meses sem novo pedido → anonimização |
| Anonimização | Pedido histórico mantém dados agregados sem identificação pessoal |
| Registro | Log de acesso a dados pessoais |
| Operador/controlador | O estabelecimento é controlador; a Replay é operadora — **exige cláusula contratual** |

---

## 9. Infraestrutura da loja (edge)

### 9.1 Hardware de referência

| Item | Especificação mínima | Recomendado |
|---|---|---|
| Servidor local | Mini-PC, 4 núcleos, 8 GB RAM, SSD 256 GB | 8 núcleos, 16 GB, SSD NVMe 512 GB |
| Energia | Nobreak 600 VA | Nobreak 1200 VA |
| Rede | Roteador com VLAN para operação | Roteador + AP dedicado à área operacional |
| KDS | Monitor 21"+ com teclado numérico USB | Tela touch industrial + bump bar |
| Caixa | Desktop ou notebook | Terminal com impressora não fiscal |
| Contingência | — | Segundo mini-PC pré-configurado (cold standby) |

### 9.2 Instalação

```yaml
# infra/edge/docker-compose.yml (resumo)
services:
  postgres:   # PostgreSQL 16, volume persistente, backup diário via cron
  redis:      # fila de sync e cache
  api-edge:   # NestJS: API + WebSocket
  web:        # Nginx servindo PWAs (pos, kds, menu)
  sync:       # worker de sincronização
  watchtower: # atualização controlada de imagens
```

Instalação por script único: `./install.sh --tenant=<id> --token=<token>` → provisiona containers, registra a instalação na nuvem, baixa configuração e cardápio, gera certificados locais.

### 9.3 Atualização do parque

Atualizações são **puxadas** pelo edge, nunca empurradas cegamente:
janela configurável (fora do horário de operação) → download → migration → health check → rollback automático se falhar.

---

## 10. Infraestrutura de nuvem

| Componente | Escolha | Observação |
|---|---|---|
| Aplicação | Containers em plataforma gerenciada | Escala horizontal por CPU/fila |
| Banco | PostgreSQL gerenciado | Backup automático, PITR, réplica de leitura na Fase 3 |
| Cache/fila | Redis gerenciado | |
| Arquivos | Object storage S3-compatível + CDN | Fotos de produto e logos |
| DNS/TLS | Certificado curinga + emissão por domínio de cliente | Fase 5 |
| Observabilidade | OpenTelemetry → coletor; Sentry para erros | Painel de saúde por instalação |
| Região | Brasil (latência e LGPD) | |

---

## 11. Desempenho — alvos e como alcançar

| Alvo | Valor | Como |
|---|---|---|
| Pedido → KDS | < 2 s (p95) | Trajeto 100% local + WebSocket |
| Toque no KDS → resposta visual | < 300 ms | Atualização otimista + confirmação |
| Carregamento do cardápio (mesa) | < 2 s em 4G | SSR/pré-render, imagens otimizadas, cache |
| Consulta ao painel do dono | < 3 s | Tabelas de agregação pré-calculadas |
| Sincronização de 1.000 eventos | < 10 s | Lote comprimido, upsert em massa |

**Estratégia de BI:** eventos brutos não são consultados diretamente pelo painel. Um worker mantém tabelas de agregação (por hora, dia, produto, canal, operador). O painel lê agregado; o detalhamento consulta o evento apenas quando o usuário abre o número (RF-BI-11).

---

## 12. Riscos técnicos e mitigação

| # | Risco | Prob. | Impacto | Mitigação |
|---|---|:-:|:-:|---|
| T1 | Falha física do servidor local no pico | Média | Crítico | Cold standby pré-configurado + cache de contingência nos dispositivos + runbook |
| T2 | Divergência de dados após sincronização longa | Média | Alto | Movimentos em vez de saldos; verificação de integridade diária; conciliação assistida |
| T3 | Wi-Fi instável na área operacional | Alta | Alto | Rede cabeada para KDS e caixa; VLAN dedicada; polling de fallback |
| T4 | Vazamento entre tenants | Baixa | Crítico | RLS + testes automatizados de isolamento em cada PR |
| T5 | Crescimento do event store degradar consultas | Média | Médio | Particionamento por mês; agregados; arquivamento após 24 meses |
| T6 | Complexidade do sync atrasar o MVP | **Alta** | Alto | Fatiar: Fase 1 sincroniza só pedido/pagamento; demais domínios depois |
| T7 | Deriva de versão entre lojas | Média | Médio | Atualização automática com janela + monitoramento de versão por instalação |
| T8 | Dependência de integração de pagamento | Média | Médio | Abstrair provedor atrás de interface; registro manual como fallback |

---

## 13. Sequência de construção recomendada

```
Sprint 0   Monorepo, CI, Prisma, RLS, autenticação, tenant, docker-compose edge
Sprint 1   Domínio de pedido + máquina de estados + event store (packages/domain)
Sprint 2   API local: cardápio, mesa, pedido, WebSocket
Sprint 3   PWA de mesa e PWA de garçom
Sprint 4   KDS com teclado numérico e cronômetro
Sprint 5   Caixa, pagamento e fechamento
Sprint 6   Motor de sincronização (outbox/inbox) + nuvem
Sprint 7   Agregados de métrica + painel do dono v1
Sprint 8   Endurecimento, piloto, observabilidade
```

> A ordem privilegia **fechar o fluxo operacional completo antes da sincronização**. O sync é a peça mais arriscada — só faz sentido construí-lo quando existe fluxo real gerando eventos reais para sincronizar.

---

*Documento 02 do pacote 004_DonaBetinha. Replay Studio.*
