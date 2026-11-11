namespace api.Configuration;

/// <summary>
/// Loads a local ".env" file (KEY=value per line) into environment variables,
/// so every developer can configure the API the same way: copy .env.example to .env.
/// ASP.NET then reads them like any environment variable (Jwt__Key → Jwt:Key).
///
/// - Real environment variables always win (production uses Fly secrets, not a file).
/// - Empty values are skipped, so blank lines copied from .env.example don't
///   override values set elsewhere (e.g. user-secrets).
/// - The .env file is git-ignored and excluded from the Docker image (.dockerignore).
/// </summary>
public static class DotEnv
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');

            if (value.Length > 0 && Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
