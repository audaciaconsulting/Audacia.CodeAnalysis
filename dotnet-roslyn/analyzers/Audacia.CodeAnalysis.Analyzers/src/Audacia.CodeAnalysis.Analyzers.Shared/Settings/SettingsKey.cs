namespace Audacia.CodeAnalysis.Analyzers.Shared.Settings;

public class SettingsKey(string rule, string name)
{
    public const string DotnetDiagnostic = "dotnet_diagnostic";

    public string Rule { get; } = rule;

    public string Name { get; } = name;

    public override bool Equals(object obj)
    {
        return obj is SettingsKey other && other.Rule == Rule && other.Name == Name;
    }

    public override int GetHashCode()
    {
        return Rule.GetHashCode() ^ Name.GetHashCode();
    }

    public override string ToString()
    {
        return string.Join(".", DotnetDiagnostic, Rule, Name);
    }
}
