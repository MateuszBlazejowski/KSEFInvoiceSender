using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Database;
public static class SQLloader
{
    public static string LoadFromFile(string relativePath)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, relativePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"SQL file not found: {fullPath}");

        return File.ReadAllText(fullPath);
    }

    public static string LoadEmbeddedQuery(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Resource names are full namespace + folder + filename
        string? fullName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

        if (fullName == null)
            throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

        using var stream = assembly.GetManifestResourceStream(fullName);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}
