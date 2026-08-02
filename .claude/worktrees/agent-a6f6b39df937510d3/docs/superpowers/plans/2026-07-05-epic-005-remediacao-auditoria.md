# EPIC-005 — Remediação da Auditoria (US-035 a US-149) — Plano de Implementação

> **Para agentes:** execute task por task. Este plano assume que EPIC-005 (US-035 a US-041, US-143 a US-149, US-236 a US-240) já está no working tree. Ele corrige os gaps encontrados numa auditoria de código feita depois da implementação. Não dê `git commit`. Branch atual: `docs/epic-005-user-stories`.

**Goal:** Fechar os gaps reais encontrados na auditoria: (R1) filtro de segurança por dor/limitação não funciona com dado real de onboarding (vocabulário incompatível); (R2) instruções passo-a-passo do exercício nunca chegam à tela; (R3) pipeline de import não tem tradução real, não tem sanitização real, não tem approve/reject de curador; (R4) nenhum evento de analytics do épico inteiro está implementado.

**Contexto da auditoria (resumo, não repita a leitura completa — já foi feita):**
- `CompleteOnboardingCommandValidator.cs` define o vocabulário real de `PhysicalLimitations` (`no_limitations, disk_herniation, knee_problem, no_impact, shoulder_injury, chronic_lumbar_pain, medical_restriction`) e `PhysicalPains` (`no_pains, neck, shoulder, wrist, back, lower_back, knees`).
- O catálogo usa outro vocabulário pras tags de risco/articulação: `knee_high_stress, lumbar_high_stress, shoulder_high_stress, wrist_high_stress, ankle_high_stress, hip_high_stress, cervical_high_stress, high_impact, high_technical_complexity, requires_spotter, requires_load_control` (`ImportExercisesCommandHandler.BuildJointStressTags/BuildRiskTags`).
- `ExerciseSafetyFilter.HasConflict` faz comparação exata de string entre os dois vocabulários — nunca batem com dado real.
- `WorkoutGeneratorService.cs` serializa `instructions` no JSON do treino, mas `QuestResponseMapper`/DTOs Flutter só expõem `description` (frase única) — a lista de instruções e as dicas (`TipsPtBr`, nem serializado) se perdem.
- `ImportExercisesCommandHandler.BuildCatalogSnapshot` copia `name`/`instructions` em inglês direto pra `NamePtBr`/`InstructionsPtBr` — não existe tradução.
- Todo item importado vira `pending_review` incondicionalmente (sem validar equipamento/dificuldade/goalTags antes).
- Não existe endpoint approve/reject de curador — só a flag `approveOnImport` no próprio import. `ExerciseRawImportStatus` já tem os valores de enum `Rejected`/`Deprecated`, só não são usados.
- Nenhum dos eventos de analytics citados nas docs do épico existe em código. O padrão real do projeto pra "analytics" é `logger.LogInformation("event_name key=value ...")` (ver `GenerateDailyQuestCommandHandler.cs`, evento `daily_quest_generated`) — **não existe `IAnalyticsService`**.

---

## Ondas de execução

```
Onda RA (paralelo, arquivos disjuntos):
  Agente R1 → Task R1 (US-040: ponte de vocabulário onboarding↔catálogo)
  Agente R2 → Task R2 (US-041: instruções chegam na tela, pre-quest E quest-execution)
  Agente R3 → Task R3 (US-143/144/148/149: tradução real, sanitização real, approve/reject)

Onda RB (sequencial, depende de tudo acima — toca os mesmos arquivos que RA acabou de mudar):
  Agente R4 → Task R4 (eventos de analytics nas 19 USes do épico + verificação final completa)
```

**Regras que já causaram dor de cabeça em rodadas anteriores — siga à risca:**
- Nunca rode testes de integração (Testcontainers) em background/Monitor e espere passivamente — subagentes não recebem notificação automática de jobs que eles mesmos iniciam. Rode SEMPRE de forma síncrona/bloqueante.
- Prefira rodar só `dotnet test backend/tests/Awaken.UnitTests` pra validar seu trabalho (rápido, sem Docker). A suíte de integração completa é responsabilidade da Task R4 (verificação final), quando só ela estiver rodando.
- Nunca `dotnet ef migrations remove`. Se precisar corrigir uma migration, edite os arquivos manualmente.
- Nunca `taskkill /IM dotnet.exe` (mata tudo). Use PID específico.
- Ao editar arquivos compartilhados, use `Edit` cirúrgico, não reescreva o arquivo inteiro.

