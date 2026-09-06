using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace ValleyArmory.Tests;

internal static class GameAssemblyResolver
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        string? gamePath = FindGamePath();
        if (gamePath is null)
        {
            return;
        }

        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            string candidate = Path.Combine(gamePath, name.Name + ".dll");
            return File.Exists(candidate)
                ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate)
                : null;
        };
    }

    private static string? FindGamePath()
    {
        string? configuredPath = Environment.GetEnvironmentVariable("STARDEW_GAME_PATH");
        if (IsGamePath(configuredPath))
        {
            return configuredPath;
        }

        string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] candidates =
        {
            Path.Combine(userDirectory, ".steam", "steam", "steamapps", "common", "Stardew Valley"),
            Path.Combine(userDirectory, ".local", "share", "Steam", "steamapps", "common", "Stardew Valley")
        };
        return candidates.FirstOrDefault(IsGamePath);
    }

    private static bool IsGamePath(string? path)
    {
        return path is not null && File.Exists(Path.Combine(path, "Stardew Valley.dll"));
    }
}
