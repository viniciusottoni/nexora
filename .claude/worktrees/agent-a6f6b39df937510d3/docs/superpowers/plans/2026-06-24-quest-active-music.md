# Música de Quest Ativa — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tocar a trilha `Awaken Protocol.mp3` enquanto a quest diária está em `in_progress` em `QuestExecutionPage`, voltando à trilha padrão (`Awaken Hunter.mp3`) em qualquer outro status ou ao sair da tela.

**Architecture:** Reaproveita o `AudioPlayer` único de `BackgroundMusicService`, adicionando um método `playQuestTheme()` que troca o `AssetSource` no mesmo player (sem segundo player, sem disputa de foco de áudio). `QuestExecutionPage` observa `questExecutionControllerProvider` via `ref.listen` e dispara `playQuestTheme()`/`playTheme()` conforme `execution.status == 'in_progress'` ou não; `dispose()` força volta à trilha padrão como rede de segurança.

**Tech Stack:** Flutter, Riverpod (`Notifier`/`ref.listen`), `audioplayers` (`AudioPlayer`/`AssetSource`/`ReleaseMode.loop`), `mocktail` para testes.

Spec de referência: `docs/superpowers/specs/2026-06-24-quest-active-music-design.md`

---

### Task 1: Copiar o asset de áudio da quest

**Files:**
- Create: `apps/mobile/assets/audio/awaken_protocol.mp3`

- [ ] **Step 1: Copiar o arquivo de origem para a pasta de assets do Flutter**

Run:
```bash
cp "docs/design-system/assets/Awaken Protocol.mp3" "apps/mobile/assets/audio/awaken_protocol.mp3"
```

Expected: novo arquivo `apps/mobile/assets/audio/awaken_protocol.mp3` existe. `apps/mobile/pubspec.yaml` já declara `assets/audio/` (linha 104), nenhuma mudança de config necessária.

- [ ] **Step 2: Confirmar que o arquivo foi copiado corretamente**

Run: `ls -la "apps/mobile/assets/audio/"`
Expected: lista mostra `awaken_hunter.mp3` (já existente) e `awaken_protocol.mp3` (novo) com tamanho > 0 bytes.

- [ ] **Step 3: Commit**

```bash
git add "apps/mobile/assets/audio/awaken_protocol.mp3"
git commit -m "feat: adiciona asset de musica da quest ativa"
```

---

### Task 2: Adicionar `playQuestTheme()` ao `BackgroundMusicService`

**Files:**
- Modify: `apps/mobile/lib/core/audio/background_music_service.dart:11,35-43`

- [ ] **Step 1: Extrair a lógica de `playTheme()` para um helper privado e adicionar `playQuestTheme()`**

Substituir o trecho atual:

```dart
  static const double defaultVolume = 0.5;
  static const _themeAsset = 'audio/awaken_hunter.mp3';
```

por:

```dart
  static const double defaultVolume = 0.5;
  static const _themeAsset = 'audio/awaken_hunter.mp3';
  static const _questAsset = 'audio/awaken_protocol.mp3';
```

E substituir o método atual:

```dart
  Future<void> playTheme() async {
    // Set the context on the specific player too. Some Android builds are
    // picky about the global context being applied before the first play().
    await _player.setAudioContext(_audioContext);
    await _player.setReleaseMode(ReleaseMode.loop);
    await _player.setVolume(await _persistedVolume());
    await _player.setSource(AssetSource(_themeAsset));
    await _player.resume();
  }
```

por:

```dart
  Future<void> playTheme() => _playLoop(_themeAsset);

  /// Toca a trilha de quest ativa enquanto a execução está em andamento.
  /// Usa o mesmo player que [playTheme] — chamar [playTheme] depois troca a
  /// trilha de volta para a padrão.
  Future<void> playQuestTheme() => _playLoop(_questAsset);

  Future<void> _playLoop(String asset) async {
    // Set the context on the specific player too. Some Android builds are
    // picky about the global context being applied before the first play().
    await _player.setAudioContext(_audioContext);
    await _player.setReleaseMode(ReleaseMode.loop);
    await _player.setVolume(await _persistedVolume());
    await _player.setSource(AssetSource(asset));
    await _player.resume();
  }
```

- [ ] **Step 2: Verificar que o projeto compila**

Run: `cd apps/mobile && flutter analyze lib/core/audio/background_music_service.dart`
Expected: `No issues found!`

- [ ] **Step 3: Commit**