---

## Task R1 — US-040: ponte de vocabulário onboarding ↔ catálogo (Agente R1, Onda RA)

**Problema:** `ExerciseSafetyFilter.HasConflict` compara string exata entre `context.PhysicalLimitations`/`PhysicalPains` (vocabulário do onboarding) e `exercise.LimitationBlockTags`/`ContraindicationTags`/`PainBlockTags` (vocabulário do catálogo). Nunca coincidem com dado real.

**Files:**
- Create: `backend/src/Awaken.Domain/Services/Quests/OnboardingTagTranslator.cs` — serviço de domínio puro (static).
- Modify: `backend/src/Awaken.Infrastructure/Services/WorkoutGeneratorService.cs` — no método `ParseProfile` (usado tanto por `GenerateWorkoutJsonAsync` quanto por `SelectSubstituteExerciseAsync`), traduzir `physicalLimitations`/`physicalPains` ANTES de colocar no `ExerciseSafetyContext`.
- Test: `backend/tests/Awaken.UnitTests/Quests/OnboardingTagTranslatorTests.cs`
- Test: estender `backend/tests/Awaken.UnitTests/Infrastructure/WorkoutGeneratorServiceTests.cs` (ou onde estiver) com um teste de ponta a ponta: perfil com `knee_problem` + exercício tagueado `knee_high_stress` → exercício é removido/substituído.

```csharp
namespace Awaken.Domain.Services.Quests;

public static class OnboardingTagTranslator
{
    // Baseado no vocabulario real de CompleteOnboardingCommandValidator.cs
    private static readonly IReadOnlyDictionary<string, string[]> LimitationMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["disk_herniation"] = ["lumbar_high_stress"],
        ["knee_problem"] = ["knee_high_stress"],
        ["no_impact"] = ["high_impact"],
        ["shoulder_injury"] = ["shoulder_high_stress"],
        ["chronic_lumbar_pain"] = ["lumbar_high_stress"],
        // conservador de proposito: restricao medica generica bloqueia as categorias de risco mais comuns.
        ["medical_restriction"] = ["lumbar_high_stress", "shoulder_high_stress", "knee_high_stress", "high_impact", "high_technical_complexity"],
    };

    private static readonly IReadOnlyDictionary<string, string[]> PainMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["neck"] = ["cervical_high_stress"],
        ["shoulder"] = ["shoulder_high_stress"],
        ["wrist"] = ["wrist_high_stress"],
        ["back"] = ["lumbar_high_stress"],
        ["lower_back"] = ["lumbar_high_stress"],
        ["knees"] = ["knee_high_stress"],
    };

    public static IReadOnlyCollection<string> TranslateLimitations(IEnumerable<string> onboardingValues) =>
        Translate(onboardingValues, LimitationMap);

    public static IReadOnlyCollection<string> TranslatePains(IEnumerable<string> onboardingValues) =>
        Translate(onboardingValues, PainMap);

    private static IReadOnlyCollection<string> Translate(IEnumerable<string> values, IReadOnlyDictionary<string, string[]> map)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            // valor desconhecido passa adiante sem traducao (nao apaga informacao, so nao expande).
            if (map.TryGetValue(value, out var catalogTags))
            {
                foreach (var tag in catalogTags) result.Add(tag);
            }
            else
            {
                result.Add(value);
            }
        }
        return result;
    }
}
```

Em `WorkoutGeneratorService.ParseProfile`, depois de montar `physicalLimitations`/`physicalPains` (linhas onde hoje faz `.Except(NoLimitationSentinels, ...)`/`.Except(NoPainSentinels, ...)`), aplicar:
```csharp
var translatedLimitations = OnboardingTagTranslator.TranslateLimitations(physicalLimitations).ToList();
var translatedPains = OnboardingTagTranslator.TranslatePains(physicalPains).ToList();
```
e usar `translatedLimitations`/`translatedPains` no `ParsedFitnessProfile` (em vez dos valores crus), que é o que acaba em `ExerciseSafetyContext.PhysicalLimitations`/`PhysicalPains`. **Importante:** o snapshot de auditoria (`AppliedFiltersJson` em `WorkoutGeneratorService.GenerateWorkoutJsonAsync`) deve continuar mostrando os valores JÁ TRADUZIDOS (é isso que o filtro realmente usou) — não precisa mudar nada ali, só o valor que entra já vem traduzido.

