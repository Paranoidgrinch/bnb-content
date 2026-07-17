namespace BnbContent.Tests;

public static class TestData
{
    // The repo's source-data directory, found from the test assembly's location.
    public static string Directory { get; } = Find();

    private static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "source-data");
            if (System.IO.Directory.Exists(candidate))
                return candidate;
            directory = directory.Parent!;
        }
        throw new InvalidOperationException("Could not find the repo's source-data directory.");
    }
}
