// Carrega o .env mais próximo sem sobrescrever variáveis já definidas no processo.
using Pi.Contracts.Constants;

namespace Pi.Host.Configuration;

public static class EnvFileLoader
{
    public static void Load()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, PiConstants.Paths.EnvFileName);
                if (File.Exists(path))
                {
                    Apply(path);
                    return;
                }

                directory = directory.Parent;
            }
        }
    }

    private static void Apply(string path)
    {
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(PiConstants.Env.CommentPrefix))
            {
                continue;
            }

            var separator = line.IndexOf(PiConstants.Env.AssignmentSeparator);
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim(PiConstants.Env.Quotes);

            // Variáveis já definidas pelo processo têm prioridade sobre o .env.
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
