namespace Awaken.Contracts.Admin.Dashboard;

/// <summary>
/// US-161 RN-005: wrapper genérico para blocos independentes do dashboard.
/// Permite que a falha de uma fonte de métrica não derrube a resposta inteira —
/// o bloco com falha retorna HasError=true e Data padrão/vazio.
/// </summary>
public record DashboardMetricBlock<T>(T? Data, bool HasError);
