# Splash + Login Fidelidade ao Logo Real Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir a logo 100% vetorial da splash/login por uma abordagem híbrida (PNG real `logo-mark.png` + overlay animado de glow/chama/partículas calibrado por análise de pixel), trocar a navegação pós-splash para a `LoginPage`, e alinhar `LoginPage` aos tokens do design system (`AwakenButton` com gradiente, novo `AwakenTextField`).

**Architecture:** `AwakenLogo` passa a renderizar `Image.asset('assets/images/logo-mark.png')` como camada base (fiel ao traço real) com 4 `CustomPainter`s sobrepostos (`_GlowHaloPainter`, `_IrisIgnitionPainter`, `_FlameLifePainter`, `_EmberParticlesPainter`) cuja intensidade é dirigida pelos parâmetros existentes `reveal`/`flame`/`alive` mais um novo parâmetro opcional `flicker` (0 por padrão — login fica "vivo" mas estático; splash alimenta `flicker` continuamente). O wordmark "AWAKEN" continua sendo um `Text` real (não imagem) para preservar acessibilidade, localização e os testes existentes que usam `find.text('AWAKEN')`.

**Tech Stack:** Flutter/Dart, `CustomPainter`, `AnimatedContainer`/`AnimatedScale` (sem pacotes novos — sem Lottie/Rive).

---

## Mapa de arquivos

| Ação | Arquivo |
|---|---|
| COPY | `apps/mobile/assets/images/logo-mark.png` (de `docs/design-system/assets/logo-mark.png`) |
| REWRITE | `apps/mobile/lib/design_system/components/awaken_logo.dart` |
| CREATE | `apps/mobile/test/design_system/components/awaken_logo_test.dart` |
| MODIFY | `apps/mobile/lib/design_system/tokens/colors.dart` |
| REWRITE | `apps/mobile/lib/design_system/components/awaken_button.dart` |
| CREATE | `apps/mobile/test/design_system/components/awaken_button_test.dart` |
| CREATE | `apps/mobile/lib/design_system/components/awaken_text_field.dart` |
| CREATE | `apps/mobile/test/design_system/components/awaken_text_field_test.dart` |
| REWRITE | `apps/mobile/lib/features/auth/presentation/pages/login_page.dart` |
| CREATE | `apps/mobile/test/features/auth/presentation/pages/login_page_test.dart` |
| MODIFY | `apps/mobile/lib/features/splash/presentation/pages/splash_page.dart` |

---

## Task 1: Copiar asset real da logo

**Files:**
- Create: `apps/mobile/assets/images/logo-mark.png`

- [ ] **Step 1.1: Copiar o PNG**

```bash
cp "docs/design-system/assets/logo-mark.png" "apps/mobile/assets/images/logo-mark.png"
```

- [ ] **Step 1.2: Confirmar que o pubspec já declara a pasta**

Abrir `apps/mobile/pubspec.yaml` e confirmar que existe:

```yaml
  assets:
    - assets/images/
```

Isso já cobre o novo arquivo (sem precisar listar arquivo a arquivo). Nenhuma edição necessária.

- [ ] **Step 1.3: Commit**

```bash
git add apps/mobile/assets/images/logo-mark.png
git commit -m "feat: adiciona asset real do mark da logo (transparente)"
```

---

## Task 2: Tokens de cor novos (border/input/glow)

**Files:**
- Modify: `apps/mobile/lib/design_system/tokens/colors.dart`

- [ ] **Step 2.1: Adicionar os tokens no final da classe `AwakenColors`**

Em `apps/mobile/lib/design_system/tokens/colors.dart`, antes do método estático `forRank` (logo após o bloco de `rankSSS`), adicionar:

```dart
  // Borders & inputs (espelha docs/design-system/tokens/colors.css)
  static const borderDefault = Color(0x1AFFFFFF); // rgba(255,255,255,0.10)
  static const borderFocus = Color(0xFF4D8BFF); // --blue-400
  static const inputBackground = Color(0x0AFFFFFF); // rgba(255,255,255,0.04)
  static const glowBlue = Color(0x732D6FF5); // rgba(45,111,245,0.45)
```

- [ ] **Step 2.2: Verificar análise estática**

```bash
cd apps/mobile && flutter analyze lib/design_system/tokens/colors.dart
```

Esperado: `No issues found!`

- [ ] **Step 2.3: Commit**

```bash
git add apps/mobile/lib/design_system/tokens/colors.dart
git commit -m "feat: adiciona tokens de border/input/glow ao AwakenColors"
```

---

## Task 3: Reescrever `AwakenLogo` (híbrido imagem + overlay)

**Files:**
- Modify: `apps/mobile/lib/design_system/components/awaken_logo.dart`
- Create: `apps/mobile/test/design_system/components/awaken_logo_test.dart`

- [ ] **Step 3.1: Escrever o teste (falha contra a implementação atual, que não usa `Image`)**

