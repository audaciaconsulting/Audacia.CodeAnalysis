namespace Audacia.CodeAnalysis.Analyzers.Settings
{
    public class SettingsKey
    {
        public const string DotnetDiagnostic = "dotnet_diagnostic";

        public string Rule { get; }

        public string Name { get; }

        public SettingsKey(string rule, string name)
        {
            Rule = rule;
            Name = name;
        }

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
}