```bash
git add apps/mobile/lib/core/audio/background_music_service.dart
git commit -m "feat: adiciona playQuestTheme ao BackgroundMusicService"
```

---

### Task 3: Disparar a troca de trilha em `QuestExecutionPage`

**Files:**
- Modify: `apps/mobile/lib/features/quests/presentation/pages/quest_execution_page.dart:1-56,147-151`

- [ ] **Step 1: Importar o provider do serviço de música**

No topo do arquivo, junto aos outros imports `package:awaken/...`, adicionar:

```dart
import 'package:awaken/core/audio/background_music_provider.dart';
```

- [ ] **Step 2: Adicionar o estado local de controle da trilha**

Substituir:

```dart
class _QuestExecutionPageState extends ConsumerState<QuestExecutionPage> {
  Timer? _clockTimer;
  DateTime _now = DateTime.now().toUtc();
```

por:

```dart
class _QuestExecutionPageState extends ConsumerState<QuestExecutionPage> {
  Timer? _clockTimer;
  DateTime _now = DateTime.now().toUtc();
  bool _questMusicActive = false;
```

- [ ] **Step 3: Reverter para a trilha padrão ao descartar a tela**

Substituir:

```dart
  @override
  void dispose() {
    _clockTimer?.cancel();
    super.dispose();
  }
```

por:

```dart
  @override
  void dispose() {
    _clockTimer?.cancel();
    if (_questMusicActive) {
      _questMusicActive = false;
      ref.read(backgroundMusicServiceProvider).playTheme().catchError((_) {});
    }
    super.dispose();
  }
```

- [ ] **Step 4: Observar o status da execução e trocar a trilha**

Substituir o início de `build()`:

```dart
  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = ref.watch(questExecutionControllerProvider);
```

por:

```dart
  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    ref.listen<QuestExecutionState>(questExecutionControllerProvider,
        (previous, next) {
      final isActive =
          next is QuestExecutionLoaded && next.execution.status == 'in_progress';
      if (isActive && !_questMusicActive) {
        _questMusicActive = true;
        ref.read(backgroundMusicServiceProvider).playQuestTheme().catchError((_) {});
      } else if (!isActive && _questMusicActive) {
        _questMusicActive = false;
        ref.read(backgroundMusicServiceProvider).playTheme().catchError((_) {});
      }
    });
    final state = ref.watch(questExecutionControllerProvider);
```

- [ ] **Step 5: Verificar que o projeto compila**

Run: `cd apps/mobile && flutter analyze lib/features/quests/presentation/pages/quest_execution_page.dart`
Expected: `No issues found!`

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/lib/features/quests/presentation/pages/quest_execution_page.dart
git commit -m "feat: troca trilha de musica conforme status da quest ativa"
```

---

### Task 4: Testes de widget cobrindo as transições de trilha

**Files:**
- Create: `apps/mobile/test/features/quests/presentation/pages/quest_execution_page_test.dart`

- [ ] **Step 1: Escrever o arquivo de teste completo**

```dart
import 'package:awaken/app/app_router.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/no_op_analytics_service.dart';
import 'package:awaken/core/audio/background_music_provider.dart';
import 'package:awaken/core/audio/background_music_service.dart';
import 'package:awaken/features/quests/domain/entities/complete_exercise_result.dart';
import 'package:awaken/features/quests/domain/entities/daily_quest.dart';
import 'package:awaken/features/quests/domain/entities/quest_attribute_breakdown.dart';
import 'package:awaken/features/quests/domain/entities/quest_cancel_result.dart';
import 'package:awaken/features/quests/domain/entities/quest_execution.dart';
import 'package:awaken/features/quests/domain/entities/quest_pause_result.dart';
import 'package:awaken/features/quests/domain/entities/quest_preview.dart';
import 'package:awaken/features/quests/domain/entities/quest_resume_result.dart';
import 'package:awaken/features/quests/domain/entities/started_quest.dart';
import 'package:awaken/features/quests/domain/repositories/quests_repository.dart';
import 'package:awaken/features/quests/presentation/pages/quest_execution_page.dart';
import 'package:awaken/features/quests/presentation/providers/quests_providers.dart';
import 'package:awaken/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';

class MockBackgroundMusicService extends Mock implements BackgroundMusicService {}

class _FakeQuestsRepository implements QuestsRepository {
  QuestExecution execution;

  _FakeQuestsRepository(this.execution);

  @override
  Future<DailyQuest> confirmDailyQuest(String questId) =>
      throw UnimplementedError();

  @override
  Future<DailyQuest> generateDailyQuest() => throw UnimplementedError();

