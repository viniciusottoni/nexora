# US-003 · Identidade visual por estabelecimento

|  |  |
|---|---|
| **Épico** | [E-00 · Fundacao da Plataforma](./README.md) |
| **Fase** | 0 — Fundação da plataforma |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-PLT-02, RF-PLT-04 |
| **Regras de negócio** | RN-016 |
| **ADRs** | ADR-010, ADR-013, ADR-030 |
| **Eventos** | EVT-055 |
| **Aplicações** | web-menu, web-pos, web-kds, web-admin, api-cloud, packages/ui |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que toda a camada web use a marca do meu negócio,
> **para** que meus clientes vejam meu estabelecimento, e não o fornecedor do sistema.

## 2. Contexto e motivação

A diretriz 2 do produto é explícita: *toda a camada web personalizável*. E o princípio arquitetural que a sustenta é igualmente explícito (doc. 02, seção 5): **nunca gerar build por cliente**. Um único artefato serve todos os estabelecimentos; a identidade é carregada em runtime.

O mecanismo é CSS custom properties aplicadas no `:root` a partir de uma resposta de API, mais manifest PWA gerado por tenant. É o que permite que a Replay tenha um pipeline de deploy só, e não N.

Violar isso é o caminho mais rápido para destruir a escalabilidade do produto — motivo pelo qual o ADR-013 proíbe código por cliente e a US-007 coloca essa proibição no CI.

## 3. Escopo

### 3.1 Dentro desta história

- Endpoint público `GET /v1/public/branding` resolvendo tenant pelo host
- Aplicação de cores, tipografia e raio de borda via CSS custom properties no `:root`
- Logo em versão clara e escura, favicon e ícone do PWA
- Manifest PWA gerado dinamicamente por tenant (nome, ícone, splash, cor de tema)
- Textos públicos configuráveis (boas-vindas, confirmação, agradecimento, termos)
- Upload de mídia para object storage com CDN
- Propagação da alteração em até 60 segundos, sem deploy
- Design tokens no `packages/ui` consumindo as variáveis

### 3.2 Fora desta história

- Domínio próprio por cliente (US-143, Fase 5)
- Editor visual de temas com pré-visualização ao vivo (Fase 5)
- Arte personalizada de QR Code (US-020 trata do QR funcional; a arte fica na Fase 5)
- Personalização de layout ou de fluxo — só identidade, nunca estrutura

## 4. Critérios de aceite

```gherkin
Funcionalidade: Identidade visual em runtime

  Cenário: Aplicação de marca sem build específico
    Dado um tenant com cores, logo e tipografia configurados
    Quando qualquer aplicação web for carregada para esse tenant
    Então as cores devem ser aplicadas via CSS custom properties no :root
    E o logo e o ícone do PWA devem ser os do tenant
    E nenhum build específico deve ter sido gerado para esse cliente

  Cenário: Alteração de marca sem deploy
    Dado que o gestor alterou a cor primária no painel
    Quando qualquer aplicação for recarregada
    Então a nova cor deve estar aplicada em até 60 segundos
    E nenhum artefato deve ter sido publicado

  Cenário: Resolução de tenant pelo host
    Dado o host "cardapio.donabetinha.com.br" mapeado ao tenant A
    Quando a aplicação pública carregar
    Então o branding retornado deve ser o do tenant A
    E nenhuma marca da Replay deve aparecer em primeiro plano

  Cenário: Ausência de configuração
    Dado um tenant recém-criado sem logo enviado
    Quando a aplicação carregar
    Então deve ser aplicado o tema neutro padrão do produto
    E a aplicação deve funcionar normalmente

  Cenário: Contraste mínimo garantido
    Dado que o gestor escolheu uma cor primária de baixo contraste sobre a superfície
    Quando salvar a configuração
    Então o sistema deve avisar sobre a falha de contraste WCAG AA
    E deve oferecer uma variação corrigida da cor

  Cenário: Manifest PWA por tenant
    Dado um cliente que instalou o PWA do cardápio
    Quando o ícone aparecer na tela inicial do celular
    Então deve ser o ícone do estabelecimento, com o nome do estabelecimento
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Regra específica de negócio é configuração, nunca código | Identidade visual é dado em `tenant_config`, lido em runtime |
| RN-015 | Isolamento entre estabelecimentos | O endpoint público resolve exatamente um tenant pelo host e não expõe lista |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-055 | `tenant.branding_updated` | Identidade visual alterada | changedKeys[], configVersion | ↓ |
| EVT-054 | `tenant.config_updated` | Textos públicos alterados | changedKeys[], configVersion | ↓ |

## 7. Contrato de API

```http
GET /v1/public/branding?host=cardapio.donabetinha.com.br
→ 200 {
    "tenant": { "id": "...", "name": "Pizzaria Dona Betinha" },
    "branding": {
      "colors": { "primary": "#C1121F", "secondary": "#669BBC",
                  "surface": "#FDF0D5", "onPrimary": "#FFFFFF" },
      "logo":   { "light": "https://cdn/.../logo-light.svg",
                  "dark":  "https://cdn/.../logo-dark.svg" },
      "favicon": "https://cdn/.../favicon.png",
      "fonts":  { "body": "Inter", "display": "Fraunces" },
      "radius": 12,
      "texts":  { "welcome": "...", "orderConfirmed": "...", "terms": "..." },
      "pwa":    { "name": "...", "shortName": "...", "themeColor": "#C1121F",
                  "icons": [...] }
    },
    "configVersion": 88
  }

