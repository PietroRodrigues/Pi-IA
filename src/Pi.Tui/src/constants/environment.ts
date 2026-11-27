// Constantes de ambiente e caminhos da TUI.

// Origem: .env, variáveis de processo e HostConfigurationLoader
// Finalidade: nomes de variáveis e caminhos relativos usados na inicialização.
export const ENV = {
  backendDll: "PI_BACKEND_DLL",
  debug: "PI_DEBUG",
  debugLogDir: "PI_DEBUG_LOG_DIR",
  dotnetEnvironment: "DOTNET_ENVIRONMENT",
  debugEnabledFlag: "1",
  debugTrueText: "true",
  dotnetDevelopment: "Development"
} as const

// Origem: caminhos relativos do repositório
// Finalidade: diretórios e arquivos usados pela TUI.
export const PATHS = {
  solution: "Pi.sln",
  globalJson: "global.json",
  tuiPackage: ["src", "Pi.Tui", "package.json"],
  gitHead: [".git", "HEAD"],
  artifacts: "artifacts",
  parentDirectory: "..",
  debugLogDir: ["artifacts", "debug"],
  hostDll: ["artifacts", "bin", "Pi.Host", "debug", "Pi.Host.dll"],
  logFiles: {
    host: "pi-host.log",
    tui: "pi-tui.log",
    ndjson: "ndjson-trace.log"
  },
  logKeys: {
    host: "host",
    tui: "tui",
    ndjson: "ndjson"
  }
} as const