```dart
// apps/mobile/test/design_system/components/awaken_logo_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:awaken/design_system/components/awaken_logo.dart';

void main() {
  group('AwakenLogo', () {
    testWidgets('renderiza a imagem real do mark e o wordmark em texto',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(home: AwakenLogo(size: 200)),
      );
      await tester.pump();

      expect(find.byType(Image), findsOneWidget);
      expect(find.text('AWAKEN'), findsOneWidget);
    });

    testWidgets('oculta o wordmark quando showText é false', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: AwakenLogo(size: 200, showText: false),
        ),
      );
      await tester.pump();

      expect(find.text('AWAKEN'), findsNothing);
    });

    testWidgets('não lança erro com flame e flicker ativos', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: AwakenLogo(
            size: 200,
            reveal: 1,
            flame: 1,
            alive: 1,
            flicker: 0.5,
          ),
        ),
      );
      await tester.pump();

      expect(tester.takeException(), isNull);
    });
  });
}
```

- [ ] **Step 3.2: Rodar o teste e confirmar falha**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_logo_test.dart --reporter=expanded
```

Esperado: FAIL no primeiro teste (`find.byType(Image)` não encontra nada — a implementação atual é 100% `CustomPaint`, sem `Image`).

- [ ] **Step 3.3: Reescrever `awaken_logo.dart`**

```dart
// apps/mobile/lib/design_system/components/awaken_logo.dart
import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../tokens/colors.dart';

/// Awaken brand mark — camada de imagem real (`logo-mark.png`) com overlay
/// animado de glow/chama/partículas calibrado por análise de pixel do asset.
class AwakenLogo extends StatelessWidget {
  const AwakenLogo({
    super.key,
    this.size = 320,
    this.reveal = 1,
    this.flame = 0,
    this.alive = 0,
    this.flicker = 0,
    this.showText = true,
  });

  /// Largura alvo do mark (a altura é derivada da proporção real do PNG).
  final double size;

  /// Progresso de revelação geral (fade-in), 0..1.
  final double reveal;

  /// Intensidade da chama do olho direito, 0..1.
  final double flame;

  /// "Vivacidade" geral (pulso de íris contínuo), 0..1.
  final double alive;

  /// Fase contínua usada para flicker/partículas. 0 = estático (sem
  /// partículas, chama com brilho constante). A SplashPage alimenta um
  /// valor crescente; a LoginPage deixa no padrão (0) para ficar viva sem
  /// repetir a animação de ignição.
  final double flicker;

  final bool showText;

  /// Proporção real de `logo-mark.png` (1010x380 px).
  static const _markAspectRatio = 380 / 1010;

  @override
  Widget build(BuildContext context) {
    final safeReveal = reveal.clamp(0.0, 1.0).toDouble();
    final safeFlame = flame.clamp(0.0, 1.0).toDouble();
    final safeAlive = alive.clamp(0.0, 1.0).toDouble();
    final width = size.clamp(160.0, 420.0).toDouble();
    final height = width * _markAspectRatio;

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        SizedBox(
          width: width,
          height: height,
          child: Stack(
            fit: StackFit.expand,
            children: [
              CustomPaint(
                painter: _GlowHaloPainter(
                  reveal: safeReveal,
                  alive: safeAlive,
                  flame: safeFlame,
                ),
              ),
              Opacity(
                opacity: safeReveal,
                child: Image.asset(
                  'assets/images/logo-mark.png',
                  fit: BoxFit.contain,
                ),
              ),
              CustomPaint(
                painter: _IrisIgnitionPainter(
                  reveal: safeReveal,
                  alive: safeAlive,
                ),
              ),
              if (safeFlame > 0)
                CustomPaint(
                  painter: _FlameLifePainter(
                    flame: safeFlame,
                    flicker: flicker,
                  ),
                ),
              if (safeFlame > 0)
                CustomPaint(
                  painter: _EmberParticlesPainter(
                    flame: safeFlame,
                    flicker: flicker,
                  ),
                ),
            ],
          ),
        ),
        if (showText) ...[
          const SizedBox(height: 10),
          Opacity(
            opacity: safeReveal,
            child: _Wordmark(glow: safeAlive),
          ),
        ],
      ],
    );
  }
}

class _GlowHaloPainter extends CustomPainter {
  const _GlowHaloPainter({
    required this.reveal,
    required this.alive,
    required this.flame,
  });

  final double reveal;
  final double alive;
  final double flame;

  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final intensity = (0.35 * reveal) + (0.35 * alive) + (0.3 * flame);
    if (intensity <= 0) return;

    final radius = size.width * 0.6;
    final paint = Paint()
      ..shader = RadialGradient(
        colors: [
          AwakenColors.primary.withValues(alpha: 0.30 * intensity),
          AwakenColors.primaryAlt.withValues(alpha: 0.18 * intensity),
          Colors.transparent,
        ],
        stops: const [0.0, 0.55, 1.0],
      ).createShader(Rect.fromCircle(center: center, radius: radius))
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 18);
    canvas.drawCircle(center, radius, paint);
  }

  @override
  bool shouldRepaint(covariant _GlowHaloPainter oldDelegate) {
    return oldDelegate.reveal != reveal ||
        oldDelegate.alive != alive ||
        oldDelegate.flame != flame;
  }
}

