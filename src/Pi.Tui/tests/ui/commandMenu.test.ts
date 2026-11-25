import { describe, expect, it } from "bun:test"
import type { CommandMenuState } from "../../src/ui/commandMenuState"
import {
  initialCommandMenuState,
  menuHeaderLabel,
  reduceCommandMenu,
  selectionGroups,
  visibleItems
} from "../../src/ui/commandMenuState"

function open(query = "", base: CommandMenuState = initialCommandMenuState): CommandMenuState {
  return reduceCommandMenu(base, { kind: "type", value: query })
}

function descend(state: CommandMenuState, commands: readonly string[]): CommandMenuState {
  let current = state
  for (const command of commands) {
    const target = visibleItems(current).findIndex(entry => entry.id === command)
    if (target < 0) throw new Error(`Comando não encontrado: ${command}`)

    while (current.index !== target) {
      current = reduceCommandMenu(current, { kind: "down" })
    }
    current = reduceCommandMenu(current, { kind: "enter" })
  }
  return current
}

describe('menu de comandos "/"', () => {
  it("oferece a árvore raiz completa em português simples", () => {
    const state = open()
    const commands = visibleItems(state).map(item => item.label)

    expect(commands).toEqual([
      "/providers",
      "/models",
      "/local-models",
      "/agents",
      "/tools",
      "/session",
      "/system"
    ])
    expect(visibleItems(state)[0].detail).toBe("Conectar e ver provedores")
  })

  it("navega por setas e Enter pelos subcomandos", () => {
    let state = open()
    state = reduceCommandMenu(state, { kind: "down" })
    expect(state.index).toBe(1)

    state = reduceCommandMenu(state, { kind: "enter" })
    expect(state.path).toEqual(["models"])
    // Dentro do submenu o prefixo já está no cabeçalho; a linha usa o nome curto.
    expect(visibleItems(state).map(item => item.label)).toEqual(["providers", "active"])
    expect(visibleItems(state).map(item => item.command)).toEqual([
      "/models/providers",
      "/models/active"
    ])

    state = reduceCommandMenu(state, { kind: "enter" })
    expect(state.path).toEqual(["models", "providers"])
    expect(visibleItems(state).map(item => item.id)).toContain("/models/ollama")
    expect(visibleItems(state).map(item => item.id)).toContain("/models/pc")
  })

  it("filtra por Search e reinicia a seleção", () => {
    let state = open()
    state = reduceCommandMenu(state, { kind: "down" })
    state = reduceCommandMenu(state, { kind: "type", value: "local" })

    expect(state.index).toBe(0)
    expect(visibleItems(state).map(item => item.label)).toEqual(["/local-models"])
  })

  it("Esc volta um nível e depois fecha", () => {
    let state = open("prov")
    state = reduceCommandMenu(state, { kind: "enter" })
    expect(state.path).toEqual(["providers"])

    state = reduceCommandMenu(state, { kind: "escape" })
    expect(state.path).toEqual([])
    expect(state.open).toBe(true)

    state = reduceCommandMenu(state, { kind: "escape" })
    expect(state.open).toBe(false)
  })

  it("filtra listas de seleção e agrupa por subtítulos", () => {
    let state = open("local")
    state = reduceCommandMenu(state, { kind: "enter" })
    state = descend(state, ["/local-models/list"])

    expect(state.path).toEqual(["local-models", "list"])
    expect(selectionGroups(state).map(group => group.title)).toEqual([
      "Modelos no PC",
      "Ollama · não quantizados",
      "Ollama · quantizados"
    ])

    state = reduceCommandMenu(state, { kind: "type", value: "IQ2" })
    expect(visibleItems(state).map(item => item.label)).toEqual(["qwen3-6b-iq2-local"])
    expect(selectionGroups(state).map(group => group.title)).toEqual(["Modelos no PC"])
  })

  it("mantém a opção de adicionar modelo do PC no topo de /local-models/add", () => {
    let state = open("local")
    state = reduceCommandMenu(state, { kind: "enter" })
    state = descend(state, ["/local-models/add"])

    expect(visibleItems(state)[0]).toMatchObject({
      id: "add-local-model",
      label: "Adicionar modelo do PC"
    })

    state = reduceCommandMenu(state, { kind: "localAddType", value: "C:/modelos" })
    state = reduceCommandMenu(state, { kind: "enter" })
    expect(state.localAddPath).toBe("C:/modelos")
    expect(state.localAddBrowse).toBe(true)
  })

  it("seleciona provedor, modelo e programa apenas no estado visual", () => {
    let state = open("prov")
    state = reduceCommandMenu(state, { kind: "enter" })
    state = descend(state, ["/providers/list"])
    state = reduceCommandMenu(state, { kind: "down" })
    state = reduceCommandMenu(state, { kind: "enter" })
    expect(state.provider).toBe("NVIDIA")
    expect(state.model).toBe(initialCommandMenuState.model)

    state = open("local", state)
    state = reduceCommandMenu(state, { kind: "enter" })
    state = descend(state, ["/local-models/app"])
    state = reduceCommandMenu(state, { kind: "down" })
    state = reduceCommandMenu(state, { kind: "enter" })
    expect(state.runtime).toBe("LM Studio")

    state = open("local", state)
    state = reduceCommandMenu(state, { kind: "enter" })
    state = descend(state, ["/local-models/list"])
    state = reduceCommandMenu(state, { kind: "type", value: "IQ2" })
    state = reduceCommandMenu(state, { kind: "enter" })
    expect(state.model).toBe("qwen3-6b-iq2-local")
  })

  it("mostra rótulos curtos e o caminho atual no cabeçalho dos submenus", () => {
    const expected: Record<string, readonly string[]> = {
      "/agents": ["list", "behavior"],
      "/tools": ["list", "safety"],
      "/session": ["list", "new", "clear", "summarize"],
      "/system": ["appearance", "settings", "health"]
    }

    for (const [command, children] of Object.entries(expected)) {
      let state = open(command.slice(1))
      state = reduceCommandMenu(state, { kind: "enter" })

      expect(menuHeaderLabel(state)).toBe(`${command} · ${children.length} item(ns)`)
      expect(visibleItems(state).map(item => item.label)).toEqual([...children])
      expect(visibleItems(state).map(item => item.command)).toEqual(
        children.map(child => `${command}/${child}`)
      )
    }
  })

  it("mantém os comandos completos na raiz", () => {
    const state = open()

    expect(menuHeaderLabel(state)).toBe("/ · 7 item(ns)")
    expect(visibleItems(state).map(item => item.label)).toEqual([
      "/providers",
      "/models",
      "/local-models",
      "/agents",
      "/tools",
      "/session",
      "/system"
    ])
  })
})
