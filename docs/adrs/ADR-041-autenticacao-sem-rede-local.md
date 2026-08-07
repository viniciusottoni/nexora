# ADR-041 · Autenticação operacional por PIN pessoal, sem dependência de rede local

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 06/08/2026 |
| **Decisores** | Tech Lead, PO, Cliente |
| **Substitui** | ADR-014 |
| **Substituído por** | — |
| **Relacionados** | ADR-040, ADR-023, ADR-031 |
| **Requisitos afetados** | RF-IAM-03, RF-IAM-05, RF-IAM-07, RNF-SEG-04, RNF-SEG-16 |

---

## Contexto

O ADR-014 apoiava parte da segurança do PIN operacional em uma camada de rede: *"dispositivo operacional só acessa o edge pela LAN"*. Com o fim do servidor local (ADR-040), essa camada deixa de poder existir — todo dispositivo acessa `iMenu.Api` pela internet, como qualquer aplicação online.

O restante do raciocínio do ADR-014 continua de pé e **não muda**: garçom e cozinha trocam de operador dezenas de vezes por turno; exigir e-mail e senha nesse cenário leva ao compartilhamento de sessão, o que destrói RF-AUD (auditoria) e RF-BI (métrica por operador). Confirmado explicitamente nesta revisão: o PIN continua **pessoal, por operador** — a identificação individual não é negociável.

## Forças em jogo

| Força | Descrição |
|---|---|
| Continuidade | O que funcionava para o modelo local precisa de um equivalente para o modelo 100% online |
| Identificação individual | RF-AUD e RF-BI continuam exigindo saber quem fez o quê, não apenas qual terminal |
| Superfície de ataque maior | Sem restrição de LAN, o endpoint de login por PIN fica exposto à internet pública, não só à rede da loja |
| Simplicidade operacional | Troca de operador continua precisando ser rápida — nenhuma fricção nova para a equipe de salão/cozinha |

## Decisão

**O dispositivo é a unidade de autorização de acesso; o PIN é a unidade de identificação da pessoa.** A camada de rede local do ADR-014 é removida sem substituto de rede — o controle equivalente passa a ser inteiramente sobre identidade: dispositivo autorizado explicitamente pelo gestor + PIN pessoal do operador nesse dispositivo.

## Detalhamento

### Fluxo de acesso

```
1. Gestor autoriza o dispositivo (código de pareamento de 6 dígitos, US-005/E-16 US-163)
   → dá um nome ao dispositivo (ex.: "Celular Garçom 2", "Caixa 1")

2. Pessoa acessa a URL do app (/{tenantName}/server, /kds ou /pos)
   → app já roda no dispositivo autorizado (sessão de pareamento persistida)

3. Operador digita seu PIN pessoal (4-6 dígitos, igual ao ADR-014)
   → POST /v1/auth/pin { "pin": "4821", "deviceId": "<uuid do dispositivo autorizado>" }
   → 200 { accessToken (turno), user, permissions }
```

O `deviceId` continua obrigatório — um PIN fora de um dispositivo autorizado não autentica nada, exatamente como antes. A diferença é **como** o dispositivo prova que está autorizado: antes, em parte, por estar na LAN; agora, inteiramente pelo pareamento explícito (token de dispositivo emitido no momento da autorização, guardado no dispositivo).

### Camadas de proteção — revisão da tabela do ADR-014

