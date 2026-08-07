# US-165 · Abertura de mesa por QR Code e número

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-SAL-01, RF-SAL-04 |
| **Regras de negócio** | — |
| **ADRs** | ADR-040 |
| **Eventos** | novo — ver seção 6 |
| **Aplicações** | `web-menu`, `web-pos` |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** cliente sentado à mesa,
> **quero** ler o QR Code e informar o número da mesa para confirmar que estou ali,
> **para** que o pedido comece rápido e a equipe saiba, no mesmo instante, que a mesa foi aberta.

## 2. Contexto e motivação

Esta história **refina** o fluxo já descrito em US-020 (Cadastrar ambientes, mesas e gerar QR Code) e US-021/US-022 (E-02) para a nova convenção de URL (US-162): o QR Code aponta para `/{tenantName}/table/{qrCode}`, e o **número da mesa** funciona como uma confirmação simples — não uma senha secreta, mas uma etapa de conveniência que garante que a pessoa está de fato na mesa física e dispara o alerta correto para caixa e garçom.

Confirmado nesta revisão: não há pretensão de segurança forte aqui — a URL já é essencialmente pública (impressa e afixada na mesa). O número da mesa serve para **confirmar a abertura** e para que caixa/garçom recebam o alerta já sabendo qual mesa foi aberta, sem depender de leitura visual do salão.

## 3. Escopo

### 3.1 Dentro desta história

- Leitura do QR Code abre `/{tenantName}/table/{qrCode}`
- Tela pede a confirmação do número da mesa antes de liberar o cardápio/pedido
- Ao confirmar o número correto, a mesa é aberta (ou a sessão do cliente é vinculada a uma mesa já aberta pelo garçom)
- Alerta imediato a caixa e garçom responsável: "Mesa {número} aberta"
- Número incorreto não trava o fluxo de forma hostil — permite nova tentativa, com limite razoável para evitar tentativa de adivinhação em sequência
- Integração com o fluxo já existente de US-022 (abertura por garçom **ou** por cliente) — este é o caminho "por cliente", agora usando número em vez de apenas o token do QR Code isoladamente

### 3.2 Fora desta história

- Cadastro de ambientes e geração do QR Code em si (já coberto por US-020, E-02)
- Impressão em lote dos QR Codes numerados (US-166)
- Mapa de mesas com status e tempo (US-023, E-02, sem alteração)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Abertura de mesa por QR Code e número

  Cenário: Confirmação correta abre a mesa
    Dado um QR Code da mesa 12 lido pelo cliente
    Quando ele informar "12" como número da mesa
    Então a mesa deve ser aberta (ou vinculada, se já aberta pelo garçom)
    E caixa e garçom devem ser alertados imediatamente: "Mesa 12 aberta"

  Cenário: Número incorreto
    Dado um QR Code da mesa 12
    Quando o cliente informar um número diferente de 12
    Então o acesso não deve ser liberado
    E deve ser oferecida nova tentativa, sem mensagem que ajude a adivinhar o número certo

  Cenário: Mesa já aberta pelo garçom
    Dado que o garçom já abriu a mesa 12 antes do cliente ler o QR Code
    Quando o cliente confirmar o número
    Então sua sessão deve se vincular à mesa já aberta
    E nenhum alerta duplicado de abertura deve ser disparado

  Cenário: Acesso sem instalação
    Dado o QR Code impresso na mesa
    Quando lido pela câmera do celular do cliente
    Então deve abrir direto no navegador, sem exigir instalação de app (ADR-009)
```

## 5. Regras de negócio aplicáveis

_Sem regra de negócio numerada nova — refina o comportamento já coberto por RN aplicáveis a US-020/021/022._

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal |
|---|---|---|---|
| EVT-novo (a numerar no catálogo, doc. 04) | `table.opened_by_customer` | Cliente confirma o número corretamente | tableId, tableNumber, qrCode |

> Reaproveita o evento `table.opened` já existente se a semântica for equivalente — a numeração exata e a decisão de reaproveitar ou criar evento novo ficam para o refinamento técnico, marcadas aqui como pendência.

## 7. Contrato de API

```http
GET  /{tenantName}/table/{qrCode}        # rota do frontend, resolve o tenant e o QR Code
POST /v1/tables/{qrCode}/confirm
{ "tableNumber": "12" }
→ 200 { "table": { "id": "...", "number": "12", "status": "OPEN" }, "sessionToken": "..." }
→ 422 { "error": "NUMBER_MISMATCH" }        # número incorreto, sem detalhar o número certo
```

## 8. Modelo de dados

Nenhuma tabela nova prevista — reaproveita `table`/`store` (domain, a confirmar em `03-Modelo-de-Dados.md` e nos domínios de operação) já existentes para ambientes e mesas (US-020). Validar se `table.number` já existe como campo comparável diretamente ao QR Code, ou se precisa de índice/constraint adicional.

## 9. Comportamento offline

_Não se aplica — ver ADR-040._

## 10. Interface e experiência

- Campo de número grande, teclado numérico, sem exigir mais do que o número (sem nome, sem cadastro)
- Mensagem de erro neutra em caso de número incorreto — não indicar "faltam N dígitos" ou qualquer pista
- Confirmação visual imediata ("Mesa 12 — bem-vindo!") ao acertar

## 11. Métricas, alertas e observabilidade

- Alerta em tempo real a caixa/garçom na abertura — reaproveita o canal de alertas já existente (E-08)
- Taxa de erro na confirmação do número (sinal de QR Code mal posicionado ou trocado entre mesas)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Número correto abre/vincula a mesa e dispara o alerta |
| Integração | Número incorreto não abre a mesa e não vaza pista |
| E2E | Fluxo completo: ler QR Code → confirmar número → ver cardápio → alerta chega ao caixa em tempo real |

## 13. Dependências

**Depende de:** US-162, e de US-020/021/022 (E-02) já existentes
**Habilita:** operação normal do salão sob a nova URL

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Confirmado com o time se `table.opened_by_customer` é evento novo ou reaproveitamento de `table.opened`
- [ ] Limite de tentativas de número incorreto definido (evitar varredura sequencial)

**DoD — a história só é concluída quando:**

- [ ] Fluxo completo testado E2E
- [ ] Alerta chega a caixa e garçom em tempo real
- [ ] Documentação atualizada (US-020/021/022 referenciadas cruzadamente)
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- **[PENDÊNCIA]** limite de tentativas para evitar que alguém sentado em uma mesa tente números de mesas vizinhas sequencialmente — não é ameaça grave (não há dado sensível exposto), mas vale um limite razoável por sessão/IP.
- Esta história deve ser lida em conjunto com US-020/021/022 (E-02) — não os substitui, refina o mecanismo de confirmação à luz da nova URL.

---

*US-165 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
