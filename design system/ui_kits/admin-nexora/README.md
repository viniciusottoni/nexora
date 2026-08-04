# UI kit — Admin Nexora (plataforma)

Recriação do módulo **M11 · Plataforma**, operado pela Replay. É o único produto do
ecossistema que veste a marca **Nexora** de forma explícita (logo no topo da navegação);
todos os outros vestem a marca do estabelecimento.

## Telas
| Tela | Conteúdo | Requisitos |
|---|---|---|
| Instâncias | Parque instalado com plano, versão, sync, volume e saúde; alerta de instalação atrasada | RF-PLT-01/07, RF-OFF-06 |
| Provisionar | Formulário de instância + identidade visual + módulos + checklist de implantação + prévia do tenant | RF-PLT-02/05/06, RF-CAT-12 |
| Auditoria | Trilha imutável de eventos de plataforma, inclusive acesso de suporte | RF-PLT-08, RF-AUD-01/04 |

## Decisões copiadas da especificação
- A **prévia do tenant** aplica os tokens reais (`data-tenant`) sobre componentes reais —
  é a prova de que personalização é configuração, não código (RN-016).
- Acesso de suporte aparece na auditoria e é **visível ao cliente** (RNF-SEG-13).
- Métrica-chave do produto em destaque: **tempo de implantação** (meta ≤ 5 dias úteis).
