# Notification Inbox + Streak Tap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fazer o sino de notificações abrir uma tela de inbox real (repository pattern, sem mocks — retorna lista vazia até o endpoint backend existir) e o ícone de streak navegar para a tela de Progressão, com badge dinâmico e empty state visual.

**Architecture:** Repository pattern completo (entity → interface → data source stub → repository impl → provider → page). O `NotificationInboxRemoteDataSource` retorna `[]` até o backend implementar `GET /api/notifications/inbox`; quando o endpoint existir, basta substituir o stub. Badge do sino some quando 0 não lidas. Streak tap → `AppRoutes.progression`.

**Tech Stack:** Flutter, Riverpod (`Notifier` + sealed state), go_router, Dio, ARB i18n (pt/en/es/fr)

---

## File Map

| Ação | Arquivo |
|------|---------|
| Create | `apps/mobile/lib/features/notifications/domain/entities/inbox_notification.dart` |
| Create | `apps/mobile/lib/features/notifications/domain/repositories/notification_inbox_repository.dart` |
| Create | `apps/mobile/lib/features/notifications/data/dtos/inbox_notification_dto.dart` |
| Create | `apps/mobile/lib/features/notifications/data/datasources/notification_inbox_remote_data_source.dart` |
| Create | `apps/mobile/lib/features/notifications/data/repositories/notification_inbox_repository_impl.dart` |
| Create | `apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_state.dart` |
| Create | `apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_controller.dart` |
| Create | `apps/mobile/lib/features/notifications/presentation/pages/notification_inbox_page.dart` |
| Modify | `apps/mobile/lib/app/app_router.dart` |
| Modify | `apps/mobile/lib/features/home/presentation/pages/home_page.dart` |
| Modify | `apps/mobile/lib/l10n/app_pt.arb` |
| Modify | `apps/mobile/lib/l10n/app_en.arb` |
| Modify | `apps/mobile/lib/l10n/app_es.arb` |
| Modify | `apps/mobile/lib/l10n/app_fr.arb` |
| Test   | `apps/mobile/test/features/notifications/notification_inbox_controller_test.dart` |

---

### Task 1: Entidade `InboxNotification` + interface do repositório

**Files:**
- Create: `apps/mobile/lib/features/notifications/domain/entities/inbox_notification.dart`
- Create: `apps/mobile/lib/features/notifications/domain/repositories/notification_inbox_repository.dart`

- [ ] **Step 1: Criar a entidade**

```dart
// apps/mobile/lib/features/notifications/domain/entities/inbox_notification.dart

enum InboxNotificationType { streakAlert, questReminder, reactivation, system }

class InboxNotification {
  const InboxNotification({
    required this.id,
    required this.title,
    required this.body,
    required this.receivedAt,
    required this.type,
    this.isRead = false,
  });

  final String id;
  final String title;
  final String body;
  final DateTime receivedAt;
  final InboxNotificationType type;
  final bool isRead;

  InboxNotification copyWith({bool? isRead}) => InboxNotification(
        id: id,
        title: title,
        body: body,
        receivedAt: receivedAt,
        type: type,
        isRead: isRead ?? this.isRead,
      );
}
```

- [ ] **Step 2: Criar a interface do repositório**

```dart
// apps/mobile/lib/features/notifications/domain/repositories/notification_inbox_repository.dart

import '../entities/inbox_notification.dart';

abstract interface class NotificationInboxRepository {
  /// GET /api/notifications/inbox
  Future<List<InboxNotification>> getInbox();
}
```

- [ ] **Step 3: Commit**

```bash
git add apps/mobile/lib/features/notifications/domain/entities/inbox_notification.dart
git add apps/mobile/lib/features/notifications/domain/repositories/notification_inbox_repository.dart
git commit -m "feat: add InboxNotification entity and repository interface"
```

---

### Task 2: Camada de dados (DTO + data source stub + repository impl)

**Files:**
- Create: `apps/mobile/lib/features/notifications/data/dtos/inbox_notification_dto.dart`
- Create: `apps/mobile/lib/features/notifications/data/datasources/notification_inbox_remote_data_source.dart`
- Create: `apps/mobile/lib/features/notifications/data/repositories/notification_inbox_repository_impl.dart`

- [ ] **Step 1: Criar o DTO**

