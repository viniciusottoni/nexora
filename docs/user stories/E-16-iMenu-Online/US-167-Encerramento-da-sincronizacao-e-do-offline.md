# US-167 · Encerramento da sincronização e do offline

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 (última desta ordem — ver README de E-16) |
| **Requisitos funcionais** | — (remove RF-OFF-01 a 09) |
| **Regras de negócio** | — |
| **ADRs** | ADR-040 |
| **Eventos** | — |
| **Aplicações** | Documentação — `docs/` |
| **Autoridade do dado** | — |

---

## 1. História

> **Como** time de produto e engenharia,
> **quero** que todo o backlog, requisitos e documentação reflitam formalmente o fim da sincronização e do modo offline, sem resíduo esquecido,
> **para** que ninguém, meses depois, planeje uma história em cima de um conceito que não existe mais.

## 2. Contexto e motivação

As histórias US-160 a US-166 constroem o novo modelo. Esta história é a **varredura de encerramento**: garantir que nada do modelo anterior sobra como referência ativa e não sinalizada — nem no PRD, nem nos RNFs, nem no backlog, nem em ADRs relacionados que não foram explicitamente revisados nesta rodada.

Diferente de US-160 (rebranding de nome), esta história trata de **conceitos**, não de texto: RF-OFF, RNF-OFF, o épico E-06 inteiro, e qualquer história em outros épicos que ainda assuma edge/offline como comportamento válido.

## 3. Escopo

### 3.1 Dentro desta história

- Confirmar que E-06 (Sincronização Local-Nuvem) está formalmente cancelado, com banner em cada uma das 9 histórias — feito nesta rodada
- Confirmar que US-006 e US-034 estão formalmente canceladas, e US-005 formalmente substituída por US-163 — feito nesta rodada
- Remover a seção RNF-OFF (documento 08) do conjunto de requisitos ativos, preservando-a como histórico com marcação clara — feito nesta rodada
- Revisar RF-OFF-01 a 09 no documento 01 (PRD) e marcá-los como removidos — **pendente**, não coberto em detalhe nesta rodada (ver seção 15)
- Varredura do restante do backlog (E-00 a E-15) por qualquer critério de aceite, contrato de API ou seção "Comportamento offline" que ainda descreva funcionamento sem internet como válido, e sinalizá-las — **parcialmente coberto** nesta rodada (US-140 sinalizada; demais histórias ainda pendentes de varredura linha a linha)
- Revisar `02-Arquitetura-Tecnica.md`, `05-Contratos-de-API.md`, `domain/09-Metricas-e-Alertas.md` e `10-Estrategia-de-Testes-e-Qualidade.md` quanto a referências a edge/sync/offline — **pendente**, fora do detalhamento desta rodada

### 3.2 Fora desta história

- Remoção de código (US-161)
- Remoção de tabelas (US-169)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Encerramento formal do modelo offline

  Cenário: Nenhum épico ativo assume offline como comportamento válido
    Dado o backlog completo (E-00 a E-16)
    Quando cada história for revisada
    Então nenhuma história ativa (não cancelada/substituída) deve descrever operação sem internet como funcional
    E toda história que hoje descreve deve estar marcada como cancelada, substituída ou com nota de revisão

  Cenário: RF-OFF e RNF-OFF sinalizados
    Dado o documento 01 (PRD) e o documento 08 (RNF)
    Quando consultados
    Então RF-OFF-01 a 09 e a seção RNF-OFF devem estar marcados como removidos/descontinuados, não silenciosamente ausentes

  Cenário: Nenhuma referência ativa a "edge" como componente vigente
    Dado toda a documentação de arquitetura (02, 05, ADRs)
    Quando consultada
    Então qualquer menção a "edge" deve estar em conteúdo explicitamente marcado como histórico/substituído
```

## 5. Regras de negócio aplicáveis

_Não se aplica — governança documental._

## 6. Eventos emitidos e consumidos

_Não se aplica._

## 7. Contrato de API

_Não se aplica diretamente — mas os endpoints de sincronização (`/v1/sync/*`) devem ser removidos do documento 05 quando essa revisão for feita (ver seção 15, pendência)._

## 8. Modelo de dados

_Ver US-169._

## 9. Comportamento offline

_Esta história **é** o encerramento formal do conceito — não se aplica no sentido operacional._

## 10. Interface e experiência

_Não se aplica._

## 11. Métricas, alertas e observabilidade

- Métricas e alertas de sincronização (atraso, fila de outbox, instalação ausente) devem ser removidos dos documentos de observabilidade quando a revisão pendente (seção 15) for concluída

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Documental | Busca por "RF-OFF", "RNF-OFF", "api-edge", "sincronização local-nuvem" fora de contexto explicitamente histórico — zero ocorrências não sinalizadas |
| Backlog | Cada história do backlog revisada uma vez contra este critério antes de entrar em sprint, até a varredura completa (pendência) ser concluída |

## 13. Dependências

**Depende de:** US-161 (para que a remoção documental reflita a remoção técnica real)
**Habilita:** integridade de longo prazo do backlog — evita que histórias futuras sejam planejadas sobre premissas obsoletas

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Lista de documentos ainda não revisados nesta rodada confirmada (seção 15)

**DoD — a história só é concluída quando:**

- [ ] Todos os itens listados na seção 15 como pendência foram endereçados
- [ ] Nenhuma referência ativa e não sinalizada a offline/edge/sync permanece no pacote de documentação
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

Esta história tem, deliberadamente, um escopo de "varredura" maior do que o que foi possível cobrir na rodada de estruturação do E-16. **O que já foi feito** nesta rodada: banners de cancelamento em E-06 e nas histórias US-006/034/005/140; ADR-001/007/014/019/027/033 marcadas como substituídas; ADR-008/010/011 com nota de revisão; seção RNF-OFF e RNF-PER-01 ajustados no documento 08; `edge_installation` removida do domain/01 (US-169).

**O que fica pendente, a ser tratado como continuação desta história antes de considerá-la concluída:**

- `01-PRD-Especificacao-Funcional.md` — remover/marcar RF-OFF-01 a 09 formalmente
- `02-Arquitetura-Tecnica.md` — reescrever a seção de topologia local/nuvem
- `05-Contratos-de-API.md` — remover endpoints `/v1/sync/*`
- `domain/09-Metricas-e-Alertas.md` — remover métricas/alertas de sincronização e de instalação
- `10-Estrategia-de-Testes-e-Qualidade.md` — remover cenários de "caos offline"
- Varredura linha a linha de E-01 a E-15 além das histórias já identificadas nesta rodada (US-048, por exemplo, foi avaliada e **mantida** — fallback de polling do WebSocket não é específico de offline, é resiliência de rede em geral, ver ADR-011 revisado)
- `ADR-036`, `ADR-037`, `ADR-039` — revisão de conteúdo quanto a `Api.Edge`/`Api.Cloud` (sinalizado também na US-161)

Recomenda-se tratar essa lista como um épico de acompanhamento de uma sprint, não tentar resolvê-la em paralelo às demais histórias de E-16.

---

*US-167 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
