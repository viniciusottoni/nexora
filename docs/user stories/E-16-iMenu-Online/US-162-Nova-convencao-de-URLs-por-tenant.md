# US-162 · Nova convenção de URLs por tenant

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-PLT-03 |
| **Regras de negócio** | — |
| **ADRs** | ADR-010 (revisado), ADR-040 |
| **Eventos** | — |
| **Aplicações** | `web-admin`, `web-kds`, `web-menu`, `web-pos` |
| **Autoridade do dado** | — |

---

## 1. História

> **Como** qualquer pessoa que acessa o sistema (cliente, garçom, cozinha, caixa, proprietário),
> **quero** uma URL previsível e específica para o meu papel, sempre contendo o nome do estabelecimento,
> **para** que eu saiba exatamente onde estou e a plataforma consiga resolver o tenant sem ambiguidade.

## 2. Contexto e motivação

O modelo anterior de resolução de tenant (ADR-010) era por host/subdomínio (`cardapio.donabetinha.com.br`). A nova direção de produto define uma convenção única, por caminho, sob um domínio-base compartilhado:

```
https://{base}/{tenantName}/server        → app do garçom
https://{base}/{tenantName}/kds           → app da cozinha
https://{base}/{tenantName}/pos           → app do caixa
https://{base}/{tenantName}/table/{qrCode} → app da mesa (aberto via QR Code)
https://{base}/{tenantName}/menu          → cardápio digital / delivery próprio
https://{base}/{tenantName}/admin         → app do proprietário
```

Isso simplifica a operação (uma URL-base para tudo, nome do estabelecimento sempre visível) e elimina a dependência de configuração de DNS por tenant como pré-requisito de lançamento — domínio próprio (US-143, Fase 5) continua possível como camada opcional por cima.

## 3. Escopo

### 3.1 Dentro desta história

- Roteamento por segmento de path `{tenantName}` como mecanismo primário de resolução de tenant, substituindo a resolução por host como padrão (ADR-010, nota de revisão)
- As seis rotas listadas acima, cada uma servida pelo pacote de frontend correspondente
- `web-pos` passa a responder tanto em `/server` (papel garçom) quanto em `/pos` (papel caixa) — mesmo artefato de build, rota e permissão decidindo o que é exibido, preservando o princípio de "um único build" do ADR-010
- `web-menu` passa a responder tanto em `/table/{qrCode}` (mesa) quanto em `/menu` (cardápio/delivery), pelo mesmo princípio
- Validação de `tenantName` inválido ou inexistente com página de erro clara, não crash genérico
- Atualização do endpoint de branding (ADR-010) para resolver por `tenantName` do path, não mais por `host`

### 3.2 Fora desta história

- Domínio próprio por cliente (US-143, Fase 5) — continua fora de escopo até lá
- Geração da página de QR Codes com o link completo por mesa (US-166)
- Lógica de autenticação em cada rota (US-163, US-164)

## 4. Critérios de aceite

```gherkin
Funcionalidade: URLs por tenant

  Cenário: Resolução por nome do estabelecimento
    Dado o tenant "pizzaria-dona-betinha" cadastrado
    Quando qualquer uma das seis rotas for acessada com esse tenantName
    Então o app correspondente deve carregar já identificado com a marca do estabelecimento

  Cenário: Tenant inexistente
    Dado um tenantName que não existe
    Quando qualquer rota for acessada com ele
    Então deve ser exibida uma página de erro clara
    E nenhum dado de nenhum outro tenant deve ser exposto

  Cenário: Mesma base de código, papéis distintos
    Dado o pacote web-pos publicado uma única vez
    Quando acessado em /server e em /pos do mesmo tenant
    Então deve exibir a experiência de garçom em /server
    E a experiência de caixa em /pos
    E cada uma deve respeitar a permissão do usuário autenticado naquele dispositivo

  Cenário: Mesa via QR Code
    Dado um QR Code apontando para /{tenantName}/table/{qrCode}
    Quando lido por um cliente
    Então deve abrir diretamente o app da mesa daquele tenant
    Sem exigir instalação (ADR-009)
```

## 5. Regras de negócio aplicáveis

_Não se aplica diretamente — é convenção técnica de roteamento._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /manifest.webmanifest?tenant=<slug>
GET /v1/public/branding?tenant=<slug>
# antes: /v1/public/branding?host=cardapio.donabetinha.com.br
```

## 8. Modelo de dados

Nenhuma tabela nova. `tenant.slug` (já existente, domain/01) passa a ser o valor usado no segmento `{tenantName}` da URL — recomenda-se validar que `slug` já segue formato compatível com path de URL (minúsculas, hífen, sem caracteres especiais); se não seguir, ajustar a constraint ou introduzir um campo `url_slug` derivado.

## 9. Comportamento offline

_Não se aplica — ver ADR-040._

## 10. Interface e experiência

- `{tenantName}` sempre visível na URL — reforça ao usuário em qual estabelecimento está, especialmente relevante para quem opera em mais de uma loja/rede
- Página de erro de tenant inexistente com identidade neutra (não a marca de nenhum tenant, já que não há tenant resolvido)
- Transição de rota dentro do mesmo pacote (`/server` ↔ `/pos`, `/table/{qrCode}` ↔ `/menu`) deve ser instantânea — é o mesmo bundle, não uma navegação de página inteira quando evitável

## 11. Métricas, alertas e observabilidade

- Taxa de acesso a tenant inexistente (possível sinal de QR Code mal gerado ou link quebrado)
- Distribuição de acesso por rota, por tenant

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Cada uma das seis rotas resolve o tenant correto a partir do path |
| Isolamento | Tenant inexistente ou inativo não vaza dado de nenhum outro tenant |
| E2E | QR Code de mesa abre corretamente o app da mesa, sem instalação |
| Regressão | Resolução por host (branding) continua funcionando como fallback, se `US-143`/domínio próprio já estiver em uso em algum ambiente de teste |

## 13. Dependências

**Depende de:** US-161
**Habilita:** US-163, US-164, US-165, US-166

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] As seis rotas e seus mapeamentos de pacote frontend confirmados com o time
- [ ] Decisão registrada sobre `web-pos` e `web-menu` servirem duas rotas cada a partir do mesmo build (adotada nesta história como [HIPÓTESE] — validar com o time antes de codificar)

**DoD — a história só é concluída quando:**

- [ ] As seis rotas funcionam ponta a ponta em ambiente de teste
- [ ] Branding resolve por `tenantName` do path
- [ ] Teste de isolamento multi-tenant cobrindo as novas rotas
- [ ] Documentação de API atualizada
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- **[HIPÓTESE]** esta história assume que `web-pos` deve continuar como um único pacote servindo `/server` e `/pos`, e `web-menu` servindo `/table/{qrCode}` e `/menu`, em vez de desmembrar em quatro pacotes — decisão alinhada ao princípio de "um único build" do ADR-010. Se o time preferir separar os pacotes, esta história precisa ser refeita antes da implementação.
- Nome do tenant no path expõe publicamente qual sistema um estabelecimento usa e, em alguma medida, sua URL de administração — mitigação natural é a própria autenticação em `/admin`, não a ocultação da rota.

---

*US-162 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
