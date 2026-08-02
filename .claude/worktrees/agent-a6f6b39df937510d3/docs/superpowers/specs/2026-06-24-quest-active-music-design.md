# Música de quest ativa

## Contexto

Hoje existe `BackgroundMusicService` (`apps/mobile/lib/core/audio/background_music_service.dart`), um único `AudioPlayer` (pacote `audioplayers`) que toca `audio/awaken_hunter.mp3` em loop desde a `splash_page`, com volume controlado por `SoundSettingsController` (`sound_music_volume` em `SharedPreferences`).

O fluxo de quest diária hoje é: `pre_quest_page` (seleção/preview, antes de iniciar) → `quest_execution_page` (lista de exercícios, status `pending`/`in_progress`/`paused`/`cancelled`/`completed`) → `exercise_execution_page` (`Navigator.push` por cima de `quest_execution_page`, não substitui a rota).

Ações na `quest_execution_page`:
- **Pausar**: botão de voltar (seta), com confirmação. Chama `pause()` no controller e depois sai da tela (`context.pop()` ou `context.go(home)`).
- **Cancelar**: botão de cancelar, com confirmação. Chama `cancel()`; usuário **permanece** na mesma tela, que passa a mostrar painel de status `cancelled`.
- **Completar**: ao concluir o último exercício, o status muda para `completed`; usuário permanece na tela com painel de conclusão.
- **Retomar**: botão de retomar quando status é `paused`; chama `resume()`, volta para `in_progress` na mesma tela.

Dungeon, raid e programa de treino **não existem** ainda no código — fora do scope deste spec.

## Objetivo

Enquanto o usuário está executando uma quest ativamente (status `in_progress`), tocar uma trilha diferente (`Awaken Protocol.mp3`) em vez da trilha padrão. Ao pausar, cancelar, completar (ou qualquer outro status/erro) e ao sair da tela, voltar para a trilha padrão. Ao retomar (`resume`), voltar para a trilha de quest.

## Design

### Asset

Copiar `docs/design-system/assets/Awaken Protocol.mp3` para `apps/mobile/assets/audio/awaken_protocol.mp3`, seguindo a convenção de nome já usada (`awaken_hunter.mp3`). A pasta `assets/audio/` já está registrada em `pubspec.yaml`, nenhuma mudança de config necessária.

### `BackgroundMusicService`

Reaproveitar o player único existente (não criar um segundo `AudioPlayer` — evita disputa de foco de áudio e mixagem indesejada). Adicionar:

- Constante `_questAsset = 'audio/awaken_protocol.mp3'`.
- Método `playQuestTheme()`, espelhando `playTheme()`: `setAudioContext` → `setReleaseMode(ReleaseMode.loop)` → `setVolume(persisted)` → `setSource(AssetSource(_questAsset))` → `resume()`.
- `playTheme()` existente é reusado, sem alteração, para voltar à trilha padrão (troca de `source` no mesmo player).

Nenhuma mudança em `BackgroundMusicProvider` ou `SoundSettingsController` — volume já se aplica a ambas as trilhas, pois é o mesmo player.

### Disparo da troca

Toda a lógica fica em `QuestExecutionPage` (`apps/mobile/lib/features/quests/presentation/pages/quest_execution_page.dart`), única tela no scope atual:

- Estado local `bool _questMusicActive = false` na `_QuestExecutionPageState`.
- Em `build()`, usar `ref.listen(questExecutionControllerProvider, (previous, next) { ... })`:
  - Se `next` é `QuestExecutionLoaded` com `execution.status == 'in_progress'` e `!_questMusicActive` → chama `backgroundMusicServiceProvider.playQuestTheme()`, seta `_questMusicActive = true`.
  - Em qualquer outro caso (`paused`, `cancelled`, `completed`, `pending`, `QuestExecutionAccessBlocked`, `QuestExecutionNotFound`, `QuestExecutionNotInProgress`, `QuestExecutionNoExercises`, `QuestExecutionNetworkError`, `QuestExecutionUnexpectedError`, `QuestExecutionLoading`) e `_questMusicActive` → chama `playTheme()`, seta `_questMusicActive = false`.
- Em `dispose()`: se `_questMusicActive`, chama `playTheme()` antes de finalizar (rede de segurança — cobre saída da tela enquanto ainda `in_progress`, ex. navegação externa).

`exercise_execution_page` é empilhada via `Navigator.push` por cima de `quest_execution_page`, que continua montada (não é descartada). O listener em `quest_execution_page` permanece ativo mesmo com `exercise_execution_page` no topo da pilha visual, então a troca de trilha ao completar/pausar/cancelar durante um exercício individual funciona sem lógica duplicada em `exercise_execution_page`.

Erros do `AudioPlayer` (ex. asset não encontrado) seguem o padrão já usado em `splash_page.dart` (`.catchError`) — não devem travar a navegação da quest.

### Fora do scope

Dungeon, raid, programa de treino: não existem no código. Quando essas features forem implementadas, devem reusar `playQuestTheme()` / `playTheme()` do mesmo `BackgroundMusicService`, replicando o padrão de listener por status descrito aqui.

## Testes

- Novo teste (ou extensão de `quest_execution_page_test.dart` se existir, senão criar) usando mock de `BackgroundMusicService` via `backgroundMusicServiceProvider` override, verificando:
  - `in_progress` → `playQuestTheme()` chamado.
  - `in_progress → paused` → `playTheme()` chamado.
  - `in_progress → cancelled` → `playTheme()` chamado.
  - `in_progress → completed` → `playTheme()` chamado.
  - `paused → in_progress` (resume) → `playQuestTheme()` chamado novamente.
  - dispose com status ainda `in_progress` → `playTheme()` chamado.
- `flutter analyze` limpo.
