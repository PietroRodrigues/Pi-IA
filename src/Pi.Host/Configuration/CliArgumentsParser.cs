// Interpreta apenas a flag de ponte; o restante segue para o host genérico.
namespace Pi.Host.Configuration;

public sealed record CliArguments(bool UseBridge, string[] HostArgs);

public static class CliArgumentsParser
{
    // Aceita --bridge, --bridge stdio e --bridge=stdio; o resto vai para o host.
    public static CliArguments Parse(string[] args)
    {
        var useBridge = false;
        var hostArgs = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            if (argument.Equals("--bridge", StringComparison.OrdinalIgnoreCase))
            {
                useBridge = true;

                if (index + 1 < args.Length &&
                    args[index + 1].Equals("stdio", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                }

                continue;
            }

            if (argument.StartsWith("--bridge=", StringComparison.OrdinalIgnoreCase))
            {
                useBridge = true;
                continue;
            }

            hostArgs.Add(argument);
        }

        return new CliArguments(useBridge, hostArgs.ToArray());
    }
}