/// Acende as íris nos centros calibrados por análise de pixel de
/// `docs/design-system/assets/logo-mark.png`: esquerda ≈(24%,70%),
/// direita ≈(69%,67%) da largura/altura do mark.
class _IrisIgnitionPainter extends CustomPainter {
  const _IrisIgnitionPainter({required this.reveal, required this.alive});

  final double reveal;
  final double alive;

  static const _leftIrisFrac = Offset(0.24, 0.70);
  static const _rightIrisFrac = Offset(0.69, 0.67);

  @override
  void paint(Canvas canvas, Size size) {
    final ignition = _smoothStep(
      ((reveal - 0.86) / 0.14).clamp(0.0, 1.0).toDouble(),
    );
    if (ignition <= 0 && alive <= 0) return;

    final pulse = 0.5 + (math.sin(alive * math.pi * 6) * 0.5);
    final intensity = (ignition + (alive * 0.35 * pulse)).clamp(0.0, 1.0);

    _paintIris(canvas, size, _leftIrisFrac, AwakenColors.primary, intensity);
    _paintIris(
        canvas, size, _rightIrisFrac, AwakenColors.primaryAlt, intensity);
  }

  void _paintIris(
    Canvas canvas,
    Size size,
    Offset frac,
    Color color,
    double intensity,
  ) {
    final center = Offset(size.width * frac.dx, size.height * frac.dy);
    final radius = size.width * 0.05;
    final paint = Paint()
      ..shader = RadialGradient(
        colors: [
          Colors.white.withValues(alpha: 0.9 * intensity),
          color.withValues(alpha: 0.7 * intensity),
          Colors.transparent,
        ],
      ).createShader(Rect.fromCircle(center: center, radius: radius * 2))
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 6);
    canvas.drawCircle(center, radius * 1.4, paint);
  }

  double _smoothStep(double v) => v * v * (3 - 2 * v);

  @override
  bool shouldRepaint(covariant _IrisIgnitionPainter oldDelegate) {
    return oldDelegate.reveal != reveal || oldDelegate.alive != alive;
  }
}

/// Brilho aditivo sobre a região da chama já desenhada no PNG (a chama
/// não é redesenhada — só "ganha vida" via flicker). Bbox calibrado por
/// análise de pixel: x:[0.55,1.0] y:[0.0,0.55] do mark.
class _FlameLifePainter extends CustomPainter {
  const _FlameLifePainter({required this.flame, required this.flicker});

  final double flame;
  final double flicker;

  @override
  void paint(Canvas canvas, Size size) {
    final flicker1 = math.sin(flicker * math.pi * 18) * 0.5;
    final flicker2 = math.sin(flicker * math.pi * 31) * 0.25;
    final brightness = (flicker1 + flicker2 + 0.75).clamp(0.3, 1.3).toDouble();

    final rect = Rect.fromLTWH(
      size.width * 0.55,
      0,
      size.width * 0.45,
      size.height * 0.55,
    );
    final paint = Paint()
      ..blendMode = BlendMode.plus
      ..shader = RadialGradient(
        colors: [
          AwakenColors.textPrimary.withValues(alpha: 0.5 * flame * brightness),
          AwakenColors.primaryAlt.withValues(alpha: 0.4 * flame * brightness),
          Colors.transparent,
        ],
      ).createShader(
        Rect.fromCircle(center: rect.center, radius: rect.longestSide * 0.6),
      )
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 14);
    canvas.drawOval(rect, paint);
  }

  @override
  bool shouldRepaint(covariant _FlameLifePainter oldDelegate) {
    return oldDelegate.flame != flame || oldDelegate.flicker != flicker;
  }
}

class _EmberParticlesPainter extends CustomPainter {
  const _EmberParticlesPainter({required this.flame, required this.flicker});

  final double flame;
  final double flicker;

  static const _count = 12;

  @override
  void paint(Canvas canvas, Size size) {
    for (var i = 0; i < _count; i++) {
      final phase = (flicker + (i * 0.083)) % 1.0;
      final originX = size.width * (0.62 + (i % 4) * 0.07);
      const originYFrac = 0.5;
      final drift = math.sin((phase * math.pi * 2) + i) * size.width * 0.02;
      final x = originX + drift;
      final y = (size.height * originYFrac) - (phase * size.height * 0.5);
      final fade = math.sin(phase * math.pi).clamp(0.0, 1.0).toDouble();
      final radius = 1.2 + ((i % 3) * 0.7);

      canvas.drawCircle(
        Offset(x, y),
        radius,
        Paint()
          ..color = Color.lerp(
            AwakenColors.primary,
            AwakenColors.primaryAlt,
            i / _count,
          )!
              .withValues(alpha: fade * flame * 0.7)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 2),
      );
    }
  }

  @override
  bool shouldRepaint(covariant _EmberParticlesPainter oldDelegate) {
    return oldDelegate.flame != flame || oldDelegate.flicker != flicker;
  }
}

