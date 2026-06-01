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

        if (Path.IsPathRooted(normalizedPath))
        {
            throw new ArgumentException(
                $"Fixture path must be relative to the test base directory, but was rooted: '{relativePath}'.",
                nameof(relativePath));
        }

        return Path.Combine(AppContext.BaseDirectory, normalizedPath);
    }
}
