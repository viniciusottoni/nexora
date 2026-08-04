# US-004 · Autenticacao e perfis de acesso

|  |  |
|---|---|
| **Épico** | [E-00 · Fundacao da Plataforma](./README.md) |
| **Fase** | 0 — Fundação da plataforma |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-IAM-01, RF-IAM-02, RF-IAM-03, RF-IAM-04, RF-IAM-06, RF-IAM-07 |
| **Regras de negócio** | RN-004, RN-011 |
| **ADRs** | ADR-014, ADR-023 |
| **Eventos** | EVT-070, EVT-071, EVT-072 |
| **Aplicações** | api-cloud, api-edge, web-admin, web-pos, web-kds |
| **Autoridade do dado** | Nuvem (cadastro) → replicado no local (validação) |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que cada pessoa da equipe acesse apenas o que corresponde à sua função,
> **para** que a operação seja rastreável e que ações sensíveis não aconteçam sem autorização.

## 2. Contexto e motivação

Perfis distintos exigem métodos de autenticação distintos — é o núcleo do ADR-014. Um pizzaiolo com as mãos sujas de farinha não digita uma senha de doze caracteres a cada troca de turno; um gestor que acessa financeiro não entra com PIN de quatro dígitos.

Daí a divisão: **e-mail e senha (com 2FA opcional) para gestão**, **PIN em dispositivo registrado para operação**. E, para a camada de ações sensíveis, um terceiro mecanismo: autorização pontual, em que o gerente digita o PIN no próprio dispositivo do operador, sem trocar de sessão (doc. 05, seção 2.2).

A autorização propriamente dita é RBAC verificado por `[Authorize(Roles = "...")]`/`IAuthorizationHandler` do ASP.NET Core **e** RLS no banco — duas camadas, porque a primeira erra.

## 3. Escopo

### 3.1 Dentro desta história

- Login por e-mail e senha com access token de 15 min e refresh de 30 dias
- Segundo fator opcional para gestor e obrigatório para admin de plataforma
- Login operacional por PIN de 4 a 6 dígitos, vinculado a dispositivo registrado, com sessão de 8 horas
- Bloqueio progressivo por tentativas incorretas de PIN
- Papéis configuráveis por tenant com conjunto de permissões
- Autorização pontual de ação sensível via `X-Authorization-Token`
- Encerramento de sessão inativa por parâmetro configurável
- Política de autorização do ASP.NET Core (`IAuthorizationHandler`), alinhada às políticas RLS

### 3.2 Fora desta história

- Federação de identidade (SSO corporativo)
- Biometria
- Registro de dispositivos (US-005, história irmã)
- Trilha de auditoria completa (US-090)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Autenticação e autorização

  Cenário: Login de gestor
    Dado um usuário com e-mail e senha válidos
    Quando efetuar login
    Então deve receber access token de 15 minutos e refresh de 30 dias
    E o token deve conter tenantId, storeId, papéis e permissões
    E o evento user.authenticated deve ser emitido

  Cenário: Login operacional por PIN
    Dado um dispositivo registrado e um operador com PIN válido
    Quando digitar o PIN
    Então deve receber sessão de 8 horas vinculada ao deviceId
    E a sessão não deve ser válida em outro dispositivo

  Cenário: PIN em dispositivo não registrado
    Dado um operador com PIN válido
    E um dispositivo que não está registrado no estabelecimento
    Quando tentar autenticar
    Então o acesso deve ser recusado com 403
    E o gestor deve ser notificado da tentativa

  Cenário: Bloqueio por tentativas
    Dado 5 tentativas de PIN incorretas no mesmo dispositivo
    Quando tentar novamente
    Então o acesso deve ser bloqueado por 15 minutos
    E o gestor deve ser notificado

  Cenário: Autorização de ação sensível
    Dado um operador sem permissão de cancelar item já iniciado
    Quando solicitar o cancelamento
    Então deve ser pedido o PIN de um perfil superior no mesmo dispositivo
    E, autorizado, deve ser emitido um authorizationToken válido por 120 segundos
    E a ação deve registrar quem executou e quem autorizou

  Cenário: Token de autorização expirado
    Dado um authorizationToken emitido há mais de 120 segundos
    Quando for apresentado para executar a ação
    Então deve ser recusado com 403
    E uma nova autorização deve ser exigida

  Cenário: Encerramento de sessão inativa
    Dado o parâmetro de inatividade configurado em 30 minutos
    E um terminal de caixa sem interação há 31 minutos
    Quando houver nova interação
    Então deve ser exigida nova autenticação

  Cenário: Permissão negada por padrão
    Dado um papel novo criado sem permissões atribuídas
    Quando o usuário tentar qualquer ação
    Então todas devem ser negadas até que a permissão seja concedida explicitamente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | O token carrega `sub` e `did`; ambos são propagados a todo evento emitido |
| RN-011 | Desconto acima do limite exige autorização de perfil superior | Implementado pelo mecanismo de `X-Authorization-Token` desta história |
| RN-015 | Isolamento entre estabelecimentos | O claim `tid` é a única fonte de tenant em rota autenticada |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-070 | `user.authenticated` | Login realizado | method (PASSWORD/PIN), deviceId | ↑ |
| EVT-071 | `authorization.granted` | Ação sensível autorizada | action, authorizedBy, context | ↑ |
| EVT-072 | `permission.changed` | Permissão de papel alterada | roleId, added[], removed[] | ↑ |

## 7. Contrato de API