GET   /v1/tenant/branding.webmanifest     # manifest PWA dinâmico
PATCH /v1/tenant/branding                 { "colors": { "primary": "#C1121F" } }
POST  /v1/tenant/branding/logo            (multipart)
```

> A resposta é cacheável por 60 s no CDN e invalidada por `configVersion` — é isso que sustenta o critério de 60 segundos.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `tenant_config` | Guarda o objeto de branding | `branding` (JSONB), `config_version`, `updated_at` |
| `media_asset` | Referência dos arquivos no object storage | `kind`, `url`, `content_type`, `bytes` |
| `tenant` | Resolução por host/slug | `slug`, `custom_domain` |

## 9. Comportamento offline

As aplicações da loja (`web-pos`, `web-kds`) fazem cache do branding no Service Worker na primeira carga e seguem usando a versão em cache enquanto estiverem offline. Uma alteração de marca feita durante uma queda de internet só chega quando o pull de configuração voltar (US-063) — comportamento aceitável, porque identidade visual não é dado operacional crítico.

O cardápio público (`web-menu`) depende de internet por natureza e não tem caminho offline nesta história.

## 10. Interface e experiência

- Nenhuma marca da Replay em primeiro plano nas telas voltadas ao cliente final
- Tema neutro de fallback funcional e apresentável, para o intervalo entre criação do tenant e envio da marca
- Aviso de contraste WCAG AA no momento da escolha de cor, com sugestão de correção — acessibilidade não pode depender do gosto do cliente
- Pré-visualização das telas principais com a marca aplicada antes de salvar
- Upload com recorte assistido para logo e ícone

## 11. Métricas, alertas e observabilidade

- Tempo entre `tenant.branding_updated` e a primeira renderização com a nova marca — meta ≤ 60 s
- Contagem de tenants com marca completa versus tema padrão — indicador de qualidade de onboarding
- Erro de carregamento de branding deve degradar para o tema padrão, nunca quebrar a aplicação

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Geração das CSS custom properties e do manifest a partir do objeto de branding |
| Unitário | Cálculo de contraste WCAG AA e sugestão de cor corrigida |
| Integração | Resolução de tenant por host; host desconhecido retorna 404 |
| Visual | Dois tenants com marcas distintas renderizados a partir do mesmo artefato de build |
| E2E | Alteração de cor no painel refletida na aplicação pública em menos de 60 s |
| Governança | Busca no código por valores de marca embutidos falha o CI (ADR-013) |

## 13. Dependências

**Depende de:** US-002  
**Habilita:** US-021, US-130, US-143

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

## 15. Riscos, premissas e pendências

- **Dependência externa** — identidade visual do cliente (logo, cores, fontes) é insumo que o cliente precisa entregar; sem ela, a personalização fica no tema padrão (PRD, seção 8).
- Fonte personalizada por tenant impacta desempenho de carregamento do cardápio (meta de 2 s em 4G); limitar a um conjunto de fontes servidas pelo CDN.

---

*US-003 · Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*