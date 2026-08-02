namespace Awaken.Contracts.Admin.Routines;

/// <summary>US-221: tamanho de fila acumulada por tipo de carga (RN-001 — fila acumulada deve ficar visível).</summary>
public record QueueStatusResponse(
    string QueueName,
    long EnqueuedCount,
    long FetchedCount,
    string Status);
