# ADR-014 · Autenticação por PIN para perfis operacionais

| | |
|---|---|
| **Status** | Substituído |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO, UX |
| **Substituído por** | [ADR-041](./ADR-041-autenticacao-sem-rede-local.md) |
| **Relacionados** | ADR-023, ADR-031 |
| **Requisitos afetados** | RF-IAM-03, RF-IAM-05, RF-IAM-07, RNF-SEG-04 a 06 |

---

> ⚠️ **Substituído em 06/08/2026 pelo [ADR-041](./ADR-041-autenticacao-sem-rede-local.md).** Fim do edge (ADR-040) remove a camada de rede local ("dispositivo só acessa pela LAN") da tabela de proteção abaixo. PIN pessoal por operador, dispositivo autorizado e sessão por turno **continuam vigentes** — apenas a camada de rede muda. Ver ADR-041 para o modelo revisado.

## Contexto

Garçom e cozinha trocam de operador dezenas de vezes por turno. O contexto é hostil a digitação: mãos ocupadas, pressão de tempo, ambiente úmido, telas compartilhadas.

Exigir e-mail e senha nesse cenário produz um resultado previsível e conhecido de qualquer operação de restaurante: **a equipe abre uma sessão única de manhã e todo mundo usa a mesma**. O efeito colateral é grave — toda métrica por operador vira ficção e toda auditoria aponta para a mesma pessoa. Ou seja, a segurança "forte" acaba destruindo dois requisitos centrais do produto (RF-AUD, RF-BI).

## Decisão

**PIN numérico de 4 a 6 dígitos, válido apenas em dispositivo previamente registrado, na rede local.** Sessão dura o turno (8 h). Ações sensíveis exigem PIN de perfil superior digitado **no próprio dispositivo do operador**.

Gestor e administrativo continuam com e-mail e senha, com segundo fator opcional (obrigatório para admin de plataforma).

## Detalhamento

### Autenticação

```http
POST /v1/auth/pin
{ "pin": "4821", "deviceId": "<uuid do terminal registrado>" }
→ 200 { accessToken (8h), user, permissions }
```

O `deviceId` é obrigatório. Um PIN sozinho, fora de dispositivo registrado, não autentica nada.

### Elevação pontual (o padrão de gerente)

```http
POST /v1/auth/authorize
{ "action": "CANCEL_STARTED_ITEM", "pin": "9911", "context": { "orderItemId": "..." } }
→ 200 { authorizationToken, expiresIn: 120, authorizedBy: { id, name } }
```

O token é enviado no header `X-Authorization-Token` da requisição que executa a ação. O gerente digita o PIN na tela do garçom, sem trocar de sessão — o fluxo operacional não é interrompido, e o registro de auditoria contém **quem executou e quem autorizou**.

### Camadas de proteção

| Camada | Medida |
|---|---|
| Vínculo | PIN só funciona em dispositivo registrado (RF-IAM-05) |
| Rede | Dispositivo operacional só acessa o edge pela LAN |
| Tentativas | Bloqueio de 15 min após 5 erros, com alerta ao gestor |
| Rotação | Obrigatória a cada 90 dias |
| Unicidade | PIN não pode repetir entre usuários ativos do mesmo tenant |
| Trivialidade | Bloqueio de sequências óbvias (1234, 0000, repetições) |
| Armazenamento | Hash com Argon2id e salt — nunca em claro |
| Expiração | Sessão encerra no fim do turno configurado |

### Por que isso é aceitável

Um PIN de 4 dígitos tem 10.000 combinações — fraco isoladamente. Mas o modelo de ameaça real aqui não é o atacante remoto: é o colega que quer lançar um desconto no nome de outro. As camadas de dispositivo registrado, rede local, bloqueio por tentativas e auditoria completa endereçam exatamente essa ameaça, com um custo de usabilidade que a operação suporta.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| E-mail e senha para todos | Segurança formal maior | Inviável operacionalmente; leva ao compartilhamento de sessão | Destruiria métrica por operador e auditoria — piora a segurança real |
| Cartão RFID / crachá | Rápido; difícil de compartilhar | Hardware adicional por loja e por pessoa; custo e logística | Avaliar em fase futura, se o cliente demandar |
| Biometria | Sem credencial a lembrar | Inviável com mãos sujas, molhadas ou com luva | Ambiente de cozinha inviabiliza |
| Sessão única do terminal, sem identificação | Simples | Sem métrica por operador, sem auditoria | Falha em dois requisitos centrais |
| QR pessoal do funcionário | Rápido | Facilmente fotografado e compartilhado | Pior que o PIN em segurança prática |

## Consequências

**Positivas**

- Troca de operador em segundos — a equipe não busca contorno
- Métrica por operador e auditoria passam a refletir a realidade
- Autorização de gerente sem interromper o fluxo nem trocar de sessão
- Modelo familiar: é como funcionam os PDVs que a equipe já conhece

**Negativas**

- PIN é credencial fraca isoladamente
- Rotação a cada 90 dias gera atrito periódico
- Risco de observação do PIN por terceiros ("shoulder surfing")

**Mitigações**

- Camadas descritas acima (dispositivo + rede + bloqueio + auditoria)
- Teclado numérico embaralhado na tela de PIN, para dificultar observação por posição
- Alerta ao gestor em padrão anômalo (autorizações fora do horário, volume incomum de cancelamentos)
- Métrica de anomalia por operador no painel (cancelamentos e descontos acima do padrão)

## Como validar

- Teste de integração: PIN correto em dispositivo não registrado é recusado
- Teste de integração: bloqueio após 5 tentativas, com alerta gerado
- Teste U-02: troca de operador na cozinha em menos de 10 s
- Auditoria contém executor **e** autorizador em toda ação sensível

## Revisitar quando

- O cliente demandar crachá RFID
- Surgir requisito regulatório de autenticação forte para operação de caixa
