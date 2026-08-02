# Orphan Audit Report — US-185

**Data:** 2026-06-28  
**Branch:** docs/epic015-user-stories  
**Executor:** Claude Code (US-185)

---

## 1. ARB Key Audit

### Keys Removed (97 keys, all 4 locales)

Keys with no usage in any non-generated Dart file under `apps/mobile/lib/`, confirmed via exhaustive search. Removed from `app_pt.arb`, `app_en.arb`, `app_es.arb`, `app_fr.arb` and regenerated with `flutter gen-l10n`.

**Onboarding — replaced by hardcoded strings in `onboarding_page.dart` steps 1–2:**
`onboardingWelcomeTitle`, `onboardingWelcomeSubtitle`, `onboardingGoalTitle`, `onboardingGoalGainMuscle`, `onboardingGoalGainStrength`, `onboardingGoalImproveCondition`, `onboardingGoalLoseWeight`, `onboardingGoalStayActive`, `onboardingLevelTitle`, `onboardingLevelBeginner`, `onboardingLevelIntermediate`, `onboardingLevelAdvanced`, `onboardingDaysTitle`, `onboardingTimeTitle`, `onboardingNextButton`, `onboardingFinishButton`, `onboardingMinutes`, `onboardingLimitationsBack`, `onboardingLimitationsKnee`, `onboardingLimitationsNone`, `onboardingLimitationsOther`, `onboardingLimitationsShoulder`, `onboardingPhysicalSexHint`, `onboardingCompleteSaving`

**Profile edit — training location/preferences section not implemented in `edit_profile_page.dart`:**
`profileEditTrainingLocationLabel`, `profileEditTrainingLocationHome`, `profileEditTrainingLocationGym`, `profileEditTrainingLocationOutdoor`, `profileEditTrainingPreferencesLabel`, `profileEditPreferenceLowImpact`, `profileEditPreferenceMobility`, `profileEditPreferenceShortSessions`, `profileEditPreferenceStrengthFocus`, `profileEditAccessExpiredError`, `profileEditLoadUnexpectedError`

**Quest execution — keys defined but not wired up in any widget:**
`preQuestTitle`, `dailyQuestSubtitle`, `editQuestButton`, `startQuestButton`, `dailyQuestBackTooltip`, `questExecutionPausedTitle`, `questExecutionPausedMessage`, `questExecutionPlaceholder`, `questExecutionSessionLabel`, `questExecutionRestTitle`, `questExecutionRestPauseButton`, `questExecutionRestResumeButton`, `questExecutionRestFinishedMessage`, `questRewardAttributePointsGrantedTitle`

**Home/profile keys defined but never referenced:**
`homeTitle`, `homeGreetingMorning`, `homeGreetingAfternoon`, `homeGreetingEvening`, `homeNutritionCardSubtitle`, `hunterProfileTitle`, `hunterProfileStreakLabel`, `hunterCardPremiumBadgeLabel`, `shareCardButton`

**Nutrition — old basic keys superseded by specific card keys:**
`waterLabel`, `waterGoal`, `logWaterButton`, `waterLoggingSuccess`, `logProteinButton`, `proteinLabel`, `proteinGoal`

**Subscriptions/paywall — unused variants:**
`subscriptionActiveStatus`, `subscriptionMonthlyTitle`, `subscriptionAnnualTitle`, `subscriptionSaveLabel`, `planMonthlyPriceLabel`, `planAnnualPriceLabel`, `paywallAnnualSavingsLabel`, `paywallSelectedLabel`, `paywallSettingsLink`, `trialOfferFallbackNote`, `trialStatusExpiringSoon`, `trialReminderViewPlansButton`, `syncEntitlementConnectionError`

**Miscellaneous unused:**
`appName`, `rankUp`, `xpGained`, `streakLabel`, `inventoryEmptySlot`, `loginForgotPasswordComingSoon`, `exerciseFeedbackLevelUp`, `changeTypeSuccessMessage`, `changeTypePreferenceSavedMessage`, `changeTypePreferenceSaveErrorMessage`, `registerContinueButton`, `registerBackButton`, `registerCredentialsSubtitle`, `responsibilityNoticeButton`, `settingsTitle`, `settingsNotifications`, `settingsPrivacy`, `settingsTerms`

### Keys Kept (intentionally not removed)

| Key group | Reason |
|---|---|
| `onboardingEquipment*` (5 keys) | Protected — about to be used by new onboarding step |
| `settingsContactUsLabel`, `settingsFaqLabel`, `settingsAboutLabel` | Protected — actions coming in US-175 |
| `pricingFooter` | Protected — another agent modifying |
| `dailyQuestReminderPushTitle/Body`, `dailyQuestReminderInAppTitle` | Push notification templates, owned by backend services |
| `streakRiskAlertPushTitle/Body`, `streakRiskAlertInAppTitle` | Push notification templates, owned by backend services |
| `notificationPermissionGrantedTitle/Message`, `notificationPermissionDeniedMessage` | Confirmation/denial state screens (may be rendered soon) |
| `notificationReminderTimeDefaultLabel/LoadError/SaveError` | Settings page sub-states, conservative keep |
| `waterCupVolumeCurrentLabel`, `waterCupVolumeSavedMessage`, `waterCupVolumeSubtitle`, `waterCupVolumeInvalidMessage` | Cup volume detail keys — the widget uses `waterCupVolumeTitle` only; detailed keys likely for a forthcoming modal/settings flow |
| `hunterProfileNoProgressAction` | Referenced in `integration_test/hunter_profile_flow_test.dart:309` |