  @override
  Future<DailyQuest?> getTodayQuest() => throw UnimplementedError();

  @override
  Future<QuestPreview> getQuestPreview(String questId) =>
      throw UnimplementedError();

  @override
  Future<QuestPreview> changeTrainingType(
    String questId,
    String trainingType, {
    String? programId,
  }) =>
      throw UnimplementedError();

  @override
  Future<void> saveWorkoutPreference(String trainingType,
          {String? programId}) =>
      throw UnimplementedError();

  @override
  Future<DailyQuest> regenerateDailyQuest({bool useReforgeScroll = false}) =>
      throw UnimplementedError();

  @override
  Future<StartedQuest> startQuest(String questId) =>
      throw UnimplementedError();

  @override
  Future<QuestExecution> getQuestExecution(String questId) async => execution;

  @override
  Future<QuestPauseResult> pauseQuest(String questId) async {
    execution = _withStatus('paused');
    return QuestPauseResult(
      questId: questId,
      questType: execution.questType,
      status: 'paused',
      pausedAt: DateTime.utc(2026, 6, 24, 12, 0, 0),
    );
  }

  @override
  Future<QuestResumeResult> resumeQuest(String questId) async {
    execution = _withStatus('in_progress');
    return QuestResumeResult(
      questId: questId,
      questType: execution.questType,
      status: 'in_progress',
      resumedAt: DateTime.utc(2026, 6, 24, 12, 1, 0),
    );
  }

  @override
  Future<QuestCancelResult> cancelQuest(String questId) async {
    execution = _withStatus('cancelled');
    return QuestCancelResult(
      questId: questId,
      questType: execution.questType,
      status: 'cancelled',
      cancelledAt: DateTime.utc(2026, 6, 24, 12, 2, 0),
    );
  }

  @override
  Future<CompleteExerciseResult> completeExercise(
    String questId,
    String questExerciseId,
  ) =>
      throw UnimplementedError();

  QuestExecution _withStatus(String status) => QuestExecution(
        questId: execution.questId,
        questType: execution.questType,
        status: status,
        startedAt: execution.startedAt,
        attributeXpPreview: execution.attributeXpPreview,
        exercises: execution.exercises,
      );
}

QuestExecution _buildExecution({required String status}) => QuestExecution(
      questId: 'qst_001',
      questType: 'daily',
      status: status,
      startedAt: DateTime.utc(2026, 6, 24, 11, 0, 0),
      attributeXpPreview: const QuestAttributeBreakdown(
        strength: 6,
        agility: 2,
        endurance: 0,
        vitality: 0,
        focus: 0,
        wisdom: 1,
      ),
      exercises: const [
        QuestExecutionExercise(
          questExerciseId: 'qst_ex_001',
          order: 1,
          status: 'pending',
          name: 'Squat',
          sets: 3,
          repsMin: 10,
          repsMax: 15,
          restSeconds: 90,
          targetRpe: '6-8',
          videoUrl: null,
          xpReward: 12,
          effectiveDifficulty: 3,
          attributeImpacts: QuestAttributeBreakdown(
            strength: 3,
            agility: 1,
            endurance: 0,
            vitality: 0,
            focus: 0,
            wisdom: 0,
          ),
          hiddenWisdomXp: 1,
          completedAtUtc: null,
        ),
      ],
    );

Widget _buildApp({
  required QuestsRepository repository,
  required BackgroundMusicService musicService,
}) {
  final router = GoRouter(
    initialLocation: '/quest-execution/qst_001',
    routes: [
      GoRoute(
        path: AppRoutes.questExecution,
        builder: (_, state) =>
            QuestExecutionPage(questId: state.pathParameters['questId']!),
      ),
      GoRoute(
        path: AppRoutes.home,
        builder: (_, __) => const Scaffold(key: Key('home-page-stub')),
      ),
    ],
  );

  return ProviderScope(
    overrides: [
      analyticsServiceProvider.overrideWithValue(const NoOpAnalyticsService()),
      questsRepositoryProvider.overrideWithValue(repository),
      backgroundMusicServiceProvider.overrideWithValue(musicService),
    ],
    child: MaterialApp.router(
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      supportedLocales: AppLocalizations.supportedLocales,
      locale: const Locale('en'),
      routerConfig: router,
    ),
  );
}

Future<void> _pumpUntil(WidgetTester tester, Finder finder) async {
  await tester.pump();
  for (var i = 0; i < 20; i++) {
    if (finder.evaluate().isNotEmpty) return;
    await tester.pump(const Duration(milliseconds: 50));
  }
}