```dart
// apps/mobile/lib/features/notifications/data/dtos/inbox_notification_dto.dart

import '../../domain/entities/inbox_notification.dart';

class InboxNotificationDto {
  const InboxNotificationDto({
    required this.id,
    required this.title,
    required this.body,
    required this.receivedAt,
    required this.type,
    required this.isRead,
  });

  final String id;
  final String title;
  final String body;
  final DateTime receivedAt;
  final String type;
  final bool isRead;

  factory InboxNotificationDto.fromJson(Map<String, dynamic> json) =>
      InboxNotificationDto(
        id: json['id'] as String,
        title: json['title'] as String,
        body: json['body'] as String,
        receivedAt: DateTime.parse(json['receivedAt'] as String),
        type: json['type'] as String,
        isRead: json['isRead'] as bool,
      );

  InboxNotification toDomain() => InboxNotification(
        id: id,
        title: title,
        body: body,
        receivedAt: receivedAt,
        type: _mapType(type),
        isRead: isRead,
      );

  static InboxNotificationType _mapType(String raw) => switch (raw) {
        'streak_alert' => InboxNotificationType.streakAlert,
        'quest_reminder' => InboxNotificationType.questReminder,
        'reactivation' => InboxNotificationType.reactivation,
        _ => InboxNotificationType.system,
      };
}
```

- [ ] **Step 2: Criar o data source (stub — retorna [] até o endpoint existir)**

```dart
// apps/mobile/lib/features/notifications/data/datasources/notification_inbox_remote_data_source.dart

import 'package:dio/dio.dart';
import '../../../../core/errors/app_error.dart';
import '../dtos/inbox_notification_dto.dart';

class NotificationInboxRemoteDataSource {
  const NotificationInboxRemoteDataSource(this._dio);

  final Dio _dio;

  /// GET /api/notifications/inbox
  ///
  /// Retorna lista vazia enquanto o endpoint não estiver implementado no
  /// backend. Quando o endpoint existir, remover o catch de [404] e a
  /// linha `return [];` abaixo dele.
  Future<List<InboxNotificationDto>> getInbox() async {
    try {
      final response = await _dio.get<List<dynamic>>(
        '/api/notifications/inbox',
      );
      final data = response.data ?? [];
      return data
          .cast<Map<String, dynamic>>()
          .map(InboxNotificationDto.fromJson)
          .toList();
    } on DioException catch (e) {
      // Endpoint ainda não existe no backend — retorna lista vazia.
      // Remover quando GET /api/notifications/inbox for implementado.
      if (e.response?.statusCode == 404) return [];
      throw _mapError(e);
    }
  }

  AppError _mapError(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.connectionError:
        return const NetworkError();
      default:
        return const UnexpectedError();
    }
  }
}
```

- [ ] **Step 3: Criar o repository impl**

```dart
// apps/mobile/lib/features/notifications/data/repositories/notification_inbox_repository_impl.dart

import '../../domain/entities/inbox_notification.dart';
import '../../domain/repositories/notification_inbox_repository.dart';
import '../datasources/notification_inbox_remote_data_source.dart';

class NotificationInboxRepositoryImpl implements NotificationInboxRepository {
  const NotificationInboxRepositoryImpl(this._remoteDataSource);

  final NotificationInboxRemoteDataSource _remoteDataSource;

  @override
  Future<List<InboxNotification>> getInbox() async {
    final dtos = await _remoteDataSource.getInbox();
    return dtos.map((dto) => dto.toDomain()).toList();
  }
}
```

- [ ] **Step 4: Verificar compilação**

```bash
cd apps/mobile && flutter analyze lib/features/notifications/data/
```

Esperado: sem erros.

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/lib/features/notifications/data/dtos/inbox_notification_dto.dart
git add apps/mobile/lib/features/notifications/data/datasources/notification_inbox_remote_data_source.dart
git add apps/mobile/lib/features/notifications/data/repositories/notification_inbox_repository_impl.dart
git commit -m "feat: add notification inbox data layer (stub returns empty until backend ready)"
```

---

### Task 3: State + Controller (Riverpod)

**Files:**
- Create: `apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_state.dart`
- Create: `apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_controller.dart`
- Test: `apps/mobile/test/features/notifications/notification_inbox_controller_test.dart`

- [ ] **Step 1: Criar os states**

```dart
// apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_state.dart

