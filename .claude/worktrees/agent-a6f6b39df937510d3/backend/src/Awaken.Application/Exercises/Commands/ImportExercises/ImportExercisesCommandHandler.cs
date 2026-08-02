using System.Globalization;
using System.Text;
using System.Text.Json;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Exercises;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Exercises;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Exercises.Commands.ImportExercises;

public class ImportExercisesCommandHandler(
    IExerciseRawImportRepository rawImportRepository,
    IExerciseCatalogRepository catalogRepository,
    IUnitOfWork unitOfWork,
    IDateTimeService dateTimeService,
    ISafeDirectoryResolver safeDirectoryResolver,
    IExerciseCatalogCacheService exerciseCatalogCache,
    IMediaStorageService mediaStorageService,
    IConfiguration configuration,
    ILogger<ImportExercisesCommandHandler> logger) : IRequestHandler<ImportExercisesCommand, ImportExercisesResponse>
{
    public async Task<ImportExercisesResponse> Handle(ImportExercisesCommand request, CancellationToken cancellationToken)
    {
        var sourceDirectory = safeDirectoryResolver.Resolve(request.BatchKey);
        if (sourceDirectory is null)
            throw new ValidationException(
                [new ValidationFailure("BatchKey", "Chave de lote inválida ou diretório raiz não configurado.")]);

        if (!Directory.Exists(sourceDirectory))
            throw new DirectoryNotFoundException("Lote não encontrado.");

        var utcNow = dateTimeService.UtcNow;
        var importBatchId = $"imp_{utcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

        // R4 (analytics): inicio do import, antes de qualquer arquivo ser processado.
        logger.LogInformation(
            "exercise_import_started importBatchId={ImportBatchId} provider={Provider} batchKey={BatchKey} datasetVersion={DatasetVersion}",
            importBatchId, request.Provider, request.BatchKey, request.DatasetVersion);

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(sourceDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        if (request.MaxFiles is > 0)
            jsonFiles = jsonFiles.Take(request.MaxFiles.Value);

        var rawImported = 0;
        var catalogCreated = 0;
        var catalogUpdated = 0;
        var failed = 0;
        var mediaUploaded = 0;
        var taxonomyApplied = 0;
        var relationshipsCreated = 0;
        var pending = 0;
        var isEnrichedDataset = !string.IsNullOrWhiteSpace(request.DatasetVersion);
        var mediaLicenseVerified = !isEnrichedDataset || IsSignedEulaAvailable(configuration);
        var seenProviderExerciseIds = new HashSet<string>(StringComparer.Ordinal);
        var persistedRawImportIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var jsonFile in jsonFiles)
        {
            var rawJson = await File.ReadAllTextAsync(jsonFile, cancellationToken);
            var providerExerciseId = Path.GetFileNameWithoutExtension(jsonFile);
            ExerciseRawImport? rawImport = null;

            try
            {
                using var document = JsonDocument.Parse(rawJson);
                providerExerciseId = RequiredString(document.RootElement, "id");

                if (!seenProviderExerciseIds.Add(providerExerciseId))
                    continue;

                rawImport = await rawImportRepository.GetByProviderExerciseIdAsync(
                    request.Provider, providerExerciseId, cancellationToken);
                var isNewRawImport = rawImport is null;

                if (rawImport is null)
                {
                    rawImport = ExerciseRawImport.CreateImported(
                        request.Provider,
                        providerExerciseId,
                        GetOptionalString(document.RootElement, "version"),
                        rawJson,
                        importBatchId,
                        sourceUrl: null,
                        sourceFilePath: $"{request.BatchKey}/{Path.GetFileName(jsonFile)}",
                        mediaBaseUrl: null,
                        mediaLicenseInfo: GetOptionalString(document.RootElement, "license"),
                        utcNow,
                        request.DatasetVersion);
                }

                var catalog = await catalogRepository.GetByProviderExerciseIdAsync(
                    request.Provider, providerExerciseId, cancellationToken);

                if (isNewRawImport)
                {
                    await rawImportRepository.AddAsync(rawImport, cancellationToken);
                    rawImported++;
                    persistedRawImportIds.Add(providerExerciseId);
                }
                else
                {
                    rawImport.RefreshImportedSnapshot(
                        rawJson,
                        importBatchId,
                        $"{request.BatchKey}/{Path.GetFileName(jsonFile)}",
                        GetOptionalString(document.RootElement, "license"),
                        request.DatasetVersion,
                        utcNow);
                    persistedRawImportIds.Add(providerExerciseId);
                }

                if (isEnrichedDataset && catalog is null)
                {
                    rawImport.MarkPendingReview(utcNow);
                    pending++;
                    continue;
                }

                var mediaPath = mediaLicenseVerified
                    ? FindMediaFile(sourceDirectory, providerExerciseId)
                    : null;
                var gifUrl = await TryUploadMediaAsync(mediaPath, providerExerciseId, cancellationToken);
                if (gifUrl is not null)
                {
                    mediaUploaded++;
                    rawImport.SetMediaBaseUrl(gifUrl, utcNow);
                }

                var (snapshot, isFullyTranslated) = BuildCatalogSnapshot(
                    document.RootElement,
                    request.Provider,
                    rawImport.Id,
                    gifUrl,
                    allowInlineGifUrl: !isEnrichedDataset);
                var contributionSeed = BuildAttributeContribution(snapshot);
                var relations = await BuildRelationsAsync(
                    document.RootElement,
                    request.Provider,
                    request.DatasetVersion,
                    cancellationToken);
                relationshipsCreated += relations.Count;

                if (catalog is null)
                {
                    catalog = ExerciseCatalog.Create(snapshot, utcNow);
                    catalog.SetAttributeContribution(contributionSeed, utcNow);
                    catalog.ReplaceRelations(relations, utcNow);

                    // R4 (analytics): US-148 - so dispara quando a sanitizacao (recomputada dentro de
                    // ExerciseCatalog.Apply) nao encontrou nenhuma pendencia para este item.
                    if (catalog.SanitizationIssues.Count == 0)
                    {
                        logger.LogInformation(
                            "exercise_sanitized providerExerciseId={ProviderExerciseId} importBatchId={ImportBatchId}",
                            providerExerciseId, importBatchId);
                    }

                    if (request.ApproveOnImport && catalog.CanBeApproved())
                    {
                        catalog.ApproveForWorkoutGeneration(utcNow);
                        rawImport.MarkApproved(utcNow);
                        logger.LogInformation(
                            "exercise_approved providerExerciseId={ProviderExerciseId} source=auto_import importBatchId={ImportBatchId}",
                            providerExerciseId, importBatchId);
                    }
                    else if (isFullyTranslated)
                    {
                        // R3.1 (US-144): so alcanca "normalized" quando nome/descricao/instrucoes/dicas
                        // foram 100% traduzidos pelo ExerciseTextTranslator (sem termos fora do dicionario).
                        rawImport.MarkNormalized(utcNow);
                    }
                    else
                    {
                        rawImport.MarkPendingReview(utcNow);
                    }

                    await catalogRepository.AddAsync(catalog, cancellationToken);
                    catalogCreated++;
                }
                else
                {
                    var taxonomyWasApplied = isEnrichedDataset
                        ? catalog.ApplyEnrichment(snapshot, utcNow)
                        : ApplyCatalogSnapshot(catalog, snapshot, utcNow);
                    if (taxonomyWasApplied)
                        taxonomyApplied++;
                    else
                        pending++;

                    // R4 (analytics): so o caminho nao-enriquecido chama catalog.Apply, que e o
                    // unico que recomputa SanitizationIssues (ApplyEnrichment nao mexe nela).
                    if (!isEnrichedDataset && catalog.SanitizationIssues.Count == 0)
                    {
                        logger.LogInformation(
                            "exercise_sanitized providerExerciseId={ProviderExerciseId} importBatchId={ImportBatchId}",
                            providerExerciseId, importBatchId);
                    }

                    if (!isEnrichedDataset && catalog.AttributeContribution is null)
                    {
                        catalog.SetAttributeContribution(contributionSeed, utcNow);
                    }
                    else if (!isEnrichedDataset)
                    {
                        catalog.AttributeContribution!.ApplyAutoGenerated(
                            contributionSeed.PrimaryAttribute,
                            contributionSeed.StrengthXp,
                            contributionSeed.AgilityXp,
                            contributionSeed.EnduranceXp,
                            contributionSeed.VitalityXp,
                            contributionSeed.FocusXp,
                            contributionSeed.WisdomXp,
                            utcNow);
                    }

                    catalog.ReplaceRelations(relations, utcNow);

                    if (request.ApproveOnImport && catalog.CanBeApproved())
                    {
                        catalog.ApproveForWorkoutGeneration(utcNow);
                        rawImport.MarkApproved(utcNow);
                        logger.LogInformation(
                            "exercise_approved providerExerciseId={ProviderExerciseId} source=auto_import importBatchId={ImportBatchId}",
                            providerExerciseId, importBatchId);
                    }
                    else
                    {
                        catalog.MarkPendingReview(utcNow);

                        // R3.1 (US-144): mesma regra da criacao — normalized so quando a traducao
                        // cobre 100% do texto; senao segue para pending_review como antes.
                        if (isFullyTranslated)
                            rawImport.MarkNormalized(utcNow);
                        else
                            rawImport.MarkPendingReview(utcNow);
                    }

                    catalogRepository.Update(catalog);
                    catalogUpdated++;
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                var alreadyPersisted = persistedRawImportIds.Contains(providerExerciseId) ||
                                       await rawImportRepository.ExistsByProviderExerciseIdAsync(
                                           request.Provider,
                                           providerExerciseId,
                                           cancellationToken);

                if (alreadyPersisted && rawImport is not null)
                {
                    rawImport.MarkFailed(ex.Message, utcNow);
                    rawImportRepository.Update(rawImport);
                }
                else if (!alreadyPersisted)
                {
                    await rawImportRepository.AddAsync(
                        ExerciseRawImport.CreateFailed(
                            request.Provider,
                            providerExerciseId,
                            importBatchId,
                            ex.Message,
                            utcNow,
                            rawJson,
                            sourceUrl: null,
                            sourceFilePath: $"{request.BatchKey}/{Path.GetFileName(jsonFile)}",
                            datasetVersion: request.DatasetVersion),
                        cancellationToken);
                    persistedRawImportIds.Add(providerExerciseId);
                }

                failed++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // US-206: invalidate approved catalog cache so next workout generation uses fresh data.
        await exerciseCatalogCache.InvalidateApprovedCatalogAsync(cancellationToken);

        // R4 (analytics): fim do import, com as contagens agregadas do lote inteiro.
        logger.LogInformation(
            "exercise_import_completed importBatchId={ImportBatchId} rawImported={RawImported} catalogCreated={CatalogCreated} catalogUpdated={CatalogUpdated} failed={Failed}",
            importBatchId, rawImported, catalogCreated, catalogUpdated, failed);

        if (isEnrichedDataset)
        {
            logger.LogInformation(
                "exercise_enriched_dataset_imported importBatchId={ImportBatchId} datasetVersion={DatasetVersion} taxonomyApplied={TaxonomyApplied} pending={Pending}",
                importBatchId, request.DatasetVersion, taxonomyApplied, pending);
        }

        if (relationshipsCreated > 0)
        {
            // R4 (analytics): 1 log agregado por lote - ExerciseRelationship.Create roda em lote
            // dentro de BuildRelationsAsync, nao faz sentido logar por item (ver plano R4).
            logger.LogInformation(
                "exercise_relationship_imported importBatchId={ImportBatchId} relationshipsCreated={RelationshipsCreated}",
                importBatchId, relationshipsCreated);
        }

        return new ImportExercisesResponse(
            importBatchId,
            rawImported,
            catalogCreated,
            catalogUpdated,
            failed,
            mediaUploaded,
            taxonomyApplied,
            relationshipsCreated,
            pending);
    }

    private static bool ApplyCatalogSnapshot(
        ExerciseCatalog catalog,
        ExerciseCatalogSnapshot snapshot,
        DateTime utcNow)
    {
        catalog.Apply(snapshot, utcNow);
        return true;
    }

    private static bool IsSignedEulaAvailable(IConfiguration configuration)
    {
        var path = configuration["ExerciseImport:SignedEulaPath"];
        return !string.IsNullOrWhiteSpace(path)
            && string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase)
            && File.Exists(path);
    }

    /// <summary>
    /// US-236 — publica o GIF 360 local no storage de mídia (bucket S3-compatível) e retorna a URL pública.
    /// Falha de upload (rede, credenciais, storage indisponível) não derruba o item do lote — RN de
    /// US-236 §9.2 só bloqueia a *aprovação*, não o import: o item segue como "mídia pendente" e pode
    /// ser reprocessado depois.
    /// </summary>
    private async Task<string?> TryUploadMediaAsync(
        string? mediaPath,
        string providerExerciseId,
        CancellationToken cancellationToken)
    {
        if (mediaPath is null)
            return null;

        try
        {
            await using var mediaStream = File.OpenRead(mediaPath);
            var url = await mediaStorageService.UploadAsync(
                $"exercises/{providerExerciseId}/360.gif",
                mediaStream,
                "image/gif",
                cancellationToken);

            logger.LogInformation(
                "exercise_media_uploaded providerExerciseId={ProviderExerciseId}",
                providerExerciseId);

            return url;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static (ExerciseCatalogSnapshot Snapshot, bool IsFullyTranslated) BuildCatalogSnapshot(
        JsonElement root,
        string providerName,
        Guid rawImportId,
        string? gifUrl,
        bool allowInlineGifUrl)
    {
        root.TryGetProperty("taxonomy", out var taxonomy);

        var id = RequiredString(root, "id");
        var name = RequiredString(root, "name");
        var bodyPart = NormalizeToken(GetOptionalString(root, "bodyPart"));
        var equipment = NormalizeToken(GetOptionalString(root, "equipment"));
        var target = NormalizeToken(GetOptionalString(root, "target"));
        var difficulty = NormalizeToken(GetOptionalString(root, "difficulty")) is { Length: > 0 } normalizedDifficulty
            ? normalizedDifficulty
            : "beginner";
        var category = NormalizeToken(GetOptionalString(root, "category"));
        var movementPattern = NormalizeToken(GetOptionalString(taxonomy, "movementPattern"));
        var movementFamily = NormalizeToken(GetOptionalString(taxonomy, "movementFamily"));
        var equipmentCategory = NormalizeToken(GetOptionalString(taxonomy, "equipmentCategory"));
        var loadType = NormalizeToken(GetOptionalString(taxonomy, "loadType"));
        var primaryRegion = NormalizeToken(GetOptionalString(taxonomy, "primaryRegion"));
        var difficultyRank = DifficultyRank(difficulty);
        var isWeighted = GetOptionalBool(taxonomy, "isWeighted");
        var isCompound = GetOptionalBool(taxonomy, "isCompound");
        var requiredEquipment = equipment is "body_weight" or "bodyweight" or "none"
            ? new List<string> { "bodyweight" }
            : NonEmptyList(equipment);
        var secondaryMuscles = GetStringList(root, "secondaryMuscles").Select(NormalizeToken).WhereNotEmpty().ToList();
        var instructions = GetStringList(root, "instructions");
        var relationIds = CollectRankedRelationIds(root, "similarExercises", "substitutions");
        var progressionIds = CollectRankedRelationIds(root, "progressions");
        var regressionIds = CollectRankedRelationIds(root, "regressions");
        var riskTags = BuildRiskTags(movementPattern, equipment, equipmentCategory, movementFamily).ToList();
        var accessibilityTags = BuildAccessibilityTags(difficulty, requiredEquipment, equipmentCategory).ToList();
        var jointStressTags = BuildJointStressTags(movementPattern, target, bodyPart).ToList();

        // R3.1 (US-144): traducao determinística de nome/descricao/instrucoes/dicas via
        // ExerciseTextTranslator (dicionario estatico, sem chamada de API). NameOriginal/
        // InstructionsOriginal continuam preservando o texto em ingles tal como veio do provider.
        var descriptionOriginal = GetOptionalString(root, "description");
        var tipsOriginal = GetStringList(root, "tips");
        var nameTranslation = ExerciseTextTranslator.Translate(name);
        var descriptionTranslation = descriptionOriginal is null ? null : ExerciseTextTranslator.Translate(descriptionOriginal);
        var instructionTranslations = instructions.Select(ExerciseTextTranslator.Translate).ToList();
        var tipsTranslations = tipsOriginal.Select(ExerciseTextTranslator.Translate).ToList();
        var isFullyTranslated = nameTranslation.IsFullyTranslated
            && (descriptionTranslation is null || descriptionTranslation.IsFullyTranslated)
            && instructionTranslations.All(translation => translation.IsFullyTranslated)
            && tipsTranslations.All(translation => translation.IsFullyTranslated);

        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: rawImportId,
            ProviderName: providerName,
            ProviderExerciseId: id,
            ProviderVersion: GetOptionalString(root, "version"),
            NamePtBr: nameTranslation.TranslatedText,
            NameOriginal: name,
            Slug: Slugify(name),
            DescriptionPtBr: descriptionTranslation?.TranslatedText ?? descriptionOriginal,
            InstructionsPtBr: instructionTranslations.Select(translation => translation.TranslatedText).ToList(),
            InstructionsOriginal: instructions,
            TipsPtBr: tipsTranslations.Select(translation => translation.TranslatedText).ToList(),
            ExerciseType: category,
            MovementPattern: movementPattern,
            MovementFamily: movementFamily,
            Mechanic: NormalizeToken(GetOptionalString(taxonomy, "mechanic")),
            ForceType: NormalizeToken(GetOptionalString(taxonomy, "forceType")),
            PlaneOfMotion: NormalizeToken(GetOptionalString(taxonomy, "planeOfMotion")),
            Laterality: NormalizeToken(GetOptionalString(taxonomy, "laterality")),
            BodyPosition: NormalizeToken(GetOptionalString(taxonomy, "bodyPosition")),
            BenchAngle: NormalizeTokenOrNull(GetOptionalString(taxonomy, "benchAngle")),
            EquipmentCategory: equipmentCategory,
            LoadType: loadType,
            PrimaryRegion: primaryRegion,
            DifficultyLevel: difficulty,
            DifficultyRank: difficultyRank,
            TechnicalComplexity: TechnicalComplexity(difficultyRank, isCompound, isWeighted),
            ImpactLevel: ImpactLevel(movementPattern, equipmentCategory, isWeighted),
            Environment: ResolveEnvironment(requiredEquipment, equipmentCategory),
            RequiredEquipment: requiredEquipment,
            PrimaryMuscleGroups: NonEmptyList(target),
            SecondaryMuscleGroups: secondaryMuscles,
            BodyParts: NonEmptyList(bodyPart),
            JointStressTags: jointStressTags,
            ContraindicationTags: riskTags,
            LimitationBlockTags: jointStressTags.Concat(riskTags).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            PainBlockTags: jointStressTags,
            GoalTags: BuildGoalTags(category, movementPattern, isWeighted).ToList(),
            RiskTags: riskTags,
            AccessibilityTags: accessibilityTags,
            TaxonomySignals: GetStringList(taxonomy, "signals").Select(NormalizeToken).WhereNotEmpty().ToList(),
            MinExperienceLevel: difficulty,
            SuitableForSedentary: difficultyRank <= 1 && accessibilityTags.Contains("sedentary_safe"),
            SuitableForBeginner: difficultyRank <= 2,
            SuitableForIntermediate: difficultyRank <= 3,
            SuitableForAdvanced: true,
            IsCompound: isCompound,
            IsUnilateral: GetOptionalBool(taxonomy, "isUnilateral"),
            IsAssisted: GetOptionalBool(taxonomy, "isAssisted"),
            IsWeighted: isWeighted,
            RegressionExerciseIds: regressionIds,
            ProgressionExerciseIds: progressionIds,
            RelatedExerciseIds: relationIds,
            VideoUrl: GetOptionalString(root, "videoUrl"),
            ImageUrl: GetOptionalString(root, "imageUrl"),
            GifUrl: gifUrl ?? (allowInlineGifUrl ? GetOptionalString(root, "gifUrl") : null),
            MediaLicenseInfo: GetOptionalString(root, "license"),
            SanitizationStatus: "pending_review",
            IsApprovedForWorkoutGeneration: false,
            Confidence: NormalizeToken(GetOptionalString(taxonomy, "confidence")) is { Length: > 0 } confidence ? confidence : "medium");

        return (snapshot, isFullyTranslated);
    }

    private static ExerciseAttributeContribution BuildAttributeContribution(ExerciseCatalogSnapshot snapshot)
    {
        var strength = 0;
        var agility = 0;
        var endurance = 0;
        var vitality = 0;
        var focus = 0;
        const int wisdom = 1;
        var primary = "strength";

        if (snapshot.ExerciseType == "cardio" || snapshot.MovementPattern is "locomotion" or "jump")
        {
            primary = "endurance";
            endurance = snapshot.DifficultyRank >= 3 ? 3 : 2;
            vitality = 1;
            agility = snapshot.MovementPattern == "jump" ? 1 : 0;
        }
        else if (snapshot.MovementPattern is "mobility" or "balance")
        {
            primary = snapshot.MovementPattern == "balance" ? "agility" : "vitality";
            agility = snapshot.MovementPattern == "balance" ? 2 : 0;
            vitality = snapshot.MovementPattern == "mobility" ? 2 : 1;
            focus = 1;
        }
        else if (snapshot.MovementPattern.StartsWith("core_", StringComparison.Ordinal))
        {
            primary = "vitality";
            vitality = 2;
            strength = 1;
            focus = 1;
        }
        else
        {
            primary = "strength";
            strength = snapshot.IsWeighted || snapshot.ExerciseType == "strength" ? Math.Min(4, snapshot.DifficultyRank) : 2;
            focus = snapshot.TechnicalComplexity >= 3 ? 1 : 0;
            endurance = snapshot.RequiredEquipment.Contains("bodyweight") ? 1 : 0;
        }

        return ExerciseAttributeContribution.CreateAutoGenerated(
            primary,
            strength,
            agility,
            endurance,
            vitality,
            focus,
            wisdom);
    }

    private async Task<List<ExerciseRelationship>> BuildRelationsAsync(
        JsonElement root,
        string provider,
        string? datasetVersion,
        CancellationToken cancellationToken)
    {
        var relations = new List<ExerciseRelationship>();
        foreach (var definition in new[]
                 {
                     ("similarExercises", "similar"),
                     ("substitutions", "substitution"),
                     ("progressions", "progression"),
                     ("regressions", "regression"),
                 })
        {
            foreach (var candidate in ReadRelationCandidates(root, definition.Item1))
            {
                var target = await catalogRepository.GetByProviderExerciseIdAsync(
                    provider,
                    candidate.Id,
                    cancellationToken);
                relations.Add(ExerciseRelationship.Create(
                    candidate.Id,
                    candidate.Name,
                    definition.Item2,
                    candidate.Types,
                    candidate.Score,
                    candidate.Confidence,
                    candidate.Reasons,
                    datasetVersion,
                    target?.Id));
            }
        }

        return relations;
    }

    private static IEnumerable<RelationCandidate> ReadRelationCandidates(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var collection) || collection.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var item in collection.EnumerateArray())
        {
            var id = GetOptionalString(item, "id");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            yield return new RelationCandidate(
                id,
                GetOptionalString(item, "name") ?? string.Empty,
                GetStringList(item, "types").Select(NormalizeToken).ToList(),
                GetOptionalDecimal(item, "score"),
                NormalizeToken(GetOptionalString(item, "confidence")) is { Length: > 0 } confidence ? confidence : "medium",
                GetStringList(item, "reasons"));
        }
    }

    private static string? FindMediaFile(string sourceDirectory, string providerExerciseId)
    {
        var preferred = Path.Combine(sourceDirectory, $"{providerExerciseId}-360.gif");
        if (File.Exists(preferred))
            return Path.GetFullPath(preferred);

        return Directory.EnumerateFiles(sourceDirectory, $"{providerExerciseId}*.*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(Path.GetExtension(path), ".gif", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(Path.GetFullPath)
            .FirstOrDefault();
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        var value = GetOptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Exercise JSON is missing required property '{propertyName}'.");

        return value;
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
            return null;

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static bool GetOptionalBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined ||
            !element.TryGetProperty(propertyName, out var property))
            return false;

        return property.ValueKind == JsonValueKind.True ||
               property.ValueKind == JsonValueKind.String &&
               bool.TryParse(property.GetString(), out var value) &&
               value;
    }

    private static decimal GetOptionalDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return 0;

        return property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var value) ? value : 0;
    }

    private static List<string> GetStringList(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
            return [];

        return property.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private static List<string> CollectRankedRelationIds(JsonElement root, params string[] propertyNames)
    {
        var candidates = new List<RelationCandidate>();
        foreach (var propertyName in propertyNames)
            candidates.AddRange(ReadRelationCandidates(root, propertyName));

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => ConfidenceRank(candidate.Confidence))
            .Select(candidate => candidate.Id)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static int ConfidenceRank(string confidence) => confidence switch
    {
        "high" => 3,
        "medium" => 2,
        "low" => 1,
        _ => 0,
    };

    private static List<string> NonEmptyList(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [value];

    private static string? NormalizeTokenOrNull(string? value)
    {
        var normalized = NormalizeToken(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Slugify(value).Replace('-', '_');
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousDash = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static int DifficultyRank(string difficulty) => difficulty switch
    {
        "sedentary" => 0,
        "beginner" => 1,
        "intermediate" => 3,
        "advanced" => 5,
        _ => 2
    };

    private static int TechnicalComplexity(int difficultyRank, bool isCompound, bool isWeighted)
    {
        var value = Math.Max(1, difficultyRank);
        if (isCompound)
            value++;
        if (isWeighted)
            value++;
        return Math.Clamp(value, 1, 5);
    }

    private static int ImpactLevel(string movementPattern, string equipmentCategory, bool isWeighted)
    {
        if (movementPattern is "jump" or "locomotion")
            return 4;

        if (isWeighted || equipmentCategory == "free_weight")
            return 2;

        return 1;
    }

    private static string ResolveEnvironment(IEnumerable<string> equipment, string equipmentCategory)
    {
        if (equipment.Contains("bodyweight") || equipmentCategory == "bodyweight")
            return "home";

        if (equipmentCategory is "free_weight" or "machine")
            return "gym";

        return "flexible";
    }

    private static IEnumerable<string> BuildGoalTags(string category, string movementPattern, bool isWeighted)
    {
        if (category == "strength" || isWeighted)
        {
            yield return "strength";
            yield return "hypertrophy";
        }

        if (category == "cardio" || movementPattern is "locomotion" or "jump")
        {
            yield return "conditioning";
            yield return "fat_loss";
        }

        yield return "maintenance";
    }

    private static IEnumerable<string> BuildRiskTags(
        string movementPattern,
        string equipment,
        string equipmentCategory,
        string movementFamily)
    {
        if (equipmentCategory == "free_weight" || equipment is "barbell" or "dumbbell")
            yield return "requires_load_control";

        if (equipment == "barbell" && movementFamily.Contains("bench_press", StringComparison.Ordinal))
            yield return "requires_spotter";

        if (movementPattern is "jump" or "locomotion")
            yield return "high_impact";
    }

    private static IEnumerable<string> BuildAccessibilityTags(
        string difficulty,
        IEnumerable<string> equipment,
        string equipmentCategory)
    {
        if (difficulty == "beginner")
            yield return "beginner_safe";

        if (difficulty is "sedentary" or "beginner")
            yield return "sedentary_safe";

        if (equipment.Contains("bodyweight") || equipmentCategory == "bodyweight")
            yield return "no_equipment";

        yield return "low_impact";
    }

    private static IEnumerable<string> BuildJointStressTags(string movementPattern, string target, string bodyPart)
    {
        if (movementPattern.Contains("push", StringComparison.Ordinal) ||
            target.Contains("delts", StringComparison.Ordinal) ||
            bodyPart == "chest")
            yield return "shoulder_high_stress";

        if (movementPattern.Contains("squat", StringComparison.Ordinal) ||
            movementPattern.Contains("lunge", StringComparison.Ordinal))
            yield return "knee_high_stress";

        if (movementPattern.Contains("hinge", StringComparison.Ordinal))
            yield return "lumbar_high_stress";

        if (movementPattern.Contains("push", StringComparison.Ordinal))
            yield return "wrist_high_stress";
    }
}

internal record RelationCandidate(
    string Id,
    string Name,
    List<string> Types,
    decimal Score,
    string Confidence,
    List<string> Reasons);

internal static class ExerciseImportEnumerableExtensions
{
    public static IEnumerable<string> WhereNotEmpty(this IEnumerable<string> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value));
}