class _Wordmark extends StatelessWidget {
  const _Wordmark({required this.glow});

  final double glow;

  @override
  Widget build(BuildContext context) {
    return ShaderMask(
      shaderCallback: (bounds) => const LinearGradient(
        colors: [AwakenColors.textPrimary, AwakenColors.primary],
      ).createShader(bounds),
      child: Text(
        'AWAKEN',
        style: TextStyle(
          fontSize: 40,
          fontWeight: FontWeight.w900,
          color: AwakenColors.textPrimary,
          letterSpacing: 6,
          shadows: [
            Shadow(
              color: AwakenColors.primary.withValues(alpha: 0.6 * glow),
              blurRadius: 16,
            ),
            Shadow(
              color: AwakenColors.primaryAlt.withValues(alpha: 0.5 * glow),
              blurRadius: 26,
            ),
          ],
        ),
      ),
    );
  }
}
```

- [ ] **Step 3.4: Rodar o teste e confirmar sucesso**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_logo_test.dart --reporter=expanded
```

Esperado: 3 testes PASS.

- [ ] **Step 3.5: Análise estática**

```bash
cd apps/mobile && flutter analyze lib/design_system/components/awaken_logo.dart
```

Esperado: `No issues found!`

- [ ] **Step 3.6: Commit**

```bash
git add apps/mobile/lib/design_system/components/awaken_logo.dart apps/mobile/test/design_system/components/awaken_logo_test.dart
git commit -m "feat: AwakenLogo usa imagem real do mark com overlay animado de glow/chama"
```

---

## Task 4: `AwakenButton` — gradiente energy + glow

**Files:**
- Modify: `apps/mobile/lib/design_system/components/awaken_button.dart`
- Create: `apps/mobile/test/design_system/components/awaken_button_test.dart`

- [ ] **Step 4.1: Escrever os testes**

```dart
// apps/mobile/test/design_system/components/awaken_button_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:awaken/design_system/components/awaken_button.dart';
import 'package:awaken/design_system/tokens/colors.dart';

void main() {
  group('AwakenButton', () {
    testWidgets('variante primary usa gradiente energy (primary→primaryAlt)',
        (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: AwakenButton(label: 'Entrar', onPressed: () {}),
          ),
        ),
      );

      final container = tester.widget<AnimatedContainer>(
        find.byType(AnimatedContainer),
      );
      final decoration = container.decoration as BoxDecoration;
      final gradient = decoration.gradient as LinearGradient;
      expect(gradient.colors.first, AwakenColors.primary);
      expect(gradient.colors.last, AwakenColors.primaryAlt);
    });

    testWidgets('dispara onPressed ao tocar quando habilitado', (
      tester,
    ) async {
      var tapped = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: AwakenButton(
              label: 'Entrar',
              onPressed: () => tapped = true,
            ),
          ),
        ),
      );

      await tester.tap(find.text('Entrar'));
      await tester.pump();

      expect(tapped, isTrue);
    });

    testWidgets('não dispara onPressed quando isLoading é true', (
      tester,
    ) async {
      var tapped = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: AwakenButton(
              label: 'Entrar',
              isLoading: true,
              onPressed: () => tapped = true,
            ),
          ),
        ),
      );

      await tester.tap(find.byType(AwakenButton));
      await tester.pump();

      expect(tapped, isFalse);
    });
  });
}
```

- [ ] **Step 4.2: Rodar e confirmar falha**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_button_test.dart --reporter=expanded
```

Esperado: FAIL no primeiro teste — a implementação atual usa `ElevatedButton` com tema global, não `AnimatedContainer` com `LinearGradient` explícito.

- [ ] **Step 4.3: Reescrever `awaken_button.dart`**

```dart
// apps/mobile/lib/design_system/components/awaken_button.dart
import 'package:flutter/material.dart';
import '../tokens/colors.dart';
import '../tokens/spacing.dart';
import '../tokens/typography.dart';

enum AwakenButtonVariant { primary, secondary, ghost }

class AwakenButton extends StatefulWidget {
  const AwakenButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.variant = AwakenButtonVariant.primary,
    this.isLoading = false,
    this.icon,
  });

  final String label;
  final VoidCallback? onPressed;
  final AwakenButtonVariant variant;
  final bool isLoading;
  final Widget? icon;

  @override
  State<AwakenButton> createState() => _AwakenButtonState();
}

class _AwakenButtonState extends State<AwakenButton> {
  bool _pressed = false;