| Camada | ADR-014 (modelo local) | ADR-041 (modelo online) |
|---|---|---|
| Vínculo | PIN só funciona em dispositivo registrado | **Mantido** — PIN só funciona em dispositivo autorizado |
| Rede | Dispositivo só acessa o edge pela LAN | **Removido** — não há LAN a restringir; controle passa a ser 100% por identidade do dispositivo |
| Tentativas | Bloqueio de 15 min após 5 erros, com alerta | **Mantido** |
| Rotação | Obrigatória a cada 90 dias | **Mantido** |
| Unicidade | PIN não repete entre usuários ativos do tenant | **Mantido** |
| Trivialidade | Bloqueio de sequências óbvias | **Mantido** |
| Armazenamento | Hash Argon2id, nunca em claro | **Mantido** |
| Expiração | Sessão encerra no fim do turno (8h) | **Mantido** — turno continua sendo a unidade de sessão, não 24h fixas (ver "Alternativas consideradas") |
| **Novo** — Rate limit de rede | — | Rate limit por IP e por dispositivo no endpoint `/v1/auth/pin`, agora exposto à internet pública (RNF-SEG-11) |
| **Novo** — Token de dispositivo | Implícito na presença na LAN | Token de dispositivo (secret emitido no pareamento) enviado em toda requisição de auth, não só o `deviceId` |

### Por que isso continua aceitável

O modelo de ameaça do ADR-014 permanece o mesmo — "o colega que quer lançar um desconto no nome de outro", não o atacante remoto genérico. A camada de rede nunca foi a defesa principal contra *esse* ameaça (era defesa em profundidade); a defesa principal sempre foi dispositivo vinculado + auditoria completa + bloqueio por tentativas. Essas se mantêm.

A exposição nova é o endpoint em si estar acessível pela internet, não apenas na rede da loja — mitigada por rate limit agressivo por IP/dispositivo e pelo fato de que um PIN sozinho, sem o `deviceId` de um dispositivo já autorizado, continua sem autenticar nada.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Sessão fixa de 24h por dispositivo | Simplifica a expiração | Aumenta a janela de uso indevido em caso de dispositivo perdido/roubado, sem ganho operacional correspondente | Avaliado e descartado nesta revisão — mantido o modelo por turno (8h) do ADR-014 |
| PIN por dispositivo (não por operador) | Elimina a etapa de identificação pessoal | Recria exatamente o problema que o ADR-014 foi criado para evitar — métrica por operador e auditoria por autor deixam de refletir a realidade | Confirmado nesta revisão: identificação continua pessoal |
| VPN ou IP fixo por loja como substituto da LAN | Mantém uma camada de rede | Exige infraestrutura de rede que o modelo 100% online explicitamente elimina (ADR-040) | Contradiz o motivo da mudança de arquitetura |
| 2FA adicional no PIN operacional | Segurança formal maior | Reintroduz fricção que o ADR-014 rejeitou para e-mail/senha, pelo mesmo motivo (troca de operador dezenas de vezes por turno) | Desproporcional à ameaça real |

## Consequências

**Positivas**

- Nenhuma mudança na experiência do operador — PIN pessoal, troca rápida, exatamente como hoje
- RF-AUD e RF-BI continuam intactos — identificação por pessoa preservada
- Modelo de autorização de dispositivo (código de 6 dígitos) já existia (US-005) e é reaproveitado, agora como única camada de vínculo, não mais coadjuvante da LAN

**Negativas**

- Superfície de ataque do endpoint de PIN aumenta (exposto à internet, não só à rede da loja)
- Perda de uma camada de defesa em profundidade (a LAN), mesmo que não fosse a principal

**Mitigações**

- Rate limit por IP e por dispositivo no endpoint de autenticação (RNF-SEG-11)
- Token de dispositivo (não apenas `deviceId`) validado em toda requisição
- Alerta ao gestor em padrão anômalo continua vigente (login fora de padrão geográfico/horário passa a ser sinal adicional a considerar — avaliar em E-16/US-164)

## Como validar

- Teste de integração: PIN correto sem token de dispositivo válido é recusado
- Teste de integração: bloqueio após 5 tentativas, com alerta, mesmo pela internet
- Teste de carga: rate limit do endpoint de PIN resiste a tentativa de força bruta distribuída
- Auditoria contém executor **e** autorizador em toda ação sensível, como antes

## Revisitar quando

- Surgir requisito regulatório de autenticação forte para operação de caixa
- Volume de tentativas maliciosas no endpoint de PIN justificar 2FA ou allowlist de rede por tenant

---

*ADR-041 · Pacote 004_DonaBetinha · Replay Studio.*