**Test scenarios:**
- `TranslateLimitations(["knee_problem"])` → contém `"knee_high_stress"`.
- `TranslatePains(["lower_back", "knees"])` → contém `"lumbar_high_stress"` e `"knee_high_stress"`.
- Valor desconhecido (`"xyz"`) passa adiante sem quebrar.
- Teste de ponta a ponta: `ParseProfile` com JSON contendo `physicalLimitations: ["knee_problem"]`, um exercício aprovado com `LimitationBlockTags: ["knee_high_stress"]` sem regressão elegível → `ExerciseSafetyFilter.Apply` remove o exercício (hoje isso NÃO acontece; depois da correção, deve acontecer).

- [ ] Escrever os 4 testes acima.
- [ ] Rodar, confirmar falha no último (prova que o bug existe hoje).
- [ ] Implementar `OnboardingTagTranslator` + integração no `ParseProfile`.
- [ ] Rodar de novo, confirmar os 4 verdes.
- [ ] Rodar `dotnet test backend/tests/Awaken.UnitTests` inteiro (síncrono), corrigir qualquer teste existente que dependia do comportamento antigo (ex.: testes de `WorkoutGeneratorService` que montavam contexto com tags do catálogo E do onboarding juntas artificialmente — ajustar pra só passar o valor de onboarding puro, já que agora a tradução é automática).

---

## Task R2 — US-041: instruções chegam na tela (Agente R2, Onda RA)

**Problema:** `InstructionsPtBr`/`TipsPtBr` são gerados mas se perdem entre o backend e a tela. Duas superfícies distintas precisam de fix:

### R2.1 — Pré-quest (dados vêm do `WorkoutJson` bruto)

**Files:**
- Modify: `backend/src/Awaken.Infrastructure/Services/WorkoutGeneratorService.cs` — no objeto serializado (`catalogWorkout.exercises`, onde já tem `instructions = exercise.InstructionsPtBr`), adicionar `tips = exercise.TipsPtBr`.
- Modify: `backend/src/Awaken.Application/Quests/Common/QuestResponseMapper.cs` — achar o record/classe que desserializa cada exercício do `WorkoutJson` (ex. `RawExercise`) e adicionar `Instructions`/`Tips` (List<string>), propagando pro DTO/response de preview.
- Modify: `backend/src/Awaken.Contracts/Quests/QuestResponse.cs` (ou onde estiver o DTO de exercício da preview, ex. `ExerciseDto`) — adicionar `Instructions`/`Tips`.
- Modify: `apps/mobile/lib/features/quests/data/dtos/quest_response_dto.dart` — parsear os 2 campos novos.
- Modify: `apps/mobile/lib/features/quests/domain/entities/quest_preview.dart` — `QuestPreviewExercise` ganha `instructions`/`tips` (`List<String>`).
- Modify: `apps/mobile/lib/features/quests/presentation/widgets/pre_quest_exercise_card.dart` — em `_showExerciseDetails`, exibir a lista numerada de `instructions` (em vez de/além de `description`) e, se `tips` não vazio, uma seção "Dicas".

### R2.2 — Quest execution (dados vêm do `QuestExercise` materializado, que NÃO guarda instruções — precisa resolver via `ExerciseCatalogProviderId` em tempo de consulta, mesmo padrão que já foi usado pra `gifUrl`/`ProviderExerciseId` na US-236)

**Files:**
- Modify: `backend/src/Awaken.Application/Quests/Queries/GetQuestExecution/GetQuestExecutionQueryHandler.cs` — já deve estar resolvendo `ExerciseCatalog` por `ProviderExerciseId` (feito na US-236 pra achar `gifUrl`); usar a MESMA resolução pra também pegar `InstructionsPtBr`/`TipsPtBr` do catálogo.
- Modify: `backend/src/Awaken.Contracts/Quests/QuestExecutionResponse.cs` — adicionar `Instructions`/`Tips` no DTO do exercício.
- Modify: `apps/mobile/lib/features/quests/data/dtos/quest_execution_response_dto.dart` — parsear.
- Modify: `apps/mobile/lib/features/quests/domain/entities/quest_execution.dart` — `QuestExecutionExercise` ganha `instructions`/`tips`.
- Modify: `apps/mobile/lib/features/quests/presentation/pages/exercise_execution_page.dart` — no `_showDemo`, exibir lista numerada de instruções + dicas.

