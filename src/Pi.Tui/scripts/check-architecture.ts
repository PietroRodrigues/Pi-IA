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
  "src/Pi.Contracts/Constants/PiConstants.cs",
  "src/Pi.Contracts/Constants/PiConstants.Configuration.cs",
  "src/Pi.Contracts/Constants/PiConstants.Protocol.cs",
  "src/Pi.Contracts/Constants/PiConstants.Execution.cs",
  "src/Pi.Contracts/Constants/PiConstants.Text.cs",
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
  "src/Pi.Tui/src/constants.ts",
  "src/Pi.Tui/src/constants/environment.ts",
  "src/Pi.Tui/src/constants/commands.ts",
  "src/Pi.Tui/src/constants/commandTree.ts",
  "src/Pi.Tui/src/constants/catalogs.ts",
  "src/Pi.Tui/src/constants/protocol.ts",
  "src/Pi.Tui/src/constants/runtime.ts",
  "src/Pi.Tui/src/constants/visual.ts",
  "src/Pi.Tui/src/constants/text.ts",
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

// Remove comentários para que documentação (ex.: "SIGINT no Ctrl+C") não
// dispare as regras; strings são preservadas.
function stripComments(code: string): string {
  let result = ""
  let quote: string | null = null
  let index = 0

  while (index < code.length) {
    const char = code[index]
    const next = code[index + 1]

    if (quote) {
      result += char
      if (char === "\\") {
        result += next ?? ""
        index += 2
        continue
      }
      if (char === quote) quote = null
      index += 1
      continue
    }

    if (char === '"' || char === "'" || char === "`") {
      quote = char
      result += char
      index += 1
      continue
    }

    if (char === "/" && next === "/") {
      while (index < code.length && code[index] !== "\n") {
        result += " "
        index += 1
      }
      continue
    }

    if (char === "/" && next === "*") {
      result += "  "
      index += 2
      while (index < code.length && !(code[index] === "*" && code[index + 1] === "/")) {
        result += code[index] === "\n" ? "\n" : " "
        index += 1
      }
      result += "  "
      index += 2
      continue
    }

    result += char
    index += 1
  }

  return result
}

// Literais da TUI vivem apenas em src/constants.ts e no diretório src/constants/
// (dividido por domínio). O contrato em protocol/schema.ts é o espelho intencional
// do C# e também é isento.
const literalRules: ReadonlyArray<{ readonly name: string; readonly pattern: RegExp }> = [
  { name: "cor hexadecimal", pattern: /#[0-9a-fA-F]{6}\b/ },
  {
    name: "URL ou endpoint",
    pattern: /https?:\/\/|\/api\/(?:tags|chat|generate|models)|\/v1\/models|127\.0\.0\.1|localhost:|integrate\.api\.nvidia/
  },
  { name: "modelo conhecido", pattern: /\b(?:qwen[0-9.-]*|llama3(?:\.2)?|deepseek-r1|phi4:)\b/i },
  { name: "variável de ambiente", pattern: /\b(?:PI|OLLAMA|DOTNET)_[A-Z0-9_]+/ },
  { name: "sinal de processo", pattern: /\bSIG(?:INT|TERM|HUP|BREAK|KILL)\b/ },
  {
    name: "comando de barra",
    pattern: /["'`]\/(?:sair|exit|providers|models|local-models|agents|tools|session|system)(?:\/[\w-]+)*["'`]/
  },
  { name: "glifo de UI", pattern: /["'`][^"'`]*[▏▎▌✓✗⚙▶●─—][^"'`]*["'`]/ },
  { name: "texto em português", pattern: /["'`][^"'`\n]*[\u00C0-\u00FF][^"'`\n]*["'`]/ },
  { name: "número fixo", pattern: /\b(?:32768|11434|4000|1800|350|160|1234|8080|3000)\b/ },
  {
    name: "tipo de protocolo",
    pattern: /["'`](?:prompt_submit|interrupt_turn|tool_decision|session_switch|session_clear|session_snapshot|stream_delta|tool_request|tool_result|context_metrics|error_event|user_cancel|invalid_message|unsupported_version|empty_prompt|unknown_command|tool_denied|tool_timeout|llm_failure|session_failure|internal)["'`]/
  }
]

for (const path of sources) {
  const relative = path.slice(root.length + 1).replace(/\\/g, "/")
  const isTuiSource = relative.startsWith("src/Pi.Tui/src/") && /\.(ts|tsx)$/.test(relative)
  const isConstantFile =
    relative.endsWith("/src/constants.ts") ||
    relative.startsWith("src/Pi.Tui/src/constants/")
  if (!isTuiSource || isConstantFile || relative.endsWith("/protocol/schema.ts")) {
    continue
  }

  const code = stripComments(readFileSync(path, "utf8"))
  for (const rule of literalRules) {
    if (rule.pattern.test(code)) {
      problems.push(`Literal de ${rule.name} fora de src/constants.ts/src/constants/: ${relative}`)
    }
  }
}

if (problems.length > 0) {
  for (const problem of problems) console.error(`✗ ${problem}`)
  process.exit(1)
}

console.log("Arquitetura OK: contrato v1, camadas e árvore alvo preservadas.")