  bool get _enabled => widget.onPressed != null && !widget.isLoading;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTapDown: _enabled ? (_) => setState(() => _pressed = true) : null,
      onTapCancel: _enabled ? () => setState(() => _pressed = false) : null,
      onTapUp: _enabled ? (_) => setState(() => _pressed = false) : null,
      onTap: _enabled ? widget.onPressed : null,
      child: AnimatedScale(
        scale: _pressed ? 0.96 : 1.0,
        duration: const Duration(milliseconds: 120),
        curve: Curves.easeOut,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 140),
          width: double.infinity,
          height: 48,
          alignment: Alignment.center,
          decoration: _decorationFor(widget.variant, _enabled),
          child: _content(),
        ),
      ),
    );
  }

  BoxDecoration _decorationFor(AwakenButtonVariant variant, bool enabled) {
    final opacity = enabled ? 1.0 : 0.4;
    final radius = BorderRadius.circular(AwakenSpacing.buttonRadius + 2);
    switch (variant) {
      case AwakenButtonVariant.primary:
        return BoxDecoration(
          borderRadius: radius,
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [
              AwakenColors.primary.withValues(alpha: opacity),
              AwakenColors.primaryAlt.withValues(alpha: opacity),
            ],
          ),
          boxShadow: enabled
              ? [
                  BoxShadow(
                    color: AwakenColors.glowBlue,
                    blurRadius: 20,
                    spreadRadius: -4,
                  ),
                ]
              : null,
        );
      case AwakenButtonVariant.secondary:
        return BoxDecoration(
          borderRadius: radius,
          color: AwakenColors.cardElevated.withValues(alpha: opacity),
          border: Border.all(color: AwakenColors.borderDefault),
        );
      case AwakenButtonVariant.ghost:
        return BoxDecoration(borderRadius: radius);
    }
  }

  Widget _content() {
    if (widget.isLoading) {
      return const SizedBox.square(
        dimension: 20,
        child: CircularProgressIndicator(
          strokeWidth: 2,
          color: AwakenColors.textPrimary,
        ),
      );
    }
    final textStyle = AwakenTypography.labelLarge.copyWith(letterSpacing: 1.2);
    if (widget.icon != null) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          widget.icon!,
          const SizedBox(width: 8),
          Text(widget.label, style: textStyle),
        ],
      );
    }
    return Text(widget.label, style: textStyle);
  }
}
```

- [ ] **Step 4.4: Rodar e confirmar sucesso**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_button_test.dart --reporter=expanded
```

Esperado: 3 testes PASS.

- [ ] **Step 4.5: Análise estática**

```bash
cd apps/mobile && flutter analyze lib/design_system/components/awaken_button.dart
```

Esperado: `No issues found!`

- [ ] **Step 4.6: Commit**

```bash
git add apps/mobile/lib/design_system/components/awaken_button.dart apps/mobile/test/design_system/components/awaken_button_test.dart
git commit -m "feat: AwakenButton primary usa gradiente energy com glow e press scale"
```

---

## Task 5: `AwakenTextField` — novo componente

**Files:**
- Create: `apps/mobile/lib/design_system/components/awaken_text_field.dart`
- Create: `apps/mobile/test/design_system/components/awaken_text_field_test.dart`

- [ ] **Step 5.1: Escrever os testes**

```dart
// apps/mobile/test/design_system/components/awaken_text_field_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:awaken/design_system/components/awaken_text_field.dart';
import 'package:awaken/design_system/tokens/colors.dart';

void main() {
  group('AwakenTextField', () {
    testWidgets('exibe o label e o campo de texto', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: AwakenTextField(label: 'E-mail')),
        ),
      );

      expect(find.text('E-mail'), findsOneWidget);
      expect(find.byType(TextField), findsOneWidget);
    });

    testWidgets('borda fica com cor de foco ao focar o campo', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: AwakenTextField(label: 'E-mail')),
        ),
      );

      await tester.tap(find.byType(TextField));
      await tester.pump();

      final container = tester.widget<AnimatedContainer>(
        find.byType(AnimatedContainer),
      );
      final decoration = container.decoration as BoxDecoration;
      final border = decoration.border as Border;
      expect(border.top.color, AwakenColors.borderFocus);
    });

    testWidgets('exibe texto de erro e borda de erro quando errorText é definido',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AwakenTextField(
              label: 'E-mail',
              errorText: 'Campo obrigatório',
            ),
          ),
        ),
      );

      expect(find.text('Campo obrigatório'), findsOneWidget);

      final container = tester.widget<AnimatedContainer>(
        find.byType(AnimatedContainer),
      );
      final decoration = container.decoration as BoxDecoration;
      final border = decoration.border as Border;
      expect(border.top.color, AwakenColors.error);
    });
  });
}
```

- [ ] **Step 5.2: Rodar e confirmar falha**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_text_field_test.dart --reporter=expanded
```

Esperado: FAIL — o arquivo `awaken_text_field.dart` ainda não existe (erro de import).

- [ ] **Step 5.3: Criar `awaken_text_field.dart`**

```dart
// apps/mobile/lib/design_system/components/awaken_text_field.dart
import 'package:flutter/material.dart';
import '../tokens/colors.dart';
import '../tokens/spacing.dart';
import '../tokens/typography.dart';