**Test scenarios:**
- Backend: dado um `WorkoutJson` com `instructions: ["a","b"]` e `tips: ["c"]`, `QuestResponseMapper` deve produzir um DTO com esses 2 campos preenchidos (hoje ficam vazios/ausentes).
- Backend: `GetQuestExecutionQueryHandlerTests` — dado um `QuestExercise` cujo catálogo resolvido tem `InstructionsPtBr`/`TipsPtBr`, o response deve conter os dois.
- Flutter: widget test em `pre_quest_exercise_card` (ou arquivo de teste correspondente) — dado exercício com 2 instruções, ao abrir o diálogo, mostra as 2 linhas numeradas.
- Flutter: widget test em `exercise_execution_page_test.dart` — mesma verificação pro `_showDemo`.

- [ ] Escrever os 4 testes.
- [ ] Rodar, confirmar falha.
- [ ] Implementar R2.1 e R2.2.
- [ ] Rodar `dotnet test backend/tests/Awaken.UnitTests` + `flutter test`, confirmar tudo verde, corrigir regressões.

---

## Task R3 — US-143/144/148/149: pipeline real (tradução, sanitização, approve/reject) (Agente R3, Onda RA)

Este é o maior bloco. Faça em ordem (mesma arquivo/handler, então sequencial dentro do seu próprio trabalho, não precisa preocupar com outros agentes aqui pois R1/R2 não tocam nestes arquivos).

### R3.1 — Tradução real de nome/descrição/instruções/dicas (US-144)

**Files:**
- Create: `backend/src/Awaken.Domain/Services/Exercises/ExerciseTextTranslator.cs` — tradutor determinístico por substituição de palavras/frases (dicionário estático, sem chamada de API externa — mantém consistência com o resto do catálogo, que é 100% determinístico e sem IA em runtime).
- Modify: `backend/src/Awaken.Application/Exercises/Commands/ImportExercises/ImportExercisesCommandHandler.cs` — usar o tradutor pra preencher `NamePtBr`/`DescriptionPtBr`/`InstructionsPtBr`/`TipsPtBr` de verdade (mantendo `NameOriginal`/`InstructionsOriginal` como estão, cópia do inglês).
- Modify: `backend/src/Awaken.Domain/Entities/Exercises/ExerciseRawImport.cs` — chamar `MarkNormalized(utcNow)` quando a tradução cobrir 100% do texto (sem termos não mapeados); senão, seguir pra `pending_review` como hoje mas com uma flag de revisão (ver R3.2).

```csharp
namespace Awaken.Domain.Services.Exercises;

public static class ExerciseTextTranslator
{
    // Dicionario curado, ordenado por tamanho de frase decrescente (match de frase antes de palavra solta).
    private static readonly (string En, string Pt)[] PhraseDictionary =
    [
        ("assisted", "assistido"), ("barbell", "barra"), ("dumbbell", "halter"),
        ("bench press", "supino"), ("push-up", "flexao"), ("push up", "flexao"),
        ("pull-up", "barra fixa"), ("pull up", "barra fixa"), ("squat", "agachamento"),
        ("lunge", "avanco"), ("curl", "rosca"), ("row", "remada"), ("raise", "elevacao"),
        ("extension", "extensao"), ("flexion", "flexao"), ("seated", "sentado"),
        ("standing", "em pe"), ("incline", "inclinado"), ("decline", "declinado"),
        ("lying", "deitado"), ("kneeling", "ajoelhado"), ("cable", "cabo"),
        ("machine", "maquina"), ("smith", "smith"), ("kettlebell", "kettlebell"),
        ("band", "elastico"), ("chest", "peito"), ("back", "costas"), ("shoulder", "ombro"),
        ("bicep", "biceps"), ("tricep", "triceps"), ("leg", "perna"), ("calf", "panturrilha"),
        ("glute", "gluteo"), ("hip", "quadril"), ("knee", "joelho"), ("wide", "aberto"),
        ("narrow", "fechado"), ("close", "fechado"), ("grip", "pegada"), ("reverse", "reverso"),
        ("single", "unilateral"), ("alternating", "alternado"), ("with", "com"), ("and", "e"), ("the", ""),
    ];

    public static ExerciseTranslationResult Translate(string englishText)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            return new ExerciseTranslationResult(englishText, true);

        var remaining = englishText.ToLowerInvariant();
        var untranslatedWords = new List<string>();

        foreach (var (en, pt) in PhraseDictionary)
            remaining = System.Text.RegularExpressions.Regex.Replace(
                remaining, $@"\b{System.Text.RegularExpressions.Regex.Escape(en)}\b", pt,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var word in remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            // Uma palavra "sobrevive" sem traducao se nao bateu com nenhuma entrada do dicionario
            // E nao e uma palavra curta de conexao ja tratada. Heuristica: letras ASCII puras e
            // comprimento >= 3 sem estar no dicionario original em ingles = suspeita de nao-traduzida.
            if (word.Length >= 3 && System.Text.RegularExpressions.Regex.IsMatch(word, "^[a-z]+$") &&
                PhraseDictionary.All(p => !string.Equals(p.Pt, word, StringComparison.OrdinalIgnoreCase)))
            {
                untranslatedWords.Add(word);
            }
        }

        var capitalized = CapitalizeFirst(remaining.Trim());
        return new ExerciseTranslationResult(capitalized, untranslatedWords.Count == 0);
    }

    private static string CapitalizeFirst(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}

public record ExerciseTranslationResult(string TranslatedText, bool IsFullyTranslated);
```

