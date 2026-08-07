# US-164 · Login pessoal por PIN sem rede local

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-IAM-03, RF-IAM-07 |
| **Regras de negócio** | RN-004 |
| **ADRs** | ADR-041 (substitui ADR-014) |
| **Eventos** | — |
| **Aplicações** | `iMenu.Api`, `web-pos`, `web-kds` |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** garçom (P2), pizzaiolo (P3) ou caixa (P4),
> **quero** entrar em `/server`, `/kds` ou `/pos` num dispositivo já autorizado e me identificar com meu PIN pessoal,
> **para** trocar de operador em segundos, com métrica e auditoria corretas por pessoa, mesmo sem rede local.

## 2. Contexto e motivação

O ADR-014 já resolvia o problema central (PIN numérico, dispositivo vinculado, sessão por turno) — esta história implementa a versão revisada pelo [ADR-041](../../adrs/ADR-041-autenticacao-sem-rede-local.md): tudo igual, exceto que a camada "dispositivo só acessa pela LAN" é substituída por validação de `deviceSecret` (US-163) em toda tentativa de login.

Confirmado explicitamente nesta revisão: o PIN **continua pessoal**, a sessão **continua durando o turno** (não 24h fixas) — nenhuma das duas coisas muda.

## 3. Escopo

### 3.1 Dentro desta história

- Login por PIN (4-6 dígitos) exigindo `deviceId` **e** `deviceSecret` válidos (US-163), não apenas o `deviceId`
- Sessão de acesso durando o turno configurado (8h, como no ADR-014 original) — não 24h fixas
- Elevação pontual (PIN de perfil superior para ação sensível, digitado no próprio dispositivo do operador) — mantida sem alteração
- Bloqueio após 5 tentativas incorretas por 15 min, com alerta ao gestor
- Rotação obrigatória de PIN a cada 90 dias
- Unicidade de PIN entre usuários ativos do tenant
- Rate limit por IP e por dispositivo no endpoint de login (novo, em relação ao ADR-014 original — ver ADR-041, seção "camadas de proteção")
- Gestor e administrativo continuam com e-mail e senha, 2FA obrigatório para admin de plataforma (sem alteração)

### 3.2 Fora desta história

- Autorização do dispositivo em si (US-163)
- Autenticação da mesa (não usa PIN — ver US-165)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Login pessoal por PIN

  Cenário: Login bem-sucedido em dispositivo autorizado
    Dado um dispositivo autorizado (deviceId + deviceSecret válidos)
    Quando o operador digitar seu PIN correto
    Então deve receber um accessToken válido pelo restante do turno
    E o evento de login deve registrar o deviceId e o operador identificado

  Cenário: PIN correto em dispositivo não autorizado
    Dado um deviceId ou deviceSecret inválido ou revogado
    Quando um PIN correto for enviado
    Então o login deve ser recusado com 401/403
    E nenhum token deve ser emitido

  Cenário: Bloqueio por tentativas
    Dado 5 tentativas de PIN incorretas em um dispositivo
    Quando a sexta tentativa ocorrer dentro de 15 minutos
    Então o dispositivo deve ser bloqueado por 15 minutos
    E o gestor deve ser alertado

  Cenário: Sessão por turno, não por tempo fixo
    Dado um login realizado às 18h com turno configurado até 23h
    Quando o relógio passar de 23h
    Então a sessão deve expirar
    E não deve durar 24h fixas independente do turno configurado

  Cenário: Elevação pontual sem trocar de sessão
    Dado um garçom tentando cancelar um item já iniciado
    Quando o gestor digitar seu próprio PIN na tela do garçom
    Então a ação deve ser autorizada sem interromper a sessão do garçom
    E a auditoria deve registrar quem executou e quem autorizou

  Cenário: Login pela internet pública
    Dado um dispositivo autorizado fora da rede de qualquer loja
    Quando o operador fizer login com PIN correto
    Então o login deve funcionar normalmente
    E deve estar sujeito ao rate limit por IP e por dispositivo
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | PIN identifica o autor; `deviceId` (US-163) identifica o dispositivo |

## 6. Eventos emitidos e consumidos

_Login em si não é evento de domínio catalogado — segue o padrão de autenticação já definido no ADR-041/ADR-014. Ações subsequentes emitem seus eventos normalmente, carregando `actorId` e `deviceId`._

## 7. Contrato de API

```http
POST /v1/auth/pin
{ "pin": "4821", "deviceId": "<uuid>", "deviceSecret": "<secret do pareamento>" }
→ 200 { accessToken (expira no fim do turno), user, permissions }
→ 401 se PIN incorreto
→ 403 se deviceId/deviceSecret inválido, revogado ou ausente

POST /v1/auth/authorize
{ "action": "CANCEL_STARTED_ITEM", "pin": "9911", "context": { "orderItemId": "..." } }
→ 200 { authorizationToken, expiresIn: 120, authorizedBy: { id, name } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `app_user` | Credencial do operador | `pin_hash`, `pin_rotated_at`, `failed_attempts`, `blocked_until` (já existentes, domain/01) |
| `device` | Vínculo de autorização | `id`, `is_active` (US-163) |

Nenhuma alteração de schema em relação ao modelo já existente — `deviceSecret` é validado contra o registro emitido no pareamento (US-163), não requer coluna nova além do que já é necessário para armazenar o secret com segurança (hash, não texto plano).

## 9. Comportamento offline

_Não se aplica — ver ADR-040. Login exige conexão com `iMenu.Api`, sempre._

## 10. Interface e experiência

- Teclado numérico embaralhado na tela de PIN, para dificultar observação por posição (mantido do ADR-014)
- Troca de operador em segundos — sem tela intermediária além do teclado de PIN
- Elevação pontual não interrompe o fluxo do operador nem exige logout/login

## 11. Métricas, alertas e observabilidade

- Métrica de anomalia por operador (cancelamentos e descontos acima do padrão) — mantida
- Alerta em padrão anômalo de autorização (fora do horário, volume incomum)
- Novo: alerta de tentativas de login de IPs incomuns para o padrão do tenant (sinal adicional agora que o endpoint é público)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | PIN correto sem `deviceSecret` válido é recusado |
| Integração | Bloqueio após 5 tentativas, com alerta gerado |
| Integração | Sessão expira no fim do turno configurado, não em 24h fixas |
| Segurança | Rate limit do endpoint resiste a tentativa de força bruta distribuída por múltiplos IPs |
| Auditoria | Toda ação sensível contém executor e autorizador |

## 13. Dependências

**Depende de:** US-163
**Habilita:** operação normal de `/server`, `/kds`, `/pos` em todas as demais histórias do backlog

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] `deviceSecret` e seu ciclo de vida (emissão, validação, revogação) confirmados com US-163
- [ ] Duração do turno confirmada como configuração existente (reaproveitada do ADR-014), não nova

**DoD — a história só é concluída quando:**

- [ ] Login funcionando ponta a ponta, incluindo bloqueio, rotação e elevação pontual
- [ ] Sessão expira corretamente no fim do turno, testada explicitamente
- [ ] Rate limit validado sob teste de carga
- [ ] Auditoria completa (executor + autorizador) validada
- [ ] Documentação atualizada
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- O endpoint de PIN, antes restrito à rede da loja, agora é público na internet — mitigado por rate limit e pelo `deviceSecret`, mas é uma mudança real de superfície de ataque que deve ser comunicada explicitamente à revisão de segurança antes do go-live.
- **[PENDÊNCIA]** confirmar limiar exato de rate limit por IP/dispositivo (RNF-SEG-11) — não estava dimensionado para tráfego público antes desta revisão.

---

*US-164 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
