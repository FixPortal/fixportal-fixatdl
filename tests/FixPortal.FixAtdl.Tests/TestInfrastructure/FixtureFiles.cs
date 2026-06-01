namespace FixPortal.FixAtdl.Tests.TestInfrastructure;

internal static class FixtureFiles
{
    public static string ReadAllText(string relativePath)
    {
        return File.ReadAllText(GetPath(relativePath));
    }

    public static Task<string> ReadAllTextAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        return File.ReadAllTextAsync(GetPath(relativePath), cancellationToken);
    }

    public static Stream OpenRead(string relativePath)
    {
        return File.OpenRead(GetPath(relativePath));
    }

    private static string GetPath(string relativePath)
    {
        string normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(AppContext.BaseDirectory, normalizedPath);
    }
}
