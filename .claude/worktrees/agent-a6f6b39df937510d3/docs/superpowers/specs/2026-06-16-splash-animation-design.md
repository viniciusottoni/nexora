# Splash Screen Animada + Tela Inicial — Design Spec

**Data:** 2026-06-16
**Status:** Aprovado

---

## Contexto

A `SplashPage` atual exibe texto "AWAKEN" com fade-in via `flutter_animate`. Este spec define a evolução para uma splash épica com logo em camadas animadas (PNG base + CustomPainter), chama azul-violeta no olho direito, partículas, e transição Hero para a tela inicial de autenticação (`PricingPage` reescrita como landing).

---

## Escopo

| Arquivo | Ação |
|---|---|
| `apps/mobile/assets/images/logo-full.png` | COPY de `docs/design-system/assets/logo-full.png` |
| `apps/mobile/lib/features/splash/presentation/pages/splash_page.dart` | REWRITE |
| `apps/mobile/lib/features/splash/presentation/painters/splash_glow_painter.dart` | CREATE |
| `apps/mobile/lib/features/splash/presentation/painters/splash_flame_painter.dart` | CREATE |
| `apps/mobile/lib/features/splash/presentation/painters/splash_particle_painter.dart` | CREATE |
| `apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart` | MODIFY (delay 3200ms) |
| `apps/mobile/lib/features/pricing/presentation/pages/pricing_page.dart` | REWRITE (landing) |
| `apps/mobile/lib/l10n/app_pt.arb` | ADD 3 chaves |
| `apps/mobile/lib/l10n/app_en.arb` | ADD 3 chaves |
| `apps/mobile/lib/l10n/app_es.arb` | ADD 3 chaves |
| `apps/mobile/test/features/splash/presentation/pages/splash_page_test.dart` | UPDATE |

---

## Arquitetura da SplashPage

### Stack de camadas

```
Scaffold(backgroundPrimary)
└── Center
    └── SizedBox(width: 280, height: 280)  ← responsivo via LayoutBuilder
        └── Hero(tag: 'awaken-logo')
            └── Stack
                ├── [0] Image.asset('assets/images/logo-full.png')
                ├── [1] CustomPaint(_GlowPainter)
                ├── [2] CustomPaint(_IrisPainter)
                ├── [3] CustomPaint(_FlamePainter)
                └── [4] CustomPaint(_ParticlePainter)
```

O tamanho responsivo é `min(screenWidth * 0.7, 320)` calculado via `LayoutBuilder`.

### AnimationControllers

`_SplashAnimationState` (StatefulWidget dentro de `_AnimatedSplash`) gerencia 3 controllers:

| Controller | Duração | Inicia em | Curva padrão |
|---|---|---|---|
| `_phase1Ctrl` | 1200ms | `initState` imediato | `easeOut` |
| `_phase2Ctrl` | 1600ms | callback de `_phase1Ctrl` ao completar | linear |
| `_phase3Ctrl` | 400ms | `t = 2800ms` (callback de `_phase2Ctrl`) | `easeIn` |

`_phase3Ctrl.addStatusListener` → quando `AnimationStatus.completed`: chama `widget.onComplete()` que dispara `context.go(route)`.

Isso desacopla o timing visual do `SplashController` — o delay do controller passa a ser `Duration.zero` para visitante; a navegação é acionada pelo callback da animação. Testes continuam usando `delay: Duration.zero` sem alteração.

---

## Timeline de Animação

```
0ms ──────────── 600ms ─── 1200ms ─────────────── 2800ms ── 3200ms
│ FASE 1                   │                       │         │
│ logo: opacity 0→1        │                       │         │
│ glow: opacity 0→0.7      │                       │         │
│ glow radius: 0.5→1.0     │                       │         │
│                  FASE 2  │                       │         │
│                  íris direita: pulso rítmico     │         │
│                  chama: height 0→max (400ms)     │         │
│                  flicker: sin contínuo           │         │
│                  partículas: stagger 10 pontos   │         │
│                                          FASE 3  │         │
│                                          logo opacity →0   │
│                                          navigate + Hero   │
```

---

## Painters

### `_GlowPainter` (fase 1)

- Dois `RadialGradient` posicionados sobre cada olho (coordenadas relativas ao tamanho do widget)
- Olho esquerdo: `Color(0xFF2D6FF5)` opacidade `0→0.5`
- Olho direito: `Color(0xFF7B2FBE)` opacidade `0→0.7` (mais intenso)
- Radius do glow: `0.5→1.0` via `_phase1Ctrl`
- `BlendMode.screen` para não cobrir a logo

### `_IrisPainter` (fase 2)

- Círculo pequeno sobre a íris do olho direito (~15% do tamanho do widget)
- `RadialGradient`: branco centro → `Color(0xFF2D6FF5)` borda
- Opacity oscila entre `0.6` e `1.0` com `sin(_phase2Ctrl.value * 2π * 3)` (3 pulsos/ciclo)
- `BlendMode.screen`

### `_FlamePainter` (fase 2)

Chama composta de **3 beziers sobrepostos** com `BlendMode.screen`:

```
Bezier 1 (base, mais largo): freq 5Hz,   amplitude 0.03
Bezier 2 (médio):            freq 6.5Hz, amplitude 0.025
Bezier 3 (ponta, mais fino): freq 8Hz,   amplitude 0.02
```