/// Awaken text field — campo escuro com label, borda de foco e erro,
/// espelhando `docs/design-system/components/core/Input.jsx`.
class AwakenTextField extends StatefulWidget {
  const AwakenTextField({
    super.key,
    required this.label,
    this.controller,
    this.obscureText = false,
    this.keyboardType,
    this.textInputAction,
    this.errorText,
  });

  final String label;
  final TextEditingController? controller;
  final bool obscureText;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final String? errorText;

  @override
  State<AwakenTextField> createState() => _AwakenTextFieldState();
}

class _AwakenTextFieldState extends State<AwakenTextField> {
  final _focusNode = FocusNode();
  bool _focused = false;

  @override
  void initState() {
    super.initState();
    _focusNode.addListener(_handleFocusChange);
  }

  void _handleFocusChange() {
    setState(() => _focused = _focusNode.hasFocus);
  }

  @override
  void dispose() {
    _focusNode.removeListener(_handleFocusChange);
    _focusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final hasError = widget.errorText != null && widget.errorText!.isNotEmpty;
    final borderColor = hasError
        ? AwakenColors.error
        : _focused
            ? AwakenColors.borderFocus
            : AwakenColors.borderDefault;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          widget.label,
          style: AwakenTypography.bodySmall.copyWith(
            color: AwakenColors.textSecondary,
          ),
        ),
        const SizedBox(height: AwakenSpacing.xs),
        AnimatedContainer(
          duration: const Duration(milliseconds: 140),
          height: 48,
          padding: const EdgeInsets.symmetric(horizontal: 14),
          decoration: BoxDecoration(
            color: AwakenColors.inputBackground,
            borderRadius: BorderRadius.circular(AwakenSpacing.inputRadius + 4),
            border: Border.all(color: borderColor),
          ),
          child: Center(
            child: TextField(
              focusNode: _focusNode,
              controller: widget.controller,
              obscureText: widget.obscureText,
              keyboardType: widget.keyboardType,
              textInputAction: widget.textInputAction,
              style: AwakenTypography.bodyLarge,
              decoration: const InputDecoration(
                isCollapsed: true,
                border: InputBorder.none,
              ),
            ),
          ),
        ),
        if (hasError) ...[
          const SizedBox(height: AwakenSpacing.xs),
          Text(
            widget.errorText!,
            style: AwakenTypography.bodySmall.copyWith(
              color: AwakenColors.error,
            ),
          ),
        ],
      ],
    );
  }
}
```

- [ ] **Step 5.4: Rodar e confirmar sucesso**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_text_field_test.dart --reporter=expanded
```

Esperado: 3 testes PASS.

- [ ] **Step 5.5: Análise estática**

```bash
cd apps/mobile && flutter analyze lib/design_system/components/awaken_text_field.dart
```

Esperado: `No issues found!`

- [ ] **Step 5.6: Commit**

```bash
git add apps/mobile/lib/design_system/components/awaken_text_field.dart apps/mobile/test/design_system/components/awaken_text_field_test.dart
git commit -m "feat: adiciona AwakenTextField com borda de foco e erro"
```

---

## Task 6: Reescrever `LoginPage` com os novos componentes

**Files:**
- Modify: `apps/mobile/lib/features/auth/presentation/pages/login_page.dart`
- Create: `apps/mobile/test/features/auth/presentation/pages/login_page_test.dart`

- [ ] **Step 6.1: Escrever o teste**

```dart
// apps/mobile/test/features/auth/presentation/pages/login_page_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:awaken/l10n/app_localizations.dart';
import 'package:awaken/app/app_router.dart';
import 'package:awaken/features/auth/presentation/pages/login_page.dart';

GoRouter _buildRouter() => GoRouter(
      initialLocation: AppRoutes.login,
      routes: [
        GoRoute(path: AppRoutes.login, builder: (_, __) => const LoginPage()),
        GoRoute(
          path: AppRoutes.onboarding,
          builder: (_, __) => const Scaffold(body: Text('Onboarding')),
        ),
        GoRoute(
          path: AppRoutes.register,
          builder: (_, __) => const Scaffold(body: Text('Register')),
        ),
      ],
    );

Widget _buildTestApp() {
  return MaterialApp.router(
    routerConfig: _buildRouter(),
    localizationsDelegates: const [
      AppLocalizations.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    supportedLocales: const [Locale('pt', 'BR')],
    locale: const Locale('pt', 'BR'),
  );
}

void main() {
  group('LoginPage', () {
    testWidgets('exibe logo, campos de e-mail/senha e botão de entrar',
        (tester) async {
      await tester.pumpWidget(_buildTestApp());
      await tester.pump();

      expect(find.text('AWAKEN'), findsOneWidget);
      expect(find.text('E-mail'), findsOneWidget);
      expect(find.text('Senha'), findsOneWidget);
      expect(find.byType(TextField), findsNWidgets(2));
      // "Entrar" aparece no título E no botão de login (mesma string em pt-BR).
      expect(find.text('Entrar'), findsNWidgets(2));
    });

    testWidgets('navega para onboarding ao tocar em Entrar', (tester) async {
      await tester.pumpWidget(_buildTestApp());
      await tester.pump();

      await tester.tap(find.text('Entrar').last);
      await tester.pumpAndSettle();

      expect(find.text('Onboarding'), findsOneWidget);
    });
  });
}
```

