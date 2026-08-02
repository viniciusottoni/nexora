using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Exercises;

/// <summary>
/// US-236 — taxonomia biomecânica do exercício, extraída de <see cref="ExerciseCatalog"/> para uma
/// tabela própria 1:1 (RN-003). O dado mora só aqui; <see cref="ExerciseCatalog"/> expõe getters
/// delegados com o mesmo nome (ex.: <c>MovementPattern</c>) para não quebrar consumidores existentes
/// (scoring engine, selection engine, mapper).
/// </summary>
public class ExerciseTaxonomy : BaseEntity
{
    public Guid ExerciseCatalogId { get; private set; }
    public string MovementFamily { get; private set; } = string.Empty;
    public string MovementPattern { get; private set; } = string.Empty;
    public string Mechanic { get; private set; } = string.Empty;
    public string ForceType { get; private set; } = string.Empty;
    public string PlaneOfMotion { get; private set; } = string.Empty;
    public string Laterality { get; private set; } = string.Empty;
    public string BodyPosition { get; private set; } = string.Empty;
    public string? BenchAngle { get; private set; }
    public string EquipmentCategory { get; private set; } = string.Empty;
    public string LoadType { get; private set; } = string.Empty;
    public string PrimaryRegion { get; private set; } = string.Empty;
    public bool IsCompound { get; private set; }
    public bool IsUnilateral { get; private set; }
    public bool IsAssisted { get; private set; }
    public bool IsWeighted { get; private set; }
    public List<string> Signals { get; private set; } = [];
    public string Confidence { get; private set; } = "medium";

    private ExerciseTaxonomy() { }

    public static ExerciseTaxonomy Create(Guid exerciseCatalogId, ExerciseTaxonomySnapshot snapshot)
    {
        var taxonomy = new ExerciseTaxonomy { ExerciseCatalogId = exerciseCatalogId };
        taxonomy.Apply(snapshot, taxonomy.CreatedAtUtc);
        return taxonomy;
    }

    public void Apply(ExerciseTaxonomySnapshot snapshot, DateTime utcNow)
    {
        MovementFamily = snapshot.MovementFamily;
        MovementPattern = snapshot.MovementPattern;
        Mechanic = snapshot.Mechanic;
        ForceType = snapshot.ForceType;
        PlaneOfMotion = snapshot.PlaneOfMotion;
        Laterality = snapshot.Laterality;
        BodyPosition = snapshot.BodyPosition;
        BenchAngle = snapshot.BenchAngle;
        EquipmentCategory = snapshot.EquipmentCategory;
        LoadType = snapshot.LoadType;
        PrimaryRegion = snapshot.PrimaryRegion;
        IsCompound = snapshot.IsCompound;
        IsUnilateral = snapshot.IsUnilateral;
        IsAssisted = snapshot.IsAssisted;
        IsWeighted = snapshot.IsWeighted;
        Signals = snapshot.Signals;
        Confidence = snapshot.Confidence;
        UpdatedAtUtc = utcNow;
    }
}

public record ExerciseTaxonomySnapshot(
    string MovementFamily,
    string MovementPattern,
    string Mechanic,
    string ForceType,
    string PlaneOfMotion,
    string Laterality,
    string BodyPosition,
    string? BenchAngle,
    string EquipmentCategory,
    string LoadType,
    string PrimaryRegion,
    bool IsCompound,
    bool IsUnilateral,
    bool IsAssisted,
    bool IsWeighted,
    List<string> Signals,
    string Confidence);