Cada bezier é um `Path` com `cubicTo` cujos control points oscilam:
```
cpX = baseX + sin(t * freq + offset) * amplitude * logoWidth
cpY = baseY - sin(t * freq * 0.7) * amplitude * logoHeight
```

Gradiente linear base→topo: `[Color(0xFF2D6FF5), Color(0xFF7B2FBE), Colors.white]`

Altura máxima da chama: `0.35 * logoHeight`. Cresce de `0→max` nos primeiros 400ms da fase 2 via `CurvedAnimation(easeOut)`.

Posicionamento: centrado sobre o olho direito (~60% da largura, ~25% da altura da logo).

### `_ParticlePainter` (fase 2)

10 partículas com propriedades fixas geradas no `initState`:

```dart
class _Particle {
  final double x;           // posição X relativa (0.0–1.0)
  final double birthTime;   // t dentro da fase 2 (0.0–0.9)
  final double speed;       // velocidade Y (0.15–0.35)
  final double radius;      // raio (2.0–4.0)
  final double drift;       // drift lateral (-0.02–+0.02)
}
```

Para cada partícula ativa (`t > birthTime`):
- `life = (t - birthTime) / (1.0 - birthTime)` (0→1)
- `posY = startY - life * speed * logoHeight`
- `opacity = (1.0 - life) * 0.8`
- Cor: `Color(0xFF2D6FF5)` com 20% chance de `Color(0xFF7B2FBE)`

---

## Responsividade

```dart
final logoSize = (MediaQuery.sizeOf(context).width * 0.7).clamp(160.0, 320.0);
```

Painters recebem `size` via `paint(Canvas canvas, Size size)` — todas as coordenadas são **proporcionais** (`x * size.width`, `y * size.height`). Nunca hardcoded em pixels.

---

## Fallback estático

`splashAnimationsEnabledProvider = false` → `_StaticSplash`:
- `Image.asset('assets/images/logo-full.png')` em tamanho fixo
- Sem painters, sem controllers
- Mantém compatibilidade com testes de widget existentes

---

## PricingPage (tela inicial)

### Layout

```
SafeArea
└── Column
    ├── Expanded(flex: 3)
    │   └── Center
    │       └── Hero(tag: 'awaken-logo')
    │           └── Column
    │               ├── Image.asset(logo, width: logoSize)
    │               │   + BoxShadow glow estático
    │               └── SizedBox(height: 16)
    │               └── Text(tagline, bodyMedium, textSecondary)
    └── Expanded(flex: 2)
        └── Padding(horizontal: 24)
            └── Column
                ├── AwakenButton('Entrar') → context.go(AppRoutes.login)
                ├── SizedBox(height: 12)
                └── Row
                    ├── Text(l10n.landingNoAccount, textMuted)
                    └── TextButton(l10n.landingCreateAccount) → /register
```

### Glow estático na logo (landing)

```dart
BoxDecoration(
  boxShadow: [
    BoxShadow(
      color: AwakenColors.primary.withOpacity(0.25),
      blurRadius: 40,
      spreadRadius: 8,
    ),
    BoxShadow(
      color: AwakenColors.primaryAlt.withOpacity(0.15),
      blurRadius: 60,
      spreadRadius: 4,
    ),
  ],
)
```

### ARB keys novas

| Chave | pt-BR | en | es |
|---|---|---|---|
| `landingEnter` | `Entrar` | `Sign In` | `Entrar` |
| `landingCreateAccount` | `Criar conta` | `Create account` | `Crear cuenta` |
| `landingNoAccount` | `Ainda não tem conta?` | `Don't have an account?` | `¿Aún no tienes cuenta?` |

---

## SplashController — mudança de delay

O controller não controla mais o timing visual. `initialize()` remove o `Future.delayed` e retorna a rota imediatamente após eventos de analytics. A navegação é acionada pelo callback `onComplete` da animação (ao fim da fase 3).

```dart
// Antes
Duration delay = const Duration(milliseconds: 1500)

// Depois
Duration delay = Duration.zero  // analytics only, timing driven by animation
```

Testes: sem impacto (já usavam `delay: Duration.zero`).

---

## Testes

### Widget tests existentes — atualizações necessárias

- `CA-001 exibe marca AWAKEN`: adicionar `find.byType(Image)` além do Hero
- `CA-002 navega para pricingIntro`: ajustar para esperar callback da animação (já coberto por `pumpAndSettle`)
- `CA-003 fallback estático`: sem mudança
- Adicionar: `CA-001 exibe logo PNG na landing`
- Adicionar: `CA-005 landing tem botão Entrar`
- Adicionar: `CA-006 landing tem link Criar conta`

### Performance

- Target: 60fps em devices mid-range (Android 8+)
- `CustomPainter.shouldRepaint` retorna `true` apenas quando `animationValue` muda
- Painters não alocam objetos no método `paint` — `Paint`, `Path` e `List<Particle>` criados no `initState` e reutilizados

---

## Diagrama de dependências

```
SplashPage
├── _AnimatedSplash (StatefulWidget)
│   ├── _GlowPainter       ← _phase1Ctrl.value
│   ├── _IrisPainter       ← _phase2Ctrl.value
│   ├── _FlamePainter      ← _phase2Ctrl.value
│   └── _ParticlePainter   ← _phase2Ctrl.value, List<_Particle>
└── _StaticSplash

PricingPage
└── AwakenButton (design_system/components)
```