- [ ] **Step 6.2: Rodar e confirmar falha**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/login_page_test.dart --reporter=expanded
```

Esperado: FAIL — a `LoginPage` atual usa `TextField` cru sem labels "E-mail"/"Senha" como `Text` visível (são `labelText` do `InputDecoration`, não encontrados por `find.text`).

- [ ] **Step 6.3: Reescrever `login_page.dart`**

```dart
// apps/mobile/lib/features/auth/presentation/pages/login_page.dart
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:awaken/l10n/app_localizations.dart';

import '../../../../app/app_router.dart';
import '../../../../design_system/components/awaken_button.dart';
import '../../../../design_system/components/awaken_logo.dart';
import '../../../../design_system/components/awaken_text_field.dart';
import '../../../../design_system/tokens/colors.dart';
import '../../../../design_system/tokens/spacing.dart';
import '../../../../design_system/tokens/typography.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      backgroundColor: AwakenColors.backgroundPrimary,
      body: Stack(
        fit: StackFit.expand,
        children: [
          const DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment(0, -0.45),
                radius: 1.1,
                colors: [
                  Color(0xFF101A32),
                  AwakenColors.backgroundSecondary,
                  AwakenColors.backgroundPrimary,
                ],
                stops: [0, 0.48, 1],
              ),
            ),
          ),
          SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(AwakenSpacing.lg),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 420),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const AwakenLogo(size: 200, reveal: 1, flame: 1, alive: 1),
                      const SizedBox(height: AwakenSpacing.xl),
                      Text(
                        l10n.loginTitle,
                        textAlign: TextAlign.center,
                        style: AwakenTypography.displayMedium,
                      ),
                      const SizedBox(height: AwakenSpacing.lg),
                      AwakenTextField(
                        label: l10n.loginEmailLabel,
                        controller: _emailController,
                        keyboardType: TextInputType.emailAddress,
                        textInputAction: TextInputAction.next,
                      ),
                      const SizedBox(height: AwakenSpacing.md),
                      AwakenTextField(
                        label: l10n.loginPasswordLabel,
                        controller: _passwordController,
                        obscureText: true,
                        textInputAction: TextInputAction.done,
                      ),
                      const SizedBox(height: AwakenSpacing.lg),
                      AwakenButton(
                        label: l10n.loginButton,
                        onPressed: () => context.go(AppRoutes.onboarding),
                      ),
                      const SizedBox(height: AwakenSpacing.sm),
                      AwakenButton(
                        label: l10n.loginWithGoogle,
                        variant: AwakenButtonVariant.secondary,
                        onPressed: () => context.go(AppRoutes.onboarding),
                      ),
                      const SizedBox(height: AwakenSpacing.md),
                      TextButton(
                        onPressed: () => context.go(AppRoutes.register),
                        child: Text(
                          '${l10n.loginNoAccount} '
                          '${l10n.loginRegisterLink}',
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
```

- [ ] **Step 6.4: Rodar e confirmar sucesso**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/login_page_test.dart --reporter=expanded
```

Esperado: 2 testes PASS.

- [ ] **Step 6.5: Análise estática**

```bash
cd apps/mobile && flutter analyze lib/features/auth/presentation/pages/login_page.dart
```

Esperado: `No issues found!`

- [ ] **Step 6.6: Commit**

```bash
git add apps/mobile/lib/features/auth/presentation/pages/login_page.dart apps/mobile/test/features/auth/presentation/pages/login_page_test.dart
git commit -m "feat: LoginPage usa AwakenTextField e AwakenButton com tokens do design system"
```

---

## Task 7: `SplashPage` — alimentar `flicker` no `AwakenLogo`

**Files:**
- Modify: `apps/mobile/lib/features/splash/presentation/pages/splash_page.dart`

- [ ] **Step 7.1: Adicionar campo `flicker` em `_SplashContent`**

Em `apps/mobile/lib/features/splash/presentation/pages/splash_page.dart`, localizar a classe `_SplashContent` e trocar:

```dart
class _SplashContent extends StatelessWidget {
  const _SplashContent({
    required this.reveal,
    required this.flame,
    required this.alive,
  });

  final double reveal;
  final double flame;
  final double alive;
```

por:

```dart
class _SplashContent extends StatelessWidget {
  const _SplashContent({
    required this.reveal,
    required this.flame,
    required this.alive,
    required this.flicker,
  });

  final double reveal;
  final double flame;
  final double alive;
  final double flicker;
```

- [ ] **Step 7.2: Passar `flicker` para o `AwakenLogo` dentro de `_SplashContent.build`**

Trocar:

```dart
              AwakenLogo(
                size: logoSize,
                reveal: reveal,
                flame: flame,
                alive: alive,
              ),
```

por:

```dart
              AwakenLogo(
                size: logoSize,
                reveal: reveal,
                flame: flame,
                alive: alive,
                flicker: flicker,
              ),
```

- [ ] **Step 7.3: Atualizar `_StaticSplash` (fallback sem animação, `flicker: 0`)**

Trocar:

```dart
class _StaticSplash extends StatelessWidget {
  const _StaticSplash();

  @override
  Widget build(BuildContext context) {
    return const _SplashContent(reveal: 1, flame: 1, alive: 1);
  }
}
```

por:

```dart
class _StaticSplash extends StatelessWidget {
  const _StaticSplash();

  @override
  Widget build(BuildContext context) {
    return const _SplashContent(reveal: 1, flame: 1, alive: 1, flicker: 0);
  }
}
```

- [ ] **Step 7.4: Alimentar `flicker` com o valor cru do controller em `_AnimatedSplash.build`**

Localizar, dentro de `_AnimatedSplash`, o `AnimatedBuilder` que monta `_SplashContent`:

```dart
            child: _SplashContent(
              reveal: reveal,
              flame: flame,
              alive: alive,
            ),
```

trocar por:

```dart
            child: _SplashContent(
              reveal: reveal,
              flame: flame,
              alive: alive,
              flicker: value,
            ),
```

(`value` já existe nesse escopo — é `controller.value`, 0..1 ao longo dos 3200ms inteiros, usado pelas funções `sin()` dos painters de chama/partículas para oscilar continuamente.)

- [ ] **Step 7.5: Rodar a suíte de testes da splash (não deve quebrar)**

```bash
cd apps/mobile && flutter test test/features/splash --reporter=expanded
```

Esperado: todos os testes PASS (nenhuma expectativa de texto/rota muda).

- [ ] **Step 7.6: Análise estática**

```bash
cd apps/mobile && flutter analyze lib/features/splash/presentation/pages/splash_page.dart
```

Esperado: `No issues found!`

- [ ] **Step 7.7: Commit**

```bash
git add apps/mobile/lib/features/splash/presentation/pages/splash_page.dart
git commit -m "feat: splash alimenta flicker contínuo no AwakenLogo durante a fase da chama"
```

---

## Task 8: Suíte completa + verificação final

**Files:** nenhum (apenas verificação)

- [ ] **Step 8.1: Rodar toda a suíte de testes**

```bash
cd apps/mobile && flutter test --reporter=expanded
```

Esperado: todos os testes PASS (incluindo os já existentes de `splash_controller_test.dart`, `splash_page_test.dart`, e os novos desta plan).

- [ ] **Step 8.2: Análise estática completa**

```bash
cd apps/mobile && flutter analyze
```

Esperado: `No issues found!`

- [ ] **Step 8.3: Gerar l10n (garantir que nada ficou inconsistente)**

```bash
cd apps/mobile && flutter gen-l10n
```

Esperado: sem erros.

- [ ] **Step 8.4: Commit final (se houver qualquer ajuste pendente de lint)**

```bash
git add -A
git commit -m "chore: ajustes finais de lint da splash/login"
```

(Só rodar este passo se `flutter analyze` ou `flutter gen-l10n` exigirem alguma correção; caso contrário, pular.)

---

## Self-review contra a spec

| Requisito da spec (`2026-06-17-splash-login-fidelity-design.md`) | Task que cobre |
|---|---|
| Copiar `logo-mark.png` real para `apps/mobile/assets/images/` | Task 1 |
| `AwakenLogo` híbrido (imagem real + overlay) | Task 3 |
| Coordenadas de íris calibradas por análise de pixel | Task 3 (`_IrisIgnitionPainter`) |
| Chama "viva" via flicker/partículas sem redesenhar a arte | Task 3 (`_FlameLifePainter`, `_EmberParticlesPainter`) |
| Wordmark continua `Text` real (a11y/l10n/testes) | Task 3 (`_Wordmark`) |
| `flicker` opcional, default 0 (login fica vivo e estático) | Task 3 (parâmetro), Task 6 (uso sem `flicker`) |
| Timeline 0–1.2s / 1.2–2.8s / 2.8–3.2s preservada | Task 7 (reaproveita `_phase1`/`_phase2`/`_phase3` já existentes, só adiciona `flicker: value`) |
| Tokens novos de cor (`borderDefault`, `borderFocus`, `inputBackground`, `glowBlue`) | Task 2 |
| `AwakenButton` com gradiente `--grad-energy` | Task 4 |
| `AwakenTextField` espelhando `Input.jsx` | Task 5 |
| `LoginPage` redesenhada com os novos componentes | Task 6 |
| Testes não regridem (`splash_page_test.dart` inalterado) | Task 7, Step 7.5 |
