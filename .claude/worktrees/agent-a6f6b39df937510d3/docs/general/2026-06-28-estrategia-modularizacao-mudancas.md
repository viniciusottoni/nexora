# Estratégia de modularização das mudanças — 2026-06-28

Revisão de arquitetura sênior sobre o estado atual do código (297 arquivos Dart, 460 C#) e modularização dos 15 pedidos em três destinos: **US-016** (recorte trial), **EPIC-018** (novo, restante) e **EPIC-017** (rastreio — só criar após aprovação).

Documentos gerados nesta rodada:
- `docs/user stories/EPIC-018-qualidade-localizacao-economia-hardening/README.md` — épico novo.
- `US-016` — adendo de UTC do trial (seção 21).
- Este memo — matriz + evidências + propostas para EPIC-017.

---

## 1. Evidências de código (estado atual → estado futuro)

| Tema | Estado atual (evidência) | Estado futuro |
|---|---|---|
| UTC backend | `DateTime.Now` direto = **0** ocorrências (bom). Mas entidades de domínio usam `DateTime.UtcNow` direto (`BaseEntity`, `Quest`, `HunterProgression`, etc.), contrariando a convenção "tudo via `IDateTimeService`" do CLAUDE.md → testabilidade/consistência | Relógio injetado; UTC garantido e auditável |
| UTC Flutter | **7** usos de `DateTime.now()` (`auth_interceptor`, `notification_inbox_page`, `quest_execution_*`, `exercise_execution_page`) | Nenhuma decisão de negócio por relógio local; UTC do backend |
| Moeda | `revenue_cat_service` usa `storeProduct.priceString` (localizado pela loja ✓), porém `pricingFooter` fixa **"Preços em BRL" / "Prices in BRL"** nos 4 idiomas | Sem moeda fixa; tudo derivado do `StoreProduct` |
| Onboarding equipamento | l10n `onboardingEquipmentTitle/None/Dumbbells…` e DTO `equipmentAvailable` **existem mas estão órfãos** (nenhuma tela/US os usa) | Passo de equipamento ligado à geração (ADR-012) |
| Configurações | Tiles **"Contato", "FAQ", "Sobre" sem `onTap`** (botões mortos). Faltam gerenciar assinatura, termos/privacidade, restaurar compras, versão | Tela 100% funcional + ticket |
| Avaliação Play Store | Nenhum `in_app_review` no código | Solicitação pós-assinatura, dentro da quota Google |
| Loja/Inventário | `ShopController.purchase` é **mock** (ADR-022): sem dedução de Gold, sem IAP, sem slots | IAP de consumíveis/slots via RevenueCat + concessão idempotente por webhook |
| RBAC | `AdminExercisesController` tem só `[Authorize]` → **qualquer usuário autenticado acessa endpoints admin**. ADR-022 confirma "backend não possui RBAC" | Política `Admin`; pré-requisito do EPIC-017 |
| Rate limiting | **Ausente** (`AddRateLimiter` não existe). Pipeline só tem `UseHttpsRedirection` condicional | Rate limit global + estrito em auth |
| CORS / headers | Sem `AddCors`/`UseCors`; sem HSTS/CSP/X-Frame | CORS para o admin React + security headers |
| Hangfire | `UseHangfireDashboard()` só em Development (ok), mas sem filtro de autorização para uso futuro em prod | Dashboard protegido por RBAC |
| Presença/online | **Inexistente** (nenhum `lastSeen`/`heartbeat`/`presence`) | Sinal de presença para o admin (EPIC-017) |
| GIFs de exercício | Catálogo serve `0001-360.gif`… localmente (`seed-data/exercises`) | CDN (R2) + lazy/preload (performance) |

---

## 2. Matriz de alocação (15 pedidos)

| # | Pedido | Destino |
|---|---|---|
| 1 | Datas em UTC, revisar todo o código | **US-016** (trial) + **US-172** (cross-cutting) |
| 2 | Valores na moeda da Play Store | **US-173** |
| 3 | Sugestão de itens e valores | **US-178** |
| 4 | Onboarding: corpo / livres / máquinas | **US-174** |
| 5 | Configurações ter todos | **US-175** |
| 6 | Avaliação na Play Store após assinar | **US-177** |
| 7 | RevenueCat: itens comprados + slots | **US-179** (+ ADR-023) |
| 8 | Componentização (Clean Arch/DRY/KISS) | **US-182** |
| 9 | Escalabilidade | **US-183** + EPIC-017 (US-192) |
| 10 | Performance | **US-184** + EPIC-017 (US-192) |
| 11 | Segurança (achar todos os gaps) | **US-180 + US-181** + EPIC-017 (US-191) |
| 12 | Recursos órfãos / ajustes | **US-185** |
| 13 | Admin: usuários online | **EPIC-017 — US-186 (proposta)** |
| 14 | Config funcional + abrir ticket | **US-175 + US-176** + EPIC-017 (US-189) |
| 15 | Varredura de segurança no código | **US-180/US-181** |

EPIC-018 reserva **US-172 a US-185**. EPIC-017 já reserva US-158–US-171; as propostas abaixo seguem a partir de **US-186**.

---

## 3. Propostas de RASTREIO para a EPIC-017 — aguardando aprovação (NÃO criadas ainda)

Como o sistema "vai ficar" depois do EPIC-018, o site admin precisa enxergar as novas dimensões. Sugiro adicionar ao EPIC-017:

| ID | Título proposto | Por quê | Habilitado por |
|---|---|---|---|
| US-186 | **Usuários online em tempo real (presença)** | Pedido #13: saber quem abriu e não fechou. Exige sinal de presença (heartbeat/sessão ativa) que hoje não existe | Backend de presença (Redis TTL) |
| US-187 | Receita de consumíveis/IAP e slots | Acompanhar a nova economia real (US-179) separada da assinatura | US-179 |
| US-188 | Funil assinatura → avaliação Play Store | Medir conversão da solicitação de avaliação (US-177) | US-177 |
| US-189 | Tickets por categoria (bug/dúvida/sugestão) | Estende US-162/US-163 com a taxonomia criada no app (US-176) | US-176 |
| US-190 | Distribuição de equipamento no onboarding | Entender base instalada (peso corporal/livres/máquinas) para conteúdo e geração | US-174 |
| US-191 | Painel de segurança: rate-limit hits, negações RBAC, brute force | Estende US-165 com os sinais que passam a existir após US-180/US-181 | US-180/US-181 |
| US-192 | SLOs de performance e escalabilidade (p95, erro, saturação) | Tornar visíveis as metas de US-183/US-184 | US-183/US-184 |

Detalhe técnico do US-186 (presença): registrar `lastSeenAt` por sessão em Redis com TTL (ex.: 60s, renovado por heartbeat leve ou por requisições autenticadas); "online" = chave viva. Evita varredura no Postgres e escala horizontalmente. O encerramento explícito (logout/app em background) expira a chave.

**Ação solicitada:** aprove (ou ajuste) esta lista para eu criar as US no `EPIC-017/README.md` e os arquivos `US-186..US-192`.

---

## 4. ADRs novos sugeridos (a criar junto das US correspondentes)

- **ADR-023 — IAP de consumíveis e slots de inventário (RevenueCat):** produtos não-assinatura, validação por webhook, concessão idempotente por `transaction_id`, sem crédito no cliente. Substitui a compra mock do ADR-022.
- **ADR-024 — RBAC e autorização administrativa:** claim/role de admin, política `Admin`, separação de autenticação admin (alinha com EPIC-017 §7).

---

## 5. Ordem recomendada de execução

1. **US-180 (RBAC)** e **US-181 (hardening)** — desbloqueiam o EPIC-017 com segurança.
2. **US-172/US-173** — localização (baixo risco, alto valor de qualidade).
3. **US-174/US-175/US-176** — onboarding equipamento, configurações e ticket (este último alimenta o admin).
4. **US-178/US-179 (+ADR-023)** — economia real.
5. **US-182/US-183/US-184** — qualidade de frontend, escalabilidade, performance.
6. **US-185** — saneamento de órfãos.
7. **US-177** — avaliação Play Store.
8. Após aprovação: **US-186..US-192** no EPIC-017.
