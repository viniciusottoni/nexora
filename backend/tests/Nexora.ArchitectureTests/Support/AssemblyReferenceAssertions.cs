using System.Reflection;

namespace Nexora.ArchitectureTests.Support;

/// <summary>
/// Verificação complementar às regras baseadas em <c>NetArchTest.Rules</c> (que inspecionam uso
/// real de tipos via Mono.Cecil): aqui inspecionamos a lista de assemblies referenciados no
/// próprio metadata do <c>.dll</c> compilado (<see cref="Assembly.GetReferencedAssemblies"/>).
/// Isso pega o caso em que alguém adiciona um <c>PackageReference</c> proibido no <c>.csproj</c>
/// mas ainda não escreveu nenhum código que o use — <c>NotHaveDependencyOnAny</c> (baseado em uso
/// de tipo) não veria isso, mas o build já teria introduzido a dependência que o ADR-039 proíbe.
/// </summary>
internal static class AssemblyReferenceAssertions
{
    /// <summary>
    /// Nomes de assembly considerados parte da BCL/runtime do .NET — o único universo permitido
    /// para <c>Nexora.Domain</c> (CLAUDE.md: "nada além da BCL").
    /// </summary>
    private static readonly string[] BclPrefixes =
    {
        "System", "mscorlib", "netstandard", "WindowsBase",
    };

    public static IReadOnlyList<string> ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToArray();

    public static IReadOnlyList<string> NonBclReferences(Assembly assembly) =>
        ReferencedAssemblyNames(assembly)
            .Where(name => !BclPrefixes.Any(prefix =>
                name.Equals(prefix, StringComparison.Ordinal) ||
                name.StartsWith(prefix + ".", StringComparison.Ordinal)))
            .ToArray();

    public static IReadOnlyList<string> ReferencesMatching(Assembly assembly, params string[] forbiddenPrefixes) =>
        ReferencedAssemblyNames(assembly)
            .Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();
}
