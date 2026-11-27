import { describe, expect, it } from "bun:test"
import {
  childrenOf,
  filterCommands,
  moveSelection,
  pathLabel,
  resolveNode,
  ROOT_COMMANDS
} from "../../src/ui/commandCatalog"

describe("árvore de comandos em português simples", () => {
  it("define os seis comandos raiz e seus subcomandos", () => {
    expect(ROOT_COMMANDS.map(node => node.command)).toEqual([
      "/providers",
      "/models",
      "/local-models",
      "/agents",
      "/session",
      "/system"
    ])

    const expected: Record<string, readonly string[]> = {
      "/providers": ["/providers/list", "/providers/connect", "/providers/status"],
      "/models": ["/models/providers", "/models/active"],
      "/local-models": [
        "/local-models/app",
        "/local-models/list",
        "/local-models/add",
        "/local-models/scan"
      ],
      "/agents": ["/agents/list", "/agents/behavior"],
      "/session": ["/session/list", "/session/new", "/session/clear", "/session/summarize"],
      "/system": ["/system/appearance", "/system/settings", "/system/health"]
    }

    for (const node of ROOT_COMMANDS) {
      expect(childrenOf(node).map(child => child.command)).toEqual([...expected[node.command]])
    }
  })

  it("mantém Ferramentas um nível abaixo de Configurações", () => {
    expect(resolveNode(["system", "settings"])?.kind).toBe("group")
    expect(childrenOf(resolveNode(["system", "settings"])!).map(node => node.command)).toEqual([
      "/system/settings/current",
      "/system/settings/tools"
    ])

    const tools = resolveNode(["system", "settings", "tools"])
    expect(tools?.kind).toBe("group")
    expect(childrenOf(tools!).map(node => node.command)).toEqual([
      "/system/settings/tools/list",
      "/system/settings/tools/safety"
    ])
    expect(resolveNode(["system", "settings", "tools", "list"])?.source).toBe("tools")
    expect(resolveNode(["tools"])).toBeUndefined()
  })

  it("oferece /models/<provider> como filhos dinâmicos de /models/providers", () => {
    const providers = resolveNode(["models", "providers"])
    expect(providers?.dynamic).toBe("modelProviders")

    const children = providers ? childrenOf(providers).map(node => node.command) : []
    expect(children).toEqual([
      "/models/ollama",
      "/models/nvidia",
      "/models/opencode",
      "/models/pc"
    ])
    expect(resolveNode(["models", "providers", "pc"])?.source).toBe("models")
    expect(pathLabel(["local-models", "add"])).toBe("/local-models/add")
  })

  it("filtra por comando ou descrição", () => {
    expect(filterCommands("provid").map(node => node.command)).toEqual(["/providers"])
    // A busca cobre apenas a raiz; Ferramentas agora vive um nível abaixo.
    expect(filterCommands("segurança")).toEqual([])
    expect(filterCommands("sistema").map(node => node.command)).toEqual(["/system"])
    expect(filterCommands("")).toHaveLength(ROOT_COMMANDS.length)
    expect(filterCommands("nada")).toEqual([])
  })

  it("navegação circular permanece estável", () => {
    expect(moveSelection(0, 3, -1)).toBe(2)
    expect(moveSelection(2, 3, 1)).toBe(0)
    expect(moveSelection(0, 0, 1)).toBe(0)
  })
})
