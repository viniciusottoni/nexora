# Splash + Login — Fidelidade ao Logo Real — Design Spec

**Data:** 2026-06-17
**Status:** Aprovado

---

## Contexto

O spec anterior (`2026-06-16-splash-animation-design.md`) propunha recriar a logo inteira via `CustomPainter` (incluindo olhos, sobrancelhas, chama) sobre `logo-full.png` quadrado, navegando para uma `PricingPage` landing. A implementação que existe hoje no código (`awaken_logo.dart`, `splash_page.dart`) seguiu parcialmente essa linha (100% vetorial, sem usar os PNGs reais) e o `SplashController` já foi adiantado para navegar a `/login` em vez de `/pricing-intro`.

O usuário considerou o resultado vetorial atual "horrível" e pediu fidelidade máxima ao asset real `docs/design-system/assets/logo-full.png`. Este spec **substitui** a abordagem 100% vetorial da `_AwakenMarkPainter` e expande o escopo para também alinhar a `LoginPage` aos tokens do design system.

Análise de pixel feita sobre `docs/design-system/assets/logo-mark.png` (1010×380, RGBA transparente):
- Centro da íris esquerda: ≈ (24%, 70%) da largura/altura da imagem.
- Centro da íris direita: ≈ (69%, 67%).
- A chama do olho direito **já está desenhada** no PNG (não precisa ser recriada vetorialmente) — só precisa "ganhar vida" via overlay animado.

---

## Escopo

| Arquivo | Ação |
|---|---|
| `docs/design-system/assets/logo-mark.png` → `apps/mobile/assets/images/logo-mark.png` | COPY |
| `apps/mobile/pubspec.yaml` | sem mudança (pasta `assets/images/` já declarada) |
| `apps/mobile/lib/design_system/components/awaken_logo.dart` | REWRITE (híbrido imagem + overlay) |
| `apps/mobile/lib/features/splash/presentation/pages/splash_page.dart` | MODIFY (passa `flicker` para `AwakenLogo`) |
| `apps/mobile/lib/design_system/tokens/colors.dart` | ADD tokens (border, input bg, glows) |
| `apps/mobile/lib/design_system/components/awaken_button.dart` | REWRITE primary (gradiente `--grad-energy`) |
| `apps/mobile/lib/design_system/components/awaken_text_field.dart` | CREATE (espelha `Input.jsx`) |
| `apps/mobile/lib/features/auth/presentation/pages/login_page.dart` | REWRITE (usa tokens + novos componentes) |
| `apps/mobile/test/features/splash/presentation/pages/splash_page_test.dart` | sem mudança esperada (continua usando `find.text('AWAKEN')`) |

---

## `AwakenLogo` — arquitetura híbrida

```
AwakenLogo(size, reveal: 0..1, flame: 0..1, alive: 0..1, flicker: 0..1 = 0, showText = true)
└── Stack
    ├── [0] _GlowHalo        — blur radial atrás da imagem, cor azul→roxo, intensidade = f(reveal, alive)
    ├── [1] Image.asset('assets/images/logo-mark.png') — arte real, opacity = reveal
    ├── [2] _IrisIgnitionPainter — glow pequeno nos 2 centros de íris calibrados por pixel
    │        intensidade = smoothstep(reveal, 0.86, 1.0) + alive * pulso senoidal lento
    ├── [3] _FlameLifePainter — brilho aditivo (BlendMode.plus) sobre a região da chama
    │        (bbox calibrado ≈ x:[0.55,1.0] y:[0.0,0.55] da imagem), intensidade = flame,
    │        flicker = sin(flicker * 2π * f1) * 0.5 + sin(flicker * 2π * f2) * 0.25 + 0.75
    ├── [4] _EmberParticlesPainter — ~12 partículas nascendo perto da chama, derivando pra cima,
    │        alpha = sin(fase) * flame — só "anda" quando `flicker` muda; flicker=0 → invisível
    └── [5] _Wordmark (Text 'AWAKEN' + ShaderMask gradiente azul→branco) — mantém Text real
             (acessibilidade, l10n, testes) — não é imagem.
```

