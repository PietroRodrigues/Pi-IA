// Guardrails da arquitetura alvo. Falha rápido se pastas, contratos ou
// fronteiras entre camadas forem alterados sem atualizar a documentação.
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs"
import { join } from "node:path"

const root = join(import.meta.dir, "..", "..", "..")

const required = [
  "Pi.sln",
  "docs/protocol-v1.md",
  ".env.example",
  ".gitattributes",
  "src/Pi.Contracts/Protocol/Envelope.cs",
  "src/Pi.Contracts/Protocol/Commands.cs",
  "src/Pi.Contracts/Protocol/Events.cs",
  "src/Pi.Contracts/Models/SessionSnapshot.cs",
  "src/Pi.Core/Abstractions/ILlmProvider.cs",
  "src/Pi.Core/Abstractions/IAgentTool.cs",
  "src/Pi.Core/Abstractions/IPermissionPolicy.cs",
  "src/Pi.Core/Abstractions/ISessionRepository.cs",
  "src/Pi.Core/Abstractions/IEventSink.cs",
  "src/Pi.Core/Engine/AgentOrchestrator.cs",
  "src/Pi.Core/Engine/ContextBudgetManager.cs",
  "src/Pi.Core/Engine/ToolExecutionCoordinator.cs",
  "src/Pi.Core/Policies/WorkspaceSandboxPolicy.cs",
  "src/Pi.Infrastructure/Llm/OllamaProvider.cs",
  "src/Pi.Infrastructure/Llm/ModelCapabilities.cs",
  "src/Pi.Infrastructure/Tools/ReadFileTool.cs",
  "src/Pi.Infrastructure/Tools/EditFileTool.cs",
  "src/Pi.Infrastructure/Tools/SearchCodeTool.cs",
  "src/Pi.Infrastructure/Tools/ExecuteShellTool.cs",
  "src/Pi.Infrastructure/Persistence/JsonSessionRepository.cs",
  "src/Pi.Host/Program.cs",
  "src/Pi.Host/Bridge/NdjsonReader.cs",
  "src/Pi.Host/Bridge/NdjsonWriter.cs",
  "src/Pi.Host/Bridge/CommandDispatcher.cs",
  "src/Pi.Host/Process/TuiProcessSupervisor.cs",
  "src/Pi.Tui/package.json",
  "src/Pi.Tui/tsconfig.json",
  "src/Pi.Tui/src/index.ts",
  "src/Pi.Tui/src/protocol/schema.ts",
  "src/Pi.Tui/src/protocol/transport.ts",
  "src/Pi.Tui/src/state/sessionStore.ts",
  "src/Pi.Tui/src/state/reducers.ts",
  "src/Pi.Tui/src/ui/App.tsx",
  "src/Pi.Tui/src/ui/ChatView.tsx",
  "src/Pi.Tui/src/ui/ApprovalModal.tsx",
  "src/Pi.Tui/src/ui/InputBar.tsx",
  "src/Pi.Tui/src/ui/StatusBar.tsx",
  "src/Pi.Tui/src/telemetry/fileLogger.ts",
  "tests/Pi.Core.Tests/Pi.Core.Tests.csproj",
  "tests/Pi.Infrastructure.Tests/Pi.Infrastructure.Tests.csproj",
  "tests/Pi.Bridge.Tests/Pi.Bridge.Tests.csproj"
]

const problems: string[] = []

for (const item of required) {
  if (!existsSync(join(root, item))) problems.push(`Arquivo ausente: ${item}`)
}

if (existsSync(join(root, "Pi.App"))) problems.push("A pasta legada Pi.App/ ainda existe.")

function files(dir: string): string[] {
  if (!existsSync(dir)) return []

  return readdirSync(dir).flatMap(name => {
    const path = join(dir, name)
    return statSync(path).isDirectory()
      ? files(path)
      : /\.(ts|tsx|cs)$/.test(name) ? [path] : []
  })
}

const sources = [
  ...files(join(root, "src")),
  ...files(join(root, "tests"))
].filter(
  path =>
    !path.includes("node_modules") &&
    !path.includes("dist") &&
    !path.endsWith("check-architecture.ts") &&
    !path.endsWith("StructureTests.cs")
)

for (const path of sources) {
  const code = readFileSync(path, "utf8")
  const relative = path.slice(root.length + 1).replace(/\\/g, "/")

  if (/Pi\.App|BodyTools|interfaceTui|Pi\.App\.slnx/.test(code)) {
    problems.push(`Referência legada em ${relative}`)
  }

  if (relative.includes("/Pi.Core/") && /Pi\.(Infrastructure|Host)|@opentui/.test(code)) {
    problems.push(`Pi.Core não pode depender de camadas externas: ${relative}`)
  }

  if (
    relative.includes("/src/Pi.Tui/src/protocol/") ||
    relative.includes("/src/Pi.Tui/src/state/")
  ) {
    if (/@opentui|\.\.\/ui\//.test(code)) {
      problems.push(`protocol/ e state/ não podem importar a View: ${relative}`)
    }
  }

  if (
    relative.includes("/src/Pi.Tui/src/ui/") &&
    /protocol\/transport/.test(code) &&
    !relative.endsWith("/ui/App.tsx")
  ) {
    problems.push(`Somente App.tsx pode criar o transporte: ${relative}`)
  }
}

if (problems.length > 0) {
  for (const problem of problems) console.error(`✗ ${problem}`)
  process.exit(1)
}

console.log("Arquitetura OK: contrato v1, camadas e árvore alvo preservadas.")
