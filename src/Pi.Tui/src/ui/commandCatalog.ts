// Árvore de comandos "/" em linguagem simples. Nenhum comando é enviado ao host:
// a navegação aqui é apenas a topologia visual da TUI.
import {
  MODEL_PROVIDERS,
  type CatalogSource,
  type InfoSource
} from "./providerCatalog"

export type CommandNodeKind = "group" | "selection" | "connect" | "local-add" | "info"

export interface CommandNode {
  readonly segment: string
  readonly command: string
  readonly description: string
  readonly kind: CommandNodeKind
  readonly source?: CatalogSource | InfoSource
  readonly children?: readonly CommandNode[]
  readonly dynamic?: "modelProviders"
}

export const ROOT_COMMANDS: readonly CommandNode[] = [
  {
    segment: "providers",
    command: "/providers",
    description: "Conectar e ver provedores",
    kind: "group",
    children: [
      {
        segment: "list",
        command: "/providers/list",
        description: "Ver provedores disponíveis",
        kind: "selection",
        source: "providers"
      },
      {
        segment: "connect",
        command: "/providers/connect",
        description: "Conectar um provedor",
        kind: "connect",
        source: "providers"
      },
      {
        segment: "status",
        command: "/providers/status",
        description: "Status dos provedores",
        kind: "info",
        source: "providerStatus"
      }
    ]
  },
  {
    segment: "models",
    command: "/models",
    description: "Escolher modelos e provedores",
    kind: "group",
    children: [
      {
        segment: "providers",
        command: "/models/providers",
        description: "Modelos por provedor",
        kind: "group",
        dynamic: "modelProviders"
      },
      {
        segment: "active",
        command: "/models/active",
        description: "Modelo ativo",
        kind: "selection",
        source: "models"
      }
    ]
  },
  {
    segment: "local-models",
    command: "/local-models",
    description: "Modelos e programas no PC",
    kind: "group",
    children: [
      {
        segment: "app",
        command: "/local-models/app",
        description: "Programa que roda a IA",
        kind: "selection",
        source: "programs"
      },
      {
        segment: "list",
        command: "/local-models/list",
        description: "Ver modelos locais",
        kind: "selection",
        source: "localModels"
      },
      {
        segment: "add",
        command: "/local-models/add",
        description: "Adicionar modelo do PC",
        kind: "local-add"
      },
      {
        segment: "scan",
        command: "/local-models/scan",
        description: "Procurar modelos (visual)",
        kind: "info",
        source: "localScan"
      }
    ]
  },
  {
    segment: "agents",
    command: "/agents",
    description: "Agentes e comportamento",
    kind: "group",
    children: [
      {
        segment: "list",
        command: "/agents/list",
        description: "Ver agentes",
        kind: "selection",
        source: "agents"
      },
      {
        segment: "behavior",
        command: "/agents/behavior",
        description: "Comportamento do agente",
        kind: "selection",
        source: "behaviors"
      }
    ]
  },
  {
    segment: "tools",
    command: "/tools",
    description: "Ferramentas e segurança",
    kind: "group",
    children: [
      {
        segment: "list",
        command: "/tools/list",
        description: "Ver ferramentas",
        kind: "selection",
        source: "tools"
      },
      {
        segment: "safety",
        command: "/tools/safety",
        description: "Regras de segurança",
        kind: "info",
        source: "toolSafety"
      }
    ]
  },
  {
    segment: "session",
    command: "/session",
    description: "Sessões da conversa",
    kind: "group",
    children: [
      {
        segment: "list",
        command: "/session/list",
        description: "Ver sessões",
        kind: "selection",
        source: "sessions"
      },
      {
        segment: "new",
        command: "/session/new",
        description: "Nova sessão (visual)",
        kind: "info",
        source: "sessionNew"
      },
      {
        segment: "clear",
        command: "/session/clear",
        description: "Limpar sessão (visual)",
        kind: "info",
        source: "sessionClear"
      },
      {
        segment: "summarize",
        command: "/session/summarize",
        description: "Resumir sessão (visual)",
        kind: "info",
        source: "sessionSummarize"
      }
    ]
  },
  {
    segment: "system",
    command: "/system",
    description: "Aparência e sistema",
    kind: "group",
    children: [
      {
        segment: "appearance",
        command: "/system/appearance",
        description: "Aparência da TUI",
        kind: "selection",
        source: "appearances"
      },
      {
        segment: "settings",
        command: "/system/settings",
        description: "Configurações atuais",
        kind: "info",
        source: "systemSettings"
      },
      {
        segment: "health",
        command: "/system/health",
        description: "Saúde do sistema",
        kind: "info",
        source: "systemHealth"
      }
    ]
  }
]

export function childrenOf(node: CommandNode): readonly CommandNode[] {
  if (node.dynamic === "modelProviders") {
    return MODEL_PROVIDERS.map(provider => ({
      segment: provider.id,
      command: `/models/${provider.id}`,
      description: `Modelos de ${provider.label}`,
      kind: "selection" as const,
      source: "models" as const
    }))
  }

  return node.children ?? []
}

export function resolvePath(path: readonly string[]): CommandNode | undefined {
  return resolveNode(path)
}

// Resolve o nó final de um caminho, incluindo os filhos dinâmicos de /models/providers.
export function resolveNode(path: readonly string[]): CommandNode | undefined {
  if (path.length === 0) return undefined

  let node = ROOT_COMMANDS.find(entry => entry.segment === path[0])
  if (!node) return undefined

  for (const segment of path.slice(1)) {
    node = childrenOf(node).find(entry => entry.segment === segment)
    if (!node) return undefined
  }

  return node
}

export function parentPath(path: readonly string[]): readonly string[] {
  return path.slice(0, -1)
}

export function pathLabel(path: readonly string[]): string {
  return path.length === 0 ? "/" : `/${path.join("/")}`
}

export function filterCommands(query: string): CommandNode[] {
  const term = query.trim().toLowerCase()
  if (term.length === 0) {
    return [...ROOT_COMMANDS]
  }

  return ROOT_COMMANDS.filter(command =>
    `${command.command} ${command.description}`.toLowerCase().includes(term))
}

// Navegação circular: a seta sempre aponta para um item quando há opções.
export function moveSelection(index: number, count: number, delta: -1 | 1): number {
  if (count <= 0) {
    return 0
  }

  return ((index + delta) % count + count) % count
}