---

## 2. Backend DTO Audit

All DTOs in `backend/src/Awaken.Contracts/` are referenced in Application handlers or Api controllers.

`ApiResponse.cs` contains three types: `ApiResponse<T>` (generic wrapper, not directly used), `ApiErrorResponse` (used in `ExceptionHandlingMiddleware.cs`), and `FieldError` (used in `ExceptionHandlingMiddleware.cs`). The `ApiResponse<T>` wrapper is technically orphaned but removing it would be premature — it's the standard envelope type. **No DTOs removed.**

---

## 3. Dead Flutter Routes

All routes in `AppRoutes` have corresponding `GoRoute` definitions and page widgets. However, one route has no navigation call pointing to it:

| Route | Path | Status |
|---|---|---|
| `AppRoutes.settingsLanguage` | `/settings/language` | **Dead** — `LanguageSettingsPage` exists and is registered in the router, but no `context.push(AppRoutes.settingsLanguage)` exists anywhere. Language selection was moved inline into `settings_page.dart` (flags row). The page itself still works if navigated to directly. |

Routes NOT removed (about to get navigation from other agents):
- Ticket form route and equipment settings route (created by other agents per task note)

---

## 4. Mock Shop Items — Gold Price Display

**Status: Disclaimer added.**

`ReforgeScroll` and `DungeonStone` in the backend `ShopCatalog` have `priceGold` values that are mock placeholder prices. The shop page (`shop_page.dart`) currently shows a "Comprar" button that calls the real backend endpoint (`POST /api/shop/items/{itemKey}/purchase`) — there is no IAP integration yet.

A visual disclaimer widget (`_ShopMockDisclaimer`) was added above the items list in `shop_page.dart` with the text: *"Itens em breve disponíveis na loja da plataforma."*

The `_formatPrice` function in `player_screen_shell.dart` formats Gold prices as `"N Gold"` — this value is computed but only rendered for items where `itemKey == null` (none currently in catalog). Gold prices are not visible to the user for current catalog items. The disclaimer covers the intent.

Per US-178/179, these will be replaced with real IAP products.

---

## 5. Backend Endpoints Without Flutter Consumers

| Endpoint | Controller | Flutter consumer? |
|---|---|---|
| `GET /api/users/me/onboarding-status` | `UsersController` | **No** — result is not fetched from Flutter; onboarding state is inferred from `SessionState` |
| `GET /api/users/me/effective-level` | `UsersController` | **No** — effective level not directly fetched by any data source |
| `POST /api/admin/exercises/import` | `AdminExercisesController` | **No** — admin-only, not a client endpoint |
| `GET /api/exercises` | `ExercisesController` | **No** — exercise catalog not fetched by Flutter UI yet |

These endpoints are not deleted — they serve valid backend purposes or will be consumed in future iterations. Added this documentation for tracking.

---

## 6. Settings Tiles With No Action

In `settings_page.dart` lines 158–171, three tiles have no `onTap`:

- `settingsContactUsLabel` — no action
- `settingsFaqLabel` — no action  
- `settingsAboutLabel` — no action

These will be fixed by US-175. **Not touched here.**

---

## 7. Config Values Without Features

`appsettings.json` config keys reviewed:

| Key | Used? |
|---|---|
| `Jwt.*` (Secret, Issuer, Audience, expiry) | Yes — `JwtTokenService` |
| `Serilog.*` | Yes — logging pipeline |
| `Google.ClientId` | Yes — Google OAuth |
| `Firebase.*` | Yes — FCM notifications |
| `OpenAI.*` | Yes — workout generation |
| `ExerciseProvider.*` | Yes — exercise catalog import |
| `Cloudflare.*` | Yes — R2 asset storage |
| `Features.PremiumCardEnabled` | Yes — `FeatureFlagsService.IsPremiumCardEnabled` → `GetHunterProfileQueryHandler` |

No orphan config keys found.

---

## Build Status

### Flutter
`flutter analyze` after changes: **15 issues** — all pre-existing from parallel agent work on the onboarding `equipmentAvailable` parameter and `in_app_review` package. **Zero new errors from this audit.**

### Backend
`dotnet build Awaken.Api.csproj`: **1 pre-existing error** in `CompleteExerciseCommandHandler.cs` from another agent's modification to `HunterProgression.AddXp` signature in `HunterProgression.cs`. **Zero new errors from this audit.**