import '../../domain/entities/inbox_notification.dart';

sealed class NotificationInboxState {
  const NotificationInboxState();
}

class NotificationInboxInitial extends NotificationInboxState {
  const NotificationInboxInitial();
}

class NotificationInboxLoading extends NotificationInboxState {
  const NotificationInboxLoading();
}

class NotificationInboxLoaded extends NotificationInboxState {
  const NotificationInboxLoaded(this.notifications);

  final List<InboxNotification> notifications;

  int get unreadCount => notifications.where((n) => !n.isRead).length;

  NotificationInboxLoaded withAllRead() => NotificationInboxLoaded(
        notifications.map((n) => n.copyWith(isRead: true)).toList(),
      );
}

class NotificationInboxError extends NotificationInboxState {
  const NotificationInboxError();
}
```

- [ ] **Step 2: Escrever testes antes do controller**

Criar `apps/mobile/test/features/notifications/notification_inbox_controller_test.dart`:

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:awaken/features/notifications/domain/entities/inbox_notification.dart';
import 'package:awaken/features/notifications/domain/repositories/notification_inbox_repository.dart';
import 'package:awaken/features/notifications/presentation/providers/notification_inbox_controller.dart';
import 'package:awaken/features/notifications/presentation/providers/notification_inbox_state.dart';

class _FakeRepo implements NotificationInboxRepository {
  _FakeRepo(this._items);
  final List<InboxNotification> _items;

  @override
  Future<List<InboxNotification>> getInbox() async => _items;
}

class _ThrowingRepo implements NotificationInboxRepository {
  @override
  Future<List<InboxNotification>> getInbox() async =>
      throw Exception('network error');
}

ProviderContainer _container(NotificationInboxRepository repo) =>
    ProviderContainer(
      overrides: [
        notificationInboxRepositoryProvider.overrideWithValue(repo),
      ],
    );

void main() {
  group('NotificationInboxController', () {
    test('load() com lista vazia → NotificationInboxLoaded([])', () async {
      final container = _container(_FakeRepo([]));
      addTearDown(container.dispose);
      await container.read(notificationInboxControllerProvider.notifier).load();
      final state = container.read(notificationInboxControllerProvider);
      expect(state, isA<NotificationInboxLoaded>());
      expect((state as NotificationInboxLoaded).notifications, isEmpty);
    });

    test('load() com itens → NotificationInboxLoaded com itens', () async {
      final items = [
        const InboxNotification(
          id: '1',
          title: 'Test',
          body: 'Body',
          receivedAt: Duration.zero,
          type: InboxNotificationType.system,
        ),
      ];
      final container = _container(_FakeRepo(items));
      addTearDown(container.dispose);
      await container.read(notificationInboxControllerProvider.notifier).load();
      final state = container.read(notificationInboxControllerProvider);
      expect(state, isA<NotificationInboxLoaded>());
      expect((state as NotificationInboxLoaded).notifications.length, 1);
    });

    test('load() com erro → NotificationInboxError', () async {
      final container = _container(_ThrowingRepo());
      addTearDown(container.dispose);
      await container.read(notificationInboxControllerProvider.notifier).load();
      expect(
        container.read(notificationInboxControllerProvider),
        isA<NotificationInboxError>(),
      );
    });

    test('markAllRead() atualiza estado → unreadCount == 0', () async {
      final items = [
        const InboxNotification(
          id: '1',
          title: 'Test',
          body: 'Body',
          receivedAt: Duration.zero,
          type: InboxNotificationType.streakAlert,
          isRead: false,
        ),
      ];
      final container = _container(_FakeRepo(items));
      addTearDown(container.dispose);
      final notifier =
          container.read(notificationInboxControllerProvider.notifier);
      await notifier.load();
      notifier.markAllRead();
      final state = container.read(notificationInboxControllerProvider);
      expect((state as NotificationInboxLoaded).unreadCount, 0);
    });
  });
}
```

**Nota:** `InboxNotification.receivedAt` é `DateTime` — ajustar o teste:

```dart
receivedAt: DateTime(2026, 1, 1),
```

