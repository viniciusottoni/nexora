---
name: nexora-design
description: Use this skill to generate well-branded interfaces and assets for Nexora (plataforma de gestão inteligente para estabelecimentos de alimentação — mesa/PWA, garçom, KDS, caixa, painel do dono e admin multi-tenant), either for production or throwaway prototypes/mocks/etc. Contains essential design guidelines, colors, type, fonts, assets, and UI kit components for protoyping.
user-invocable: true
---

Read the README.md file within this skill, and explore the other available files.
If creating visual artifacts (slides, mocks, throwaway prototypes, etc), copy assets out and create static HTML files for the user to view. If working on production code, you can copy assets and read the rules here to become an expert in designing with this brand.
If the user invokes this skill without any other guidance, ask them what they want to build or design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_ production code, depending on the need.

## Atalhos deste sistema

- `readme.md` — contexto do produto, CONTENT FUNDAMENTALS, VISUAL FOUNDATIONS, ICONOGRAPHY e índice.
- `styles.css` — linke este arquivo; ele importa todos os tokens e fontes.
- `components/*/<Name>.prompt.md` — o que cada componente é e quando usar.
- `ui_kits/*/README.md` — mapa de telas → requisitos por produto.
- Interface em **português do Brasil**, sem emoji. Números sempre com comparativo ou meta.
- Duas marcas: **Nexora** (navy, ferramentas internas e plataforma) e **o tenant**
  (`data-tenant="…"`, PWA do cliente e canais públicos). Não misture.