**Nota para quem implementar:** este dicionário é deliberadamente pequeno e incompleto — o objetivo NÃO é tradução perfeita (isso exigiria um serviço de tradução real, fora de escopo aqui e inconsistente com "sem IA em runtime" do catálogo), é sair do estado atual (zero tradução, cópia literal do inglês) para um estado testável e auditável (tradução parcial determinística + marcação clara de quando não está completa). Sinta-se livre para expandir o dicionário com mais termos comuns do ExerciseDB se tiver tempo, mas não é obrigatório ir além do que está aqui.

**Test scenarios:**
- `Translate("Barbell Bench Press")` → `"Barra Supino"` (ou similar), `IsFullyTranslated=true`.
- `Translate("Zurricanguru Twist")` (palavra inventada) → `IsFullyTranslated=false`.
- `Translate("")` → retorna vazio, `IsFullyTranslated=true` (não é erro).

- [ ] Escrever os 3 testes + testes de integração no handler (nome/instruções realmente diferentes do original depois do import).
- [ ] Rodar, falhar, implementar, passar.

### R3.2 — Enum fechado pra equipamento/parte do corpo + sanitização real (US-144 RN-003/004, US-148)

**Files:**
- Create: `backend/src/Awaken.Domain/Entities/Training/EquipmentTypes.cs` — static class + `HashSet` (mesmo padrão de `MuscleGroups`/`MovementPatterns`, já existentes de US-237): `Barbell, Dumbbell, Cable, Machine, Bodyweight, Kettlebell, ResistanceBand, MedicineBall, StabilityBall, EzBarbell, SmithMachine, LeverageMachine, Assisted, Rope`.
- Modify: `backend/src/Awaken.Domain/Entities/Exercises/ExerciseCatalog.cs` — adicionar `public List<string> SanitizationIssues { get; private set; } = [];` (calculado, não é input externo) e um método `RecomputeSanitizationIssues()` chamado dentro de `Apply(...)`, que popula a lista com strings tipo `"equipment_unmapped"`, `"difficulty_out_of_range"`, `"missing_goal_tags"`, `"missing_primary_muscle"` conforme as regras da US-148 (seção 6 do doc: RN-001 a RN-008). **Estender** `CanBeApproved()` pra também exigir `SanitizationIssues.Count == 0` (além das checagens já existentes) — isso implementa RN-009 sem reestruturar as transições de status existentes (nenhum exercício com problema de sanitização consegue ser aprovado, que é o efeito prático que a regra pede).
- Modify: `ImportExercisesCommandHandler.cs` — passar a validar `equipment`/`bodyPart`/`target` contra `EquipmentTypes.IsValid`/`MuscleGroups.IsValid` ao montar o snapshot; valor fora do enum não interrompe o item (RN-002 do US-236 já estabelece esse princípio pro dataset enriquecido, mantenha consistência), só entra como está E fica registrado em `SanitizationIssues` via `RecomputeSanitizationIssues`.