Substituir todas as ocorrências de `receivedAt: Duration.zero` por `receivedAt: DateTime(2026, 1, 1)` no arquivo de teste acima.

- [ ] **Step 3: Rodar testes (devem falhar — controller não existe)**

```bash
cd apps/mobile && flutter test test/features/notifications/notification_inbox_controller_test.dart
```

Esperado: erro de import.

- [ ] **Step 4: Criar o controller**

```dart
// apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_controller.dart

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/network/dio_client.dart';
import '../../data/datasources/notification_inbox_remote_data_source.dart';
import '../../data/repositories/notification_inbox_repository_impl.dart';
import '../../domain/repositories/notification_inbox_repository.dart';
import 'notification_inbox_state.dart';

// ──────────────────────────────────────────────────────────
// Providers de infraestrutura
// ──────────────────────────────────────────────────────────

final notificationInboxRemoteDataSourceProvider =
    Provider<NotificationInboxRemoteDataSource>((ref) {
  return NotificationInboxRemoteDataSource(
    ref.watch(authenticatedDioProvider),
  );
});

final notificationInboxRepositoryProvider =
    Provider<NotificationInboxRepository>((ref) {
  return NotificationInboxRepositoryImpl(
    ref.watch(notificationInboxRemoteDataSourceProvider),
  );
});

// ──────────────────────────────────────────────────────────
// Controller
// ──────────────────────────────────────────────────────────

class NotificationInboxController extends Notifier<NotificationInboxState> {
  @override
  NotificationInboxState build() => const NotificationInboxInitial();

  Future<void> load() async {
    state = const NotificationInboxLoading();
    try {
      final notifications =
          await ref.read(notificationInboxRepositoryProvider).getInbox();
      state = NotificationInboxLoaded(notifications);
    } catch (_) {
      state = const NotificationInboxError();
    }
  }

  void markAllRead() {
    final current = state;
    if (current is! NotificationInboxLoaded) return;
    state = current.withAllRead();
  }
}

final notificationInboxControllerProvider = NotifierProvider<
    NotificationInboxController, NotificationInboxState>(
  NotificationInboxController.new,
);
```

- [ ] **Step 5: Rodar testes**

```bash
cd apps/mobile && flutter test test/features/notifications/notification_inbox_controller_test.dart
```

Esperado: 4 testes passando.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_state.dart
git add apps/mobile/lib/features/notifications/presentation/providers/notification_inbox_controller.dart
git add apps/mobile/test/features/notifications/notification_inbox_controller_test.dart
git commit -m "feat: add NotificationInboxController with load/markAllRead and tests"
```

---

### Task 4: Strings i18n (pt / en / es / fr)

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`
- Modify: `apps/mobile/lib/l10n/app_fr.arb`

Em cada arquivo, **substituir a última linha** `}` pelo bloco abaixo (o arquivo já termina sem vírgula, então a vírgula vem antes dos novos campos).

- [ ] **Step 1: app_pt.arb** — substituir `}` final por:

```json
  ,
  "notificationInboxTitle": "Notificações",
  "@notificationInboxTitle": { "description": "Título da tela de inbox de notificações" },
  "notificationInboxEmpty": "Nenhuma notificação por aqui.",
  "@notificationInboxEmpty": { "description": "Estado vazio do inbox" },
  "notificationInboxMarkAllRead": "Marcar todas como lidas",
  "@notificationInboxMarkAllRead": { "description": "Botão para marcar todas como lidas" },
  "notificationInboxError": "Não foi possível carregar as notificações.",
  "@notificationInboxError": { "description": "Erro ao carregar inbox" },
  "notificationInboxRetry": "Tentar novamente",
  "@notificationInboxRetry": { "description": "Botão de retry no inbox" },
  "notificationTypeStreakAlert": "Streak",
  "@notificationTypeStreakAlert": { "description": "Rótulo de tipo: alerta de streak" },
  "notificationTypeQuestReminder": "Quest",
  "@notificationTypeQuestReminder": { "description": "Rótulo de tipo: lembrete de quest" },
  "notificationTypeReactivation": "Novidade",
  "@notificationTypeReactivation": { "description": "Rótulo de tipo: reativação" },
  "notificationTypeSystem": "Sistema",
  "@notificationTypeSystem": { "description": "Rótulo de tipo: sistema" }
}
```

