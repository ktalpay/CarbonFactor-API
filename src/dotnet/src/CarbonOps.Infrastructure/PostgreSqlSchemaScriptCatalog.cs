using System.Text;

namespace CarbonOps.Infrastructure;

public sealed class PostgreSqlSchemaScriptCatalog : IPostgreSqlSchemaScriptCatalog
{
    private const string ScriptRoot = "Database/postgresql";
    private const string ManifestName = "schema-manifest.txt";

    private readonly string _basePath;

    public PostgreSqlSchemaScriptCatalog(string? basePath = null)
    {
        _basePath = basePath ?? AppContext.BaseDirectory;
    }

    public IReadOnlyList<PostgreSqlSchemaScript> GetScripts()
    {
        var scriptDirectoryPath = ResolvePath(ScriptRoot);
        var manifestPath = ResolvePath(Path.Combine(ScriptRoot, ManifestName));

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Schema manifest not found at '{manifestPath}'.", manifestPath);
        }

        var scripts = new List<PostgreSqlSchemaScript>();

        foreach (var entry in File.ReadAllLines(manifestPath, Encoding.UTF8)
                     .Select(static line => line.Trim())
                     .Where(static line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#')))
        {
            var relativePath = Path.Combine(ScriptRoot, entry);
            var scriptPath = ResolvePath(relativePath);

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Schema script listed in manifest was not found: '{scriptPath}'.", scriptPath);
            }

            var sql = File.ReadAllText(scriptPath, Encoding.UTF8);
            scripts.Add(new PostgreSqlSchemaScript(entry, relativePath.Replace('\\', '/'), sql));
        }

        return scripts;
    }

    private string ResolvePath(string relativePath) => Path.GetFullPath(Path.Combine(_basePath, relativePath));
}
