# ADR-009 · PWA em vez de aplicativo nativo

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO, UX |
| **Relacionados** | ADR-010, ADR-026, ADR-027 |
| **Requisitos afetados** | RF-SAL-02, RNF-CMP-*, RNF-USA-09 |

---

## Contexto

O cliente do salão precisa acessar o cardápio ao ler o QR Code da mesa. Exigir instalação de aplicativo nesse momento é a maior barreira de adoção possível — a pessoa sentou para comer, não para baixar software. Uma taxa de adoção baixa aqui invalida boa parte do valor do produto.

Garçom, cozinha e caixa também precisam de instalação simples, sem depender de publicação em loja de aplicativos — o que atrasaria correções críticas em dias.

## Decisão

**Progressive Web App para todas as interfaces**: cardápio da mesa, delivery, garçom, KDS, caixa, painel do dono e painel da plataforma.

## Detalhamento

| Aplicação | Modo de uso | Instalação |
|---|---|---|
| `web-menu` | Navegador, via QR Code ou domínio | Nenhuma |
| `web-pos` (garçom/caixa) | Instalado na tela inicial | "Adicionar à tela de início" |
| `web-kds` | Navegador em modo quiosque | Atalho no boot do terminal |
| `web-admin` | Navegador | Opcional |

### Recursos de PWA utilizados

| Recurso | Uso |
|---|---|
| Service Worker (Workbox) | Cache de assets e do catálogo; fila offline (ADR-027) |
| Web App Manifest | Gerado dinamicamente por tenant (ADR-010) |
| Web Push (VAPID) | Alertas ao gestor e ao cliente de delivery |
| IndexedDB (Dexie) | Fila de ações e cache local |
| Wake Lock | Manter a tela do KDS ligada |

### Limitações aceitas e como são tratadas

| Limitação | Tratamento |
|---|---|
| Sem impressão térmica direta | Serviço de impressão no edge (ADR-026) |
| Push em iOS com restrições | Alertas críticos vivem nas telas operacionais, sempre visíveis; push é complemento |
| GPS em segundo plano limitado | Avaliar na Fase 4 para o entregador; possível app nativo dedicado |
| Sem acesso a hardware específico | Teclado numérico é reconhecido como teclado comum — sem problema |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| App nativo (React Native) | Acesso pleno a hardware; push confiável | Instalação obrigatória mata a adoção na mesa; publicação em loja atrasa correção | Inviável no caso de uso central |
| Híbrido: PWA público + nativo interno | Melhor de cada mundo | Duas bases, dois deploys, dois times de teste | Custo desproporcional para equipe pequena |
| Aplicativo desktop (Electron) para KDS e caixa | Acesso a impressora e periféricos | Mais um artefato a distribuir e atualizar no parque | O serviço de impressão no edge resolve sem isso |

## Consequências

**Positivas**

- Zero fricção na mesa: ler o QR Code e pedir
- Atualização instantânea, sem loja de aplicativos — correção crítica chega em minutos
- Um só código para todas as interfaces
- Instalável na tela inicial para garçom e KDS, com aparência de app
- Marca do estabelecimento no ícone e na splash (ADR-010)

**Negativas**

- Dependência do comportamento de Service Worker em navegadores diferentes
- iOS impõe limites em push e em armazenamento persistente
- Impressão exige componente adicional no edge
- Wake Lock nem sempre disponível — pode exigir configuração do sistema no terminal do KDS

**Mitigações**

- Matriz de compatibilidade testada (RNF-CMP): Chrome/Edge 110+, Safari 15+, Android 8+, iOS 14+
- Alertas críticos nunca dependem exclusivamente de push
- Terminal do KDS configurado no ato da instalação (protetor de tela desativado)

## Como validar

- Teste U-03: 5 clientes reais leem o QR e concluem o pedido sem instrução — meta ≥ 80%
- RNF-PER-03: cardápio carrega em menos de 2 s em 4G
- RNF-PER-10: bundle inicial do cardápio abaixo de 250 KB gzip
- Teste em dispositivos reais das versões mínimas suportadas

## Revisitar quando

- Fase 4: se o rastreio de entregador exigir GPS em segundo plano de forma confiável, avaliar app nativo **apenas** para esse perfil
- Se algum requisito futuro exigir hardware inacessível ao navegador