```http
POST /v1/auth/login
{ "email": "...", "password": "...", "otp": "123456" }
→ 200 { "accessToken": "...", "refreshToken": "...", "user": {...}, "tenant": {...} }

POST /v1/auth/pin
{ "pin": "4821", "deviceId": "..." }
→ 200 { "accessToken": "...", "user": {...}, "permissions": [...] }
→ 403 { "code": "DEVICE_NOT_REGISTERED" }
→ 429 { "code": "PIN_LOCKED", "meta": { "retryAfterSeconds": 900 } }

POST /v1/auth/refresh
{ "refreshToken": "..." }

POST /v1/auth/authorize
{ "action": "CANCEL_STARTED_ITEM", "pin": "9911",
  "context": { "orderItemId": "..." } }
→ 200 { "authorizationToken": "...", "expiresIn": 120, "authorizedBy": {...} }

# Claims do JWT
{ "sub": "<userId>", "tid": "<tenantId>", "sid": "<storeId>",
  "roles": ["WAITER"], "perms": ["order:create","order:read","table:open"],
  "did": "<deviceId>", "exp": 1234567890 }
```

> O `authorizationToken` viaja no header `X-Authorization-Token` da requisição que executa a ação sensível — o gerente digita o PIN no dispositivo do operador, sem trocar de sessão.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `app_user` | Usuário do estabelecimento | `email`, `password_hash`, `pin_hash`, `status`, `mfa_secret` |
| `role` | Papel configurável por tenant | `code`, `name`, `permissions` (array) |
| `user_role` | Vínculo usuário↔papel | `user_id`, `role_id` |
| `device` | Terminal autorizado (US-005) | `id`, `label`, `kind`, `status` |
| `audit_log` | Registro de autorização e alteração de permissão | `actor_id`, `authorized_by`, `action`, `before`, `after` |

> PIN e senha são armazenados com Argon2id. O PIN nunca trafega nem é armazenado em claro (ADR-031).

## 9. Comportamento offline

**Crítico para a operação.** O edge server valida PIN localmente contra a réplica de `app_user` recebida da nuvem — se dependesse da nuvem, uma queda de internet impediria a troca de turno e pararia a loja, violando o requisito estruturante RF-OFF-01.

Comportamento por caminho:

- **PIN operacional:** validado 100% local. Funciona com internet caída.
- **Autorização de ação sensível:** validada localmente, pelo mesmo mecanismo.
- **E-mail e senha:** validado na nuvem. Offline, o gestor perde acesso ao painel — degradação aceitável, porque painel de gestão não é operação crítica.
- **Alteração de papéis e permissões:** feita na nuvem, propagada ao edge pelo pull de configuração (US-063). Enquanto offline, valem as permissões da última sincronização.

## 10. Interface e experiência

- Teclado numérico grande em tela cheia para o PIN — cozinha e salão operam com pressa e mãos ocupadas
- Troca de operador em no máximo dois toques, sem tela de logout intermediária
- Autorização de ação sensível como modal sobre o contexto, sem perder o que o operador estava fazendo
- Indicação clara de quem está logado no terminal, sempre visível
- Mensagem de bloqueio informando o tempo restante, sem revelar se o PIN existe

## 11. Métricas, alertas e observabilidade

- Contagem de autenticações por método e por papel
- Taxa de bloqueio por tentativas incorretas — pico indica problema de usabilidade ou de treinamento
- Contagem de autorizações de ação sensível por tipo e por autorizador — insumo do painel de gestão
- Duração média de sessão operacional por dispositivo
- Alerta ao gestor em: PIN bloqueado, tentativa em dispositivo não registrado, alteração de permissão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Hash e verificação de PIN e senha; expiração de token; cálculo de bloqueio progressivo |
| Unitário | Política de autorização nega por padrão quando a permissão não está declarada |
| Integração | Sessão de PIN não é válida em outro `deviceId` |
| Integração | Ação sensível sem `X-Authorization-Token` retorna 403; com token expirado, também |
| Integração | Validação de PIN funciona com o edge desconectado da nuvem |
| Segurança | Força bruta de PIN bloqueada; mensagens não revelam existência de usuário |
| E2E | Troca de turno na cozinha em menos de dois toques |

## 13. Dependências

**Depende de:** US-001, US-005  
**Habilita:** US-033, US-054, US-055, US-090

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

**DoD — a história só é concluída quando:**

- [ ] Código revisado e aprovado por outro desenvolvedor
- [ ] Testes unitários dos casos de negócio passando
- [ ] Teste de integração do fluxo principal passando
- [ ] Teste de isolamento multi-tenant (quando a história toca tabela com `tenant_id`)
- [ ] Eventos emitidos conforme o catálogo do documento 04
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste pelo PO
- [ ] Sem violação do ADR-013 (proibição de código por cliente)
- [ ] Documentação atualizada (OpenAPI, catálogo de eventos, modelo de dados)
- [ ] Observabilidade instrumentada (log estruturado + traço OpenTelemetry)
- [ ] Aprovada pelo PO
- [ ] Revisão de segurança do fluxo de autenticação registrada

## 15. Riscos, premissas e pendências

- PIN de 4 dígitos tem entropia baixa por natureza; a mitigação é o vínculo obrigatório com dispositivo registrado somado ao bloqueio progressivo. Documentado no ADR-014.
- Compartilhamento de PIN entre operadores destrói a rastreabilidade por pessoa — risco de processo, não de sistema. Mitigação: treinamento e monitoramento de padrão de uso.
- Réplica de credenciais no edge amplia a superfície de ataque; exige criptografia em repouso e rotação de segredo por instalação (ADR-031).

---

*US-004 · Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*