- [ ] **Step 2: app_en.arb** — substituir `}` final por:

```json
  ,
  "notificationInboxTitle": "Notifications",
  "@notificationInboxTitle": { "description": "Notification inbox screen title" },
  "notificationInboxEmpty": "Nothing here yet.",
  "@notificationInboxEmpty": { "description": "Inbox empty state" },
  "notificationInboxMarkAllRead": "Mark all as read",
  "@notificationInboxMarkAllRead": { "description": "Button to mark all as read" },
  "notificationInboxError": "Could not load notifications.",
  "@notificationInboxError": { "description": "Error loading inbox" },
  "notificationInboxRetry": "Try again",
  "@notificationInboxRetry": { "description": "Retry button in inbox" },
  "notificationTypeStreakAlert": "Streak",
  "@notificationTypeStreakAlert": { "description": "Type label: streak alert" },
  "notificationTypeQuestReminder": "Quest",
  "@notificationTypeQuestReminder": { "description": "Type label: quest reminder" },
  "notificationTypeReactivation": "News",
  "@notificationTypeReactivation": { "description": "Type label: reactivation" },
  "notificationTypeSystem": "System",
  "@notificationTypeSystem": { "description": "Type label: system" }
}
```

- [ ] **Step 3: app_es.arb** — substituir `}` final por:

```json
  ,
  "notificationInboxTitle": "Notificaciones",
  "@notificationInboxTitle": { "description": "Título de la pantalla de bandeja" },
  "notificationInboxEmpty": "Nada por aquí aún.",
  "@notificationInboxEmpty": { "description": "Estado vacío de la bandeja" },
  "notificationInboxMarkAllRead": "Marcar todas como leídas",
  "@notificationInboxMarkAllRead": { "description": "Botón para marcar todas como leídas" },
  "notificationInboxError": "No se pudieron cargar las notificaciones.",
  "@notificationInboxError": { "description": "Error al cargar bandeja" },
  "notificationInboxRetry": "Intentar de nuevo",
  "@notificationInboxRetry": { "description": "Botón de reintento en bandeja" },
  "notificationTypeStreakAlert": "Racha",
  "@notificationTypeStreakAlert": { "description": "Etiqueta: alerta de racha" },
  "notificationTypeQuestReminder": "Quest",
  "@notificationTypeQuestReminder": { "description": "Etiqueta: recordatorio de quest" },
  "notificationTypeReactivation": "Novedades",
  "@notificationTypeReactivation": { "description": "Etiqueta: reactivación" },
  "notificationTypeSystem": "Sistema",
  "@notificationTypeSystem": { "description": "Etiqueta: sistema" }
}
```

- [ ] **Step 4: app_fr.arb** — substituir `}` final por:

```json
  ,
  "notificationInboxTitle": "Notifications",
  "@notificationInboxTitle": { "description": "Titre de la boîte de réception" },
  "notificationInboxEmpty": "Rien ici pour l'instant.",
  "@notificationInboxEmpty": { "description": "État vide de la boîte de réception" },
  "notificationInboxMarkAllRead": "Tout marquer comme lu",
  "@notificationInboxMarkAllRead": { "description": "Bouton pour tout marquer comme lu" },
  "notificationInboxError": "Impossible de charger les notifications.",
  "@notificationInboxError": { "description": "Erreur de chargement" },
  "notificationInboxRetry": "Réessayer",
  "@notificationInboxRetry": { "description": "Bouton de réessai" },
  "notificationTypeStreakAlert": "Série",
  "@notificationTypeStreakAlert": { "description": "Étiquette: alerte de série" },
  "notificationTypeQuestReminder": "Quête",
  "@notificationTypeQuestReminder": { "description": "Étiquette: rappel de quête" },
  "notificationTypeReactivation": "Actualités",
  "@notificationTypeReactivation": { "description": "Étiquette: réactivation" },
  "notificationTypeSystem": "Système",
  "@notificationTypeSystem": { "description": "Étiquette: système" }
}
```

- [ ] **Step 5: Gerar l10n**

```bash
cd apps/mobile && flutter gen-l10n
```

