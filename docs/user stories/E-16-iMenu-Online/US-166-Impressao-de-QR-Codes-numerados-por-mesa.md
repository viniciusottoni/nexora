# US-166 · Impressão de QR Codes numerados por mesa

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-SAL-01 |
| **Regras de negócio** | — |
| **ADRs** | ADR-010 |
| **Eventos** | — |
| **Aplicações** | `web-admin` |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** baixar uma única página com o QR Code de todas as mesas, cada um já identificado com o número,
> **para** imprimir e afixar em cada mesa sem trabalho manual de conferência.

## 2. Contexto e motivação

US-020 (E-02) já cobre o cadastro de ambientes/mesas e a geração do QR Code por mesa. Esta história cobre especificamente a **saída consolidada para impressão**: uma página (PDF ou HTML pronto para impressão) com o QR Code de cada mesa cadastrada, o número da mesa em destaque e a marca do estabelecimento (ADR-010).

Isso importa porque a URL do QR Code agora aponta para `/{tenantName}/table/{qrCode}` (US-162), e a confirmação por número (US-165) só funciona bem se o número impresso ao lado do QR Code corresponder exatamente ao que o sistema espera — gerar isso automaticamente elimina erro humano de digitação/etiquetagem manual.

## 3. Escopo

### 3.1 Dentro desta história

- Botão "Baixar QR Codes" no cadastro de mesas (US-020), gerando uma página com todas as mesas ativas
- Cada QR Code acompanhado do número da mesa em destaque visual e do nome/logo do estabelecimento
- Layout pensado para corte e afixação individual (uma mesa por "cartão", com margem de corte)
- Regeneração sob demanda quando uma mesa é adicionada, renomeada ou removida — a página nunca fica desatualizada em relação ao cadastro
- Formato de saída em PDF, pronto para impressão doméstica ou gráfica

### 3.2 Fora desta história

- Cadastro de ambientes e mesas em si (US-020, já existente)
- Confirmação do número pelo cliente (US-165)
- Arte personalizada avançada do QR Code além de logo/cores do tenant (ADR-010 já cobre o essencial; personalização extra fica como possível [HIPÓTESE] futura, fora de escopo aqui)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Impressão de QR Codes numerados

  Cenário: Geração da página consolidada
    Dado um estabelecimento com 20 mesas cadastradas
    Quando o gestor clicar em "Baixar QR Codes"
    Então deve receber um PDF com 20 QR Codes, cada um com o número da mesa correspondente visível
    E cada QR Code deve apontar para /{tenantName}/table/{qrCode} da mesa certa

  Cenário: Mesa nova refletida na próxima geração
    Dado uma mesa nova cadastrada após a última geração da página
    Quando o gestor gerar a página novamente
    Então a mesa nova deve aparecer, sem exigir qualquer configuração manual adicional

  Cenário: Layout pronto para corte
    Dado a página gerada
    Quando impressa
    Então cada QR Code deve estar em um bloco com margem suficiente para corte individual
```

## 5. Regras de negócio aplicáveis

_Não se aplica — geração de artefato de apoio, sem regra de negócio nova._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/tables/qrcodes/print
→ 200 (application/pdf) — página com todos os QR Codes ativos, numerados
```

## 8. Modelo de dados

Nenhuma tabela nova — leitura de `table`/`store` já existentes (US-020). Nenhum dado novo é persistido; a página é gerada sob demanda a partir do cadastro atual.

## 9. Comportamento offline

_Não se aplica — ver ADR-040._

## 10. Interface e experiência

- Botão de download visível na tela de cadastro de mesas (US-020), não em local separado de difícil acesso
- Pré-visualização antes do download, para o gestor conferir que o número de mesas está correto
- Marca do estabelecimento (logo, cor) aplicada automaticamente via ADR-010, sem configuração adicional nesta tela

## 11. Métricas, alertas e observabilidade

_Não se aplica diretamente._

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | PDF gerado contém um QR Code por mesa ativa, com número correto |
| Integração | QR Code lido de fato abre a URL certa da mesa certa |
| Regressão | Mesa removida não aparece mais na próxima geração |

## 13. Dependências

**Depende de:** US-020 (E-02), US-162
**Habilita:** operação normal do salão sob a nova URL (junto com US-165)

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Layout de impressão (tamanho do QR Code, posição do número, margens de corte) validado com o time de UX

**DoD — a história só é concluída quando:**

- [ ] PDF gerado corretamente para estabelecimento com 1 e com dezenas de mesas
- [ ] QR Codes testados fisicamente (impressos e lidos por celular real)
- [ ] Documentação atualizada
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- Nenhum risco técnico relevante identificado — é geração de artefato estático a partir de dado já existente.

---

*US-166 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