**Por que não usar `logo-wordmark.png` direto:** os testes (`find.text('AWAKEN')`) e a leitura por screen reader dependem de um `Text` real. Mantemos o texto como widget, só aproximando visualmente do traço via gradiente e tracking.

**Por que `flicker` é opcional/0 por padrão:** no Login a logo fica "viva" mas estática — sem partículas piscando, sem `AnimationController` rodando — economiza bateria e evita repetir a cena de ignição. Só a `SplashPage` alimenta `flicker` crescendo continuamente durante a fase 2.

---

## Timeline (Splash) — inalterada em relação ao spec anterior

```
0 ────────────── 1200ms ──────────────────── 2800ms ── 3200ms
│ FASE 1 (reveal 0→1)        │ FASE 2 (flame 0→1, flicker rodando) │ FASE 3 (exit) │
│ fade-in geral + glow       │ chama bruxuleante, pulso, partículas │ scale/fade out │
│ no fim: íris acende        │                                      │ + navigate     │
```

- `_phase1`: `reveal` 0→1, `easeOut`, 1200ms.
- `_phase2`: `flame` 0→1 (primeiros ~300ms da fase) e `flicker` 0→1 contínuo, 1600ms.
- `_phase3`: `alive` se mantém 1, leve `scale 1→0.96` + `opacity 1→0.85`, 400ms, então `context.go(AppRoutes.login)`.

Implementado com **1 único `AnimationController`** de 3200ms (já existe em `splash_page.dart` via `_splashAnimationDuration`); fases derivadas por `Interval` — sem necessidade de 3 controllers separados como o spec antigo sugeria.

---

## Tokens novos em `colors.dart`

Espelhando `docs/design-system/tokens/colors.css`:

```dart
static const borderDefault = Color(0x1AFFFFFF);   // rgba(255,255,255,0.10)
static const borderFocus = Color(0xFF4D8BFF);     // --blue-400
static const inputBackground = Color(0x0AFFFFFF); // rgba(255,255,255,0.04)
static const glowBlue = Color(0x732D6FF5);         // rgba(45,111,245,0.45) para BoxShadow
```

---

## `AwakenButton` primary — espelha `Button.jsx`

- `primary`: `Container` com `LinearGradient(135deg, primary→primaryAlt)`, texto uppercase, `letterSpacing 0.02em`, altura 48 (md), `borderRadius 14`.
- Pressed: `AnimatedScale` 0.96 (substitui `onMouseDown` do web).
- `secondary`/`ghost`: mantêm estrutura atual (border/transparent), só ajusta radius para o token `--radius-md` (14).

## `AwakenTextField` — novo componente

Espelha `Input.jsx`: label opcional, `height: 48`, `borderRadius: 14`, `background: inputBackground`, `border` muda de `borderDefault`→`borderFocus` no foco (via `FocusNode` + `AnimatedContainer`), hint/error abaixo do campo.

## `LoginPage` — redesign

Mesma estrutura de fluxo (email, senha, botão entrar, Google, link cadastro), trocando:
- `TextField` cru → `AwakenTextField`.
- Espaçamento pelos tokens `AwakenSpacing` (já usados, sem mudança).
- Logo no topo: `AwakenLogo(size: 200, reveal: 1, flame: 1, alive: 1)` — sem `flicker` (estática viva).

---

## Performance

- `_FlameLifePainter`/`_EmberParticlesPainter` só recalculam quando `flicker` muda (`shouldRepaint` compara o valor).
- Nenhuma alocação de `Path`/`Paint`/lista de partículas dentro de `paint()` — partículas geradas uma vez (`late final List<_Ember> _embers`) no construtor do painter ou via `useMemoized`.
- Target 60fps em Android 8+; sem Lottie/Rive (overhead de parser/runtime desnecessário pra esse efeito).

---

## Testes

- `splash_page_test.dart`: sem mudança de expectativas (`find.text('AWAKEN')`, navegação para `/login`, fallback estático).
- Novo: golden/widget test simples garantindo que `AwakenLogo` renderiza `Image` com `logo-mark.png` quando `reveal >= algum valor` (smoke test, sem golden de pixel).
- `LoginPage`: novos widget tests para `AwakenTextField` (foco muda borda) — ainda não havia testes de login; adicionar testes básicos de presença dos campos/botão.