Esperado: sem erros. Novos getters disponíveis em `AppLocalizations`.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/lib/l10n/
git commit -m "feat: add notification inbox i18n strings (pt/en/es/fr)"
```

---

### Task 5: `NotificationInboxPage`

**Files:**
- Create: `apps/mobile/lib/features/notifications/presentation/pages/notification_inbox_page.dart`

- [ ] **Step 1: Criar a página com loading / empty / error / list states**

```dart
// apps/mobile/lib/features/notifications/presentation/pages/notification_inbox_page.dart

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:awaken/design_system/components/awaken_empty_state.dart';
import 'package:awaken/design_system/components/awaken_error_state.dart';
import 'package:awaken/design_system/components/awaken_loading_page.dart';
import 'package:awaken/design_system/components/awaken_panel.dart';
import 'package:awaken/design_system/tokens/colors.dart';
import 'package:awaken/design_system/tokens/spacing.dart';
import 'package:awaken/design_system/tokens/typography.dart';
import 'package:awaken/features/notifications/domain/entities/inbox_notification.dart';
import 'package:awaken/features/notifications/presentation/providers/notification_inbox_controller.dart';
import 'package:awaken/features/notifications/presentation/providers/notification_inbox_state.dart';
import 'package:awaken/l10n/app_localizations.dart';

class NotificationInboxPage extends ConsumerStatefulWidget {
  const NotificationInboxPage({super.key});

  @override
  ConsumerState<NotificationInboxPage> createState() =>
      _NotificationInboxPageState();
}

class _NotificationInboxPageState
    extends ConsumerState<NotificationInboxPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(notificationInboxControllerProvider.notifier).load();
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = ref.watch(notificationInboxControllerProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.notificationInboxTitle),
        actions: [
          if (state is NotificationInboxLoaded && state.unreadCount > 0)
            TextButton(
              onPressed: () => ref
                  .read(notificationInboxControllerProvider.notifier)
                  .markAllRead(),
              child: Text(
                l10n.notificationInboxMarkAllRead,
                style: AwakenTypography.labelSmall.copyWith(
                  color: AwakenColors.xp,
                ),
              ),
            ),
        ],
      ),
      body: switch (state) {
        NotificationInboxInitial() || NotificationInboxLoading() =>
          const AwakenLoadingPage(),
        NotificationInboxError() => AwakenErrorState(
            message: l10n.notificationInboxError,
            onRetry: () =>
                ref.read(notificationInboxControllerProvider.notifier).load(),
            retryLabel: l10n.notificationInboxRetry,
          ),
        NotificationInboxLoaded(notifications: final items) when items.isEmpty =>
          AwakenEmptyState(message: l10n.notificationInboxEmpty),
        NotificationInboxLoaded(notifications: final items) =>
          ListView.separated(
            padding: const EdgeInsets.all(AwakenSpacing.md),
            itemCount: items.length,
            separatorBuilder: (_, __) =>
                const SizedBox(height: AwakenSpacing.sm),
            itemBuilder: (context, index) => _NotificationCard(
              notification: items[index],
              l10n: l10n,
            ),
          ),
      },
    );
  }
}