**Test scenarios:**
- Exercício sem `RequiredEquipment` mapeado → `SanitizationIssues` contém `"equipment_unmapped"` → `CanBeApproved()` retorna `false` mesmo com instrução/mídia/atributo válidos.
- Exercício com `DifficultyRank` fora de 1-5 → issue correspondente.
- Exercício sem nenhum `GoalTags` → issue correspondente (hoje isso não acontece na prática pois `BuildGoalTags` sempre retorna `"maintenance"`, então este teste serve de regressão-guarda caso isso mude).
- Exercício totalmente válido → `SanitizationIssues` vazio, `CanBeApproved()` segue funcionando como antes (não regredir os testes existentes de aprovação).

- [ ] Escrever os testes.
- [ ] Rodar, falhar (exceto o último, que já deveria passar hoje — confirme que continua passando).
- [ ] Implementar.
- [ ] Rodar TODA a suíte de `ExerciseCatalogTests`/`ImportExercisesCommandHandlerTests`/testes de integração de exercício — corrigir qualquer fixture de teste que hoje monta um exercício "válido" mas que na verdade tinha uma issue de sanitização não percebida antes (é esperado que alguns testes precisem de um `RequiredEquipment`/`DifficultyRank` mais realista no setup).

### R3.3 — Approve/Reject de curador (US-149)

**Files:**
- Modify: `backend/src/Awaken.Domain/Entities/Exercises/ExerciseCatalog.cs` — adicionar `public string? RejectionReason { get; private set; }`, `public DateTime? ReviewedAtUtc { get; private set; }`, `public string? ReviewedBy { get; private set; }`, e método `Reject(string reason, string? reviewedBy, DateTime utcNow)` (seta `SanitizationStatus = "rejected"`, `IsApprovedForWorkoutGeneration = false`, grava motivo/revisor/data). `ApproveForWorkoutGeneration` ganha overload/parâmetro opcional `reviewedBy` pra também gravar quem aprovou manualmente (distinção de aprovação automática no import vs. manual do curador pode usar o mesmo método, só passando `reviewedBy` quando vier de um approve manual).
- Modify: `backend/src/Awaken.Domain/Entities/Exercises/ExerciseRawImport.cs` — usar os já existentes `Rejected`/`Deprecated` do enum `ExerciseRawImportStatus`: adicionar `MarkRejected(string reason, DateTime utcNow)` e `MarkDeprecated(DateTime utcNow)`.
- Create: `backend/src/Awaken.Application/Exercises/Commands/ApproveExercise/{ApproveExerciseCommand,ApproveExerciseCommandHandler,ApproveExerciseValidator}.cs`
- Create: `backend/src/Awaken.Application/Exercises/Commands/RejectExercise/{RejectExerciseCommand,RejectExerciseCommandHandler,RejectExerciseValidator}.cs`
- Modify: `backend/src/Awaken.Api/Controllers/V1/AdminExercisesController.cs` — `POST /api/admin/exercises/{id}/approve`, `POST /api/admin/exercises/{id}/reject` (body: `{ "reason": "..." }`).
- Create: `backend/src/Awaken.Contracts/Exercises/{ApproveExerciseRequest,RejectExerciseRequest}.cs` (reject precisa de `reason` obrigatório).

**Test scenarios:**
- `ApproveExerciseCommandHandler`: dado exercício `pending_review` com `CanBeApproved()==true`, quando executado, então `IsApprovedForWorkoutGeneration=true` e `ReviewedBy` gravado.
- `ApproveExerciseCommandHandler`: dado exercício com `SanitizationIssues` não vazio, quando executado, então lança exceção (não aprova, RN-001).
- `RejectExerciseCommandHandler`: dado qualquer exercício, quando executado com `reason="sem sentido"`, então `SanitizationStatus="rejected"`, `IsApprovedForWorkoutGeneration=false`, `RejectionReason` gravado.
- Endpoint: teste de integração (`AdminExercisesController`) — `POST /approve` sem role Admin → 403; com role Admin em item válido → 200.

- [ ] Escrever os testes.
- [ ] Rodar, falhar, implementar, passar.
- [ ] Rodar `dotnet test backend/tests/Awaken.UnitTests` inteiro (síncrono) — confirme que os 1501 testes anteriores continuam passando + os novos desta task.
- [ ] Rodar `dotnet build`, gerar migration se algum campo novo precisar de coluna (`SanitizationIssues`, `RejectionReason`, `ReviewedAtUtc`, `ReviewedBy` em `ExerciseCatalog`).

---

## Task R4 — Analytics de todo o EPIC-005 + verificação final (Agente R4, Onda RB — depois de R1/R2/R3 terminarem)