void main() {
  late MockBackgroundMusicService musicService;

  setUp(() {
    musicService = MockBackgroundMusicService();
    when(() => musicService.playTheme()).thenAnswer((_) async {});
    when(() => musicService.playQuestTheme()).thenAnswer((_) async {});
  });

  testWidgets('toca a trilha de quest quando a execucao esta in_progress',
      (tester) async {
    await tester.pumpWidget(_buildApp(
      repository: _FakeQuestsRepository(_buildExecution(status: 'in_progress')),
      musicService: musicService,
    ));

    await _pumpUntil(tester, find.byTooltip('Pause quest'));

    verify(() => musicService.playQuestTheme()).called(1);
    verifyNever(() => musicService.playTheme());
  });

  testWidgets('volta para a trilha padrao ao pausar a quest', (tester) async {
    await tester.pumpWidget(_buildApp(
      repository: _FakeQuestsRepository(_buildExecution(status: 'in_progress')),
      musicService: musicService,
    ));
    await _pumpUntil(tester, find.byTooltip('Pause quest'));

    await tester.tap(find.byTooltip('Pause quest'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Yes, pause'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('home-page-stub')), findsOneWidget);
    verify(() => musicService.playQuestTheme()).called(1);
    verify(() => musicService.playTheme()).called(1);
  });

  testWidgets('volta a tocar a trilha de quest ao retomar', (tester) async {
    await tester.pumpWidget(_buildApp(
      repository: _FakeQuestsRepository(_buildExecution(status: 'paused')),
      musicService: musicService,
    ));
    await _pumpUntil(tester, find.text('Resume quest'));

    verifyNever(() => musicService.playQuestTheme());
    verifyNever(() => musicService.playTheme());

    await tester.tap(find.text('Resume quest'));
    await tester.pumpAndSettle();

    verify(() => musicService.playQuestTheme()).called(1);
    verifyNever(() => musicService.playTheme());
  });

  testWidgets('volta para a trilha padrao ao cancelar a quest', (tester) async {
    await tester.pumpWidget(_buildApp(
      repository: _FakeQuestsRepository(_buildExecution(status: 'in_progress')),
      musicService: musicService,
    ));
    await _pumpUntil(tester, find.byTooltip('Cancel quest'));

    await tester.tap(find.byTooltip('Cancel quest'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Yes, cancel'));
    await tester.pumpAndSettle();

    verify(() => musicService.playQuestTheme()).called(1);
    verify(() => musicService.playTheme()).called(1);
  });

  testWidgets(
      'volta para a trilha padrao ao descartar a tela ainda in_progress',
      (tester) async {
    await tester.pumpWidget(_buildApp(
      repository: _FakeQuestsRepository(_buildExecution(status: 'in_progress')),
      musicService: musicService,
    ));
    await _pumpUntil(tester, find.byTooltip('Pause quest'));
    verify(() => musicService.playQuestTheme()).called(1);

    await tester.pumpWidget(const SizedBox());

    verify(() => musicService.playTheme()).called(1);
  });
}
```

- [ ] **Step 2: Rodar os testes**

Run: `cd apps/mobile && flutter test test/features/quests/presentation/pages/quest_execution_page_test.dart`
Expected: todos os 5 testes passam (`+5: All tests passed!`).

- [ ] **Step 3: Rodar `flutter analyze` no projeto inteiro**

Run: `cd apps/mobile && flutter analyze`
Expected: `No issues found!`

- [ ] **Step 4: Commit**

```bash
git add apps/mobile/test/features/quests/presentation/pages/quest_execution_page_test.dart
git commit -m "test: cobre transicoes de trilha de musica na quest ativa"
```

---

## Resumo de cobertura da spec

| Requisito da spec | Task |
|---|---|
| Asset `awaken_protocol.mp3` em `assets/audio/` | Task 1 |
| `BackgroundMusicService.playQuestTheme()` | Task 2 |
| Trilha de quest ativa enquanto `in_progress` | Task 3, Step 4 |
| Volta à trilha padrão em pause/cancel/completed/erro | Task 3, Step 4 |
| Volta à trilha padrão ao sair da tela ainda `in_progress` (dispose) | Task 3, Step 3 |
| `exercise_execution_page` cobertura via mesma instância de `quest_execution_page` | Coberto pela arquitetura (Task 3) — sem código adicional, pois a página permanece montada sob o `Navigator.push` |
| Testes de transição de status | Task 4 |