// ── Card ─────────────────────────────────────────────────────────────────────

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({
    required this.notification,
    required this.l10n,
  });

  final InboxNotification notification;
  final AppLocalizations l10n;

  String _typeLabel() => switch (notification.type) {
        InboxNotificationType.streakAlert => l10n.notificationTypeStreakAlert,
        InboxNotificationType.questReminder =>
          l10n.notificationTypeQuestReminder,
        InboxNotificationType.reactivation =>
          l10n.notificationTypeReactivation,
        InboxNotificationType.system => l10n.notificationTypeSystem,
      };

  Color _typeColor() => switch (notification.type) {
        InboxNotificationType.streakAlert => AwakenColors.amber,
        InboxNotificationType.questReminder => AwakenColors.xp,
        InboxNotificationType.reactivation => AwakenColors.rankB,
        InboxNotificationType.system => AwakenColors.textMuted,
      };

  @override
  Widget build(BuildContext context) {
    final isUnread = !notification.isRead;

    return AwakenPanel(
      padding: const EdgeInsets.all(AwakenSpacing.md),
      borderColor: isUnread
          ? AwakenColors.borderDefault
          : AwakenColors.borderDefault.withValues(alpha: 0.4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 14,
            child: isUnread
                ? Padding(
                    padding: const EdgeInsets.only(top: 5),
                    child: Container(
                      width: 6,
                      height: 6,
                      decoration: const BoxDecoration(
                        color: AwakenColors.xp,
                        shape: BoxShape.circle,
                      ),
                    ),
                  )
                : null,
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    _TypeChip(label: _typeLabel(), color: _typeColor()),
                    const Spacer(),
                    Text(
                      _formatAge(notification.receivedAt),
                      style: AwakenTypography.labelSmall.copyWith(
                        color: AwakenColors.textDisabled,
                        fontSize: 10,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AwakenSpacing.xs),
                Text(
                  notification.title,
                  style: AwakenTypography.titleSmall.copyWith(
                    color: isUnread
                        ? AwakenColors.textPrimary
                        : AwakenColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  notification.body,
                  style: AwakenTypography.bodySmall.copyWith(
                    color: AwakenColors.textMuted,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _formatAge(DateTime receivedAt) {
    final diff = DateTime.now().difference(receivedAt);
    if (diff.inMinutes < 60) return '${diff.inMinutes}m';
    if (diff.inHours < 24) return '${diff.inHours}h';
    return '${diff.inDays}d';
  }
}

class _TypeChip extends StatelessWidget {
  const _TypeChip({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AwakenSpacing.xs,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        label,
        style: AwakenTypography.labelSmall.copyWith(
          color: color,
          fontSize: 9,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
```

- [ ] **Step 2: Verificar — AwakenErrorState aceita `onRetry` e `retryLabel`**

Antes de salvar, conferir a assinatura de `AwakenErrorState` em:
`apps/mobile/lib/design_system/components/awaken_error_state.dart`

Se os parâmetros tiverem nomes diferentes (ex.: `onAction`, `actionLabel`), ajustar o código acima para usar os nomes corretos.

- [ ] **Step 3: Verificar compilação**

```bash
cd apps/mobile && flutter analyze lib/features/notifications/presentation/pages/notification_inbox_page.dart
```

Esperado: sem erros.

- [ ] **Step 4: Commit**

```bash
git add apps/mobile/lib/features/notifications/presentation/pages/notification_inbox_page.dart
git commit -m "feat: add NotificationInboxPage with loading/empty/error/list states"
```

---

### Task 6: Rota `/notifications`

**Files:**
- Modify: `apps/mobile/lib/app/app_router.dart`

- [ ] **Step 1: Adicionar constante de rota em `AppRoutes`**

Após `static const notificationReminderTime = '/notification-reminder-time';` adicionar:

```dart
static const notificationInbox = '/notifications';
```

- [ ] **Step 2: Adicionar import**

Adicionar junto aos demais imports:

```dart
import '../features/notifications/presentation/pages/notification_inbox_page.dart';
```

- [ ] **Step 3: Registrar `GoRoute`**

Após a rota `notificationReminderTime` no array `routes`, adicionar:

```dart
GoRoute(
  path: AppRoutes.notificationInbox,
  pageBuilder: (ctx, state) => _buildPage(
    state: state,
    child: const NotificationInboxPage(),
  ),
),
```

- [ ] **Step 4: Verificar**

```bash
cd apps/mobile && flutter analyze lib/app/app_router.dart
```

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/lib/app/app_router.dart
git commit -m "feat: register /notifications route"
```

---

### Task 7: Wiring na `HomePage` — onTap + badge dinâmico

**Files:**
- Modify: `apps/mobile/lib/features/home/presentation/pages/home_page.dart`

- [ ] **Step 1: Adicionar import do controller**

Adicionar no topo do arquivo:

```dart
import 'package:awaken/features/notifications/presentation/providers/notification_inbox_controller.dart';
import 'package:awaken/features/notifications/presentation/providers/notification_inbox_state.dart';
```

- [ ] **Step 2: Tornar `_HudIconButton.badge` nullable e adicionar `onTap`**

Substituir a classe `_HudIconButton` inteira (~linha 677) por:

```dart
class _HudIconButton extends StatelessWidget {
  const _HudIconButton({
    required this.icon,
    required this.badgeColor,
    required this.iconColor,
    this.badge,
    this.onTap,
  });

  final IconData icon;
  final String? badge;
  final Color badgeColor;
  final Color iconColor;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          AwakenPanel(
            width: 40,
            height: 40,
            surfaceColor: AwakenColors.cardElevated,
            surfaceOpacity: AwakenColors.cardOpacity,
            borderColor: AwakenColors.borderDefault,
            child: Icon(icon, color: iconColor, size: 19),
          ),
          if (badge != null)
            Positioned(
              top: -5,
              right: -5,
              child: Container(
                constraints: const BoxConstraints(minWidth: 18),
                height: 18,
                padding: const EdgeInsets.symmetric(horizontal: 5),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: badgeColor,
                  borderRadius: BorderRadius.circular(99),
                ),
                child: Text(
                  badge!,
                  style: AwakenTypography.labelSmall.copyWith(
                    color: AwakenColors.textOnPrimary,
                    fontSize: 9,
                    letterSpacing: 0,
                    fontWeight: FontWeight.w700,
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

- [ ] **Step 3: Converter `_PlayerHeader` para `ConsumerWidget`**

Substituir:

```dart
class _PlayerHeader extends StatelessWidget {
  const _PlayerHeader({
    required this.player,
    required this.isLoading,
  });

  final PlayerData player;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
```

por:

```dart
class _PlayerHeader extends ConsumerWidget {
  const _PlayerHeader({
    required this.player,
    required this.isLoading,
  });

  final PlayerData player;
  final bool isLoading;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
```

- [ ] **Step 4: Atualizar os dois `_HudIconButton` dentro de `_PlayerHeader.build`**

Substituir o bloco dos dois botões (~linhas 445-457):

```dart
          const SizedBox(width: AwakenSpacing.sm),
          _HudIconButton(
            icon: Icons.local_fire_department,
            badge: '${player.streakDays}',
            badgeColor: AwakenColors.amber,
            iconColor: AwakenColors.amber,
          ),
          const SizedBox(width: AwakenSpacing.sm),
          const _HudIconButton(
            icon: Icons.notifications,
            badge: '3',
            badgeColor: AwakenColors.error,
            iconColor: AwakenColors.xp,
          ),
```

por:

```dart
          const SizedBox(width: AwakenSpacing.sm),
          _HudIconButton(
            icon: Icons.local_fire_department,
            badge: '${player.streakDays}',
            badgeColor: AwakenColors.amber,
            iconColor: AwakenColors.amber,
            onTap: () => context.go(AppRoutes.progression),
          ),
          const SizedBox(width: AwakenSpacing.sm),
          _InboxBellButton(),
```

- [ ] **Step 5: Adicionar `_InboxBellButton` no final do arquivo (antes do último `}`)**

```dart
class _InboxBellButton extends ConsumerWidget {
  const _InboxBellButton();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final inboxState = ref.watch(notificationInboxControllerProvider);
    final unread = inboxState is NotificationInboxLoaded
        ? inboxState.unreadCount
        : 0;

    return _HudIconButton(
      icon: Icons.notifications,
      badge: unread > 0 ? '$unread' : null,
      badgeColor: AwakenColors.error,
      iconColor: AwakenColors.xp,
      onTap: () => context.go(AppRoutes.notificationInbox),
    );
  }
}
```

- [ ] **Step 6: Verificar compilação**

```bash
cd apps/mobile && flutter analyze lib/features/home/presentation/pages/home_page.dart
```

Esperado: sem erros.

- [ ] **Step 7: Rodar todos os testes**

```bash
cd apps/mobile && flutter test
```

Esperado: todos passando.

- [ ] **Step 8: Commit**

```bash
git add apps/mobile/lib/features/home/presentation/pages/home_page.dart
git commit -m "feat: wire bell to notification inbox and streak to progression, dynamic badge"
```

---

## Verificação final

```bash
cd apps/mobile && flutter analyze && flutter test
```

**Fluxos a testar manualmente:**

1. Home → tap no sino 🔔 → abre inbox com loading → empty state "Nenhuma notificação por aqui."
2. Home → tap no fogo 🔥 → navega para Progressão com painel de streak
3. Sino sem badge quando 0 não lidas (badge some completamente)
4. Quando backend implementar `GET /api/notifications/inbox`, remover o `catch 404 → return []` em `NotificationInboxRemoteDataSource.getInbox()` — sem mais mudanças necessárias
