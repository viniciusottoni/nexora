# ADR-025 · Emissão fiscal por adaptador — decisão parcialmente adiada

| | |
|---|---|
| **Status** | **Adiado** (arquitetura definida, provedor pendente) |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO — **aguarda cliente e contador** |
| **Relacionados** | ADR-013, ADR-024, ADR-026 |
| **Requisitos afetados** | RN-023, C6 |

---

## Contexto

**A emissão de documento fiscal não foi abordada na reunião de descoberta.** É uma lacuna relevante: venda no varejo alimentar tem obrigação fiscal, e a forma dela varia por estado (NFC-e na maioria, SAT/CF-e em São Paulo, MFE no Ceará), por regime tributário (Simples Nacional, Lucro Presumido) e por porte.

Isso cria uma situação delicada de projeto: não podemos decidir o provedor sem informação do cliente e do contador, mas **também não podemos deixar a arquitetura sem lugar para isso** — retrofit de emissão fiscal em um sistema de PDV pronto é caro e toca o núcleo de pagamento e fechamento.

## Decisão

**Decidimos agora a arquitetura; adiamos conscientemente a escolha do provedor.**

1. A emissão fiscal vive atrás de uma interface (`FiscalProvider`), no mesmo padrão do ADR-024
2. O modelo de dados já contempla os campos fiscais desde a Fase 1
3. O adaptador `NONE` (sem emissão) é o padrão inicial e permite operar o piloto
4. A escolha do provedor é **bloqueante para o go-live em produção legal**, não para o desenvolvimento

## Detalhamento

### Interface

```ts
export interface FiscalProvider {
  readonly code: string;
  readonly documentType: 'NFCE' | 'SAT' | 'MFE' | 'NONE';

  issue(input: FiscalIssueInput): Promise<FiscalDocument>;
  cancel(key: string, reason: string): Promise<FiscalCancellation>;
  getStatus(key: string): Promise<FiscalStatus>;
  issueContingency?(input: FiscalIssueInput): Promise<FiscalDocument>;
}
```

### Campos fiscais no modelo (desde a Fase 1)

| Entidade | Campos |
|---|---|
| `tenant_config.fiscal` | Regime tributário, CNPJ, IE, CSC, certificado, série, ambiente |
| `product` | NCM, CEST, CFOP, origem, unidade tributável |
| `product_variant` | Alíquotas (ICMS, PIS, COFINS), CST/CSOSN |
| `order` | Chave do documento, número, série, status fiscal, protocolo |
| `payment` | Código de forma de pagamento fiscal |

Esses campos ficam **opcionais e vazios** enquanto o adaptador for `NONE`. Adicioná-los agora custa quase nada; adicioná-los depois exigiria migration em tabelas grandes e revisão do fluxo de fechamento.

### Contingência offline — o ponto crítico

Emissão fiscal exige comunicação com a SEFAZ, que depende de internet. Isso colide diretamente com o ADR-001.

O tratamento previsto:

```
Fechamento de conta
   ├─ online   → emite normalmente
   └─ offline  → registra a venda, marca status fiscal PENDING
                 emite em contingência conforme a regra do modelo escolhido
                 transmite quando a conexão retornar
```

A regra específica de contingência **depende do provedor e do estado**, e é parte do que a decisão adiada precisa resolver.

### Alternativas de provedor (a avaliar quando a pendência for resolvida)

| Opção | Prós | Contras |
|---|---|---|
| API de terceiro (Focus NFe, WebmaniaBR, Nuvem Fiscal) | Rápido; sem gerir certificado nem schema da SEFAZ | Custo por documento; depende de internet |
| Biblioteca local que fala com a SEFAZ | Sem custo por documento; contingência mais controlada | Complexidade alta; manutenção de schema e certificado é permanente |
| Emissor de terceiro fora do sistema | Zero esforço | Dupla digitação; quebra a integridade do fluxo |
| SAT / MFE (hardware) | Funciona offline por natureza | Restrito a alguns estados; hardware adicional |

**Recomendação preliminar:** API de terceiro para a primeira versão. O custo por documento é previsível e evita que o time assuma a manutenção permanente do schema da SEFAZ — que é trabalho contínuo e sem relação com o valor do produto.

## Consequências

**Positivas**

- O desenvolvimento não fica bloqueado pela pendência
- Quando a decisão vier, é um adaptador — não uma reforma do núcleo
- Campos fiscais já existem; sem migration pesada depois
- Piloto pode operar com `NONE` em ambiente controlado

**Negativas**

- **O sistema não pode ir a produção legal sem essa decisão**
- Estimativa de prazo e custo permanece incompleta
- Contingência offline pode alterar o fluxo de fechamento

**Mitigações**

- A pendência está registrada como **bloqueio B2** no plano de entrega (doc. 09)
- Reunião com cliente e contador foi recomendada como próximo passo
- Arquitetura preparada reduz o impacto de decidir tarde

## Gatilho para converter em decisão

Esta ADR passa de `Adiado` para `Aceito` quando:

1. O cliente e o contador informarem o regime tributário e o estado de operação
2. For definido o tipo de documento exigido (NFC-e, SAT, MFE)
3. For avaliado se há emissor já contratado
4. For decidido quem responde pela configuração fiscal (Replay, contador ou cliente)

**Prazo desejável:** antes do início da Sprint S1.7 (caixa e pagamento).

## Como validar (quando implementado)

- Emissão em ambiente de homologação da SEFAZ
- Cancelamento dentro do prazo legal
- Contingência offline com transmissão posterior
- Conferência do contador sobre os primeiros documentos emitidos
