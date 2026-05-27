using Pi.App.Core;

namespace Pi.App.Tools
{
    public class SearchFilesTool : ITool
    {
        public string Name => ToolNames.SearchFiles;

        private readonly string[] _allowedFolders =
        {
            @"E:\MeusProjetosRepositorio",
            @"D:\Gearbox"
        };

        public bool CanHandle(string input)
        {
            return input.StartsWith("procure", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> Execute(string input)
        {
            var searchTerm = input.Replace("procure", "", StringComparison.OrdinalIgnoreCase).Trim();

            return await Task.Run(() =>
            {
                Console.WriteLine(
                    $"\nDEBUG: procurando '{searchTerm}'"
                );

                var foundFiles = new List<string>();

                foreach (var folder in _allowedFolders)
                {
                    Console.WriteLine(
                        $"DEBUG: pasta {folder}"
                    );

                    if (!Directory.Exists(folder))
                    {
                        Console.WriteLine(
                            "DEBUG: pasta não existe"
                        );

                        continue;
                    }

                    try
                    {
                        var files = Directory.EnumerateFiles(
                            folder, 
                            "*", 
                            SearchOption.AllDirectories
                        );


                        foreach (var file in files)
                        {
                            var fileName = Path.GetFileName(file);

                            if(fileName.Contains(
                                searchTerm,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                foundFiles.Add(file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"DEBUG ERRO: {ex.Message}"
                        );
                    }
                                 
                }

                if (!foundFiles.Any()) 
                    return "Nenhum arquivo encontrado.";
                
                return string.Join(
                    Environment.NewLine, 
                    foundFiles.Take(20)
                    );

            });
        }
    }
}