**Padrão real do projeto (confirmado na auditoria): não existe `IAnalyticsService`. "Analytics" = `logger.LogInformation("event_name key1={Value1} key2={Value2}", ...)`, no mesmo estilo de `daily_quest_generated` em `GenerateDailyQuestCommandHandler.cs`.** Use exatamente esse padrão, não invente uma abstração nova.

**Eventos a adicionar (nome do evento → onde disparar):**
| Evento | Arquivo/método |
|---|---|
| `exercise_import_started` | Início de `ImportExercisesCommandHandler.Handle` |
| `exercise_import_completed` | Fim de `ImportExercisesCommandHandler.Handle`, com contagens (rawImported/catalogCreated/catalogUpdated/failed) |
| `exercise_sanitized` | Quando `RecomputeSanitizationIssues()` roda e o resultado é "sem issues" (dentro do fluxo do handler, não dentro do método de domínio — domínio não deve logar) |
| `exercise_approved` | `ApproveExerciseCommandHandler` e no branch de auto-aprovação do import |
| `exercise_rejected` | `RejectExerciseCommandHandler` |
| `exercise_enriched_dataset_imported` | Se já existir um comando de import do dataset enriquecido (US-236) — confirme o arquivo e adicione lá; senão, no mesmo `ImportExercisesCommandHandler` quando `DatasetVersion` não for nulo |
| `exercise_relationship_imported` | Onde `ExerciseRelationship.Create` é chamado em lote — pode ser 1 log agregado com a contagem, não por item |
| `exercise_media_uploaded` | Onde `IMediaStorageService.UploadAsync` é chamado com sucesso |
| `training_split_map_seeded` / `training_split_map_validation_failed` | Isso é seed via migration, não roda em código C# de request — **pule este e o de baixo, documente como não aplicável** (seed de migration não tem logger de aplicação) |
| `program_day_resolved` | `GetResolvedProgramDayQueryHandler` / onde `DailyProgramDayResolver.Resolve` é chamado na geração de quest |
| `program_rotation_reset` | Mesmo local, quando `reason == "program_changed"` |
| `muscle_recovery_state_updated` | `CompleteQuestCommandHandler`, no wiring da US-239 |
| `recovery_plan_generated` | Onde `RecoveryPlanner`/`DailyWorkoutBlueprintBuilder` monta o plano |
| `overload_guard_applied` | Quando `volumeCapFactor &lt; 1.0` for aplicado a algum grupo no blueprint |
| `daily_blueprint_composed` / `daily_blueprint_fallback` / `daily_blueprint_rest_day` | `DailyWorkoutBlueprintBuilder`/`WorkoutGeneratorService`, conforme o resultado |

**Test scenarios:** não precisa testar o conteúdo exato da string de log (frágil); onde fizer sentido, use o padrão já existente no projeto pra testar log (se `GenerateDailyQuestCommandHandlerTests` já verifica que um log foi emitido, replique o mesmo estilo; senão, não invente um mecanismo de asserção de log novo — só garanta que a chamada existe e não quebra nada).

- [ ] Adicionar os logs conforme a tabela acima (pule os 2 marcados como não aplicável, documente por quê).
- [ ] Rodar `dotnet build` limpo.
- [ ] Rodar `dotnet test backend/tests/Awaken.UnitTests` completo, síncrono — confirmar 0 falhas (baseline esperado: 1501+ testes das rodadas anteriores + os novos de R1/R2/R3).
- [ ] Rodar `dotnet test backend/tests/Awaken.ArchitectureTests`.
- [ ] Rodar a suíte de integração completa UMA VEZ, de forma síncrona (você é o único agente rodando nesta onda, sem contenção de Docker) — reporte resultado real. Se houver falhas em áreas fora do EPIC-005 (Notification/Shop/BattleLog, já flagradas como pré-existentes numa auditoria anterior), não precisa corrigir, só confirmar que ainda são as mesmas 5 e não aumentaram.
- [ ] `flutter analyze` + `flutter test` completo.
- [ ] `dotnet ef database update` local — confirmar que toda migration nova desta rodada de remediação aplica sem conflito.
- [ ] Não commitar.

Ao final, dê um relatório único cobrindo R1+R2+R3+R4: o que foi corrigido, números reais de teste de TODAS as suítes, e qualquer gap que ainda reste (ex.: se o dicionário de tradução da R3.1 ficou claramente incompleto pra uso real, diga isso sem rodeio).
