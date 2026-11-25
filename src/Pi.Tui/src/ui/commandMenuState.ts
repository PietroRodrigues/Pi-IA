// Estado puramente local do menu "/". Nenhuma ação altera o host, o store de
// sessão ou o protocolo: tudo aqui é demonstração visual da TUI.
import {
  childrenOf,
  filterCommands,
  moveSelection,
  pathLabel,
  resolveNode,
  type CommandNode
} from "./commandCatalog"
import {
  catalogFor,
  filterCatalogItems,
  groupCatalogItems,
  INFO_PANELS,
  LOCAL_ADD_OPTION,
  PROVIDERS,
  LOCAL_MODELS,
  type CatalogItem,
  type CatalogSource,
  type InfoSource,
  type SelectionGroup
} from "./providerCatalog"

export interface ApiKeyWindowState {
  readonly provider: string
  readonly providerId: string
  readonly value: string
  readonly returnPath: readonly string[]
}

export interface CommandMenuState {
  readonly open: boolean
  readonly path: readonly string[]
  readonly query: string
  readonly index: number
  readonly provider: string
  readonly model: string
  readonly runtime: string
  readonly connectedProviders: readonly string[]
  readonly apiKey: ApiKeyWindowState | undefined
  readonly localAddPath: string
  readonly localAddBrowse: boolean
}

export interface CommandMenuItem {
  readonly id: string
  readonly command?: string
  readonly label: string
  readonly detail: string
  readonly active: boolean
}

export const initialCommandMenuState: CommandMenuState = {
  open: false,
  path: [],
  query: "",
  index: 0,
  provider: PROVIDERS[0]?.label ?? "Ollama (Local)",
  model: LOCAL_MODELS[0]?.label ?? "qwen3.8-2b-local",
  runtime: "Ollama",
  connectedProviders: [],
  apiKey: undefined,
  localAddPath: "",
  localAddBrowse: false
}

export type CommandMenuAction =
  | { kind: "type"; value: string }
  | { kind: "up" }
  | { kind: "down" }
  | { kind: "enter" }
  | { kind: "escape" }
  | { kind: "close" }
  | { kind: "apiKeyType"; value: string }
  | { kind: "apiKeySubmit" }
  | { kind: "apiKeyCancel" }
  | { kind: "localAddType"; value: string }

function closed(state: CommandMenuState): CommandMenuState {
  return {
    ...state,
    open: false,
    path: [],
    query: "",
    index: 0,
    apiKey: undefined,
    localAddBrowse: false
  }
}

function catalogItemsFor(state: CommandMenuState, source: CatalogSource): CatalogItem[] {
  const items = [...catalogFor(source)]

  // /models/<provider> mostra apenas os modelos daquele provedor.
  if (source === "models" && state.path[0] === "models" && state.path.length > 1) {
    const providerId = state.path[state.path.length - 1]
    if (providerId !== "active" && providerId !== "providers") {
      return items.filter(item => item.providerId === providerId)
    }
  }

  return items
}

function isActive(state: CommandMenuState, source: CatalogSource, item: CatalogItem): boolean {
  switch (source) {
    case "providers":
      return item.label === state.provider || state.connectedProviders.includes(item.id)
    case "models":
    case "localModels":
      return item.label === state.model
    case "programs":
      return item.label === state.runtime
    default:
      return false
  }
}

function infoItems(state: CommandMenuState, source: InfoSource): CommandMenuItem[] {
  const dynamic: CommandMenuItem[] = []

  if (source === "providerStatus") {
    dynamic.push(
      { id: "provider", label: "Provedor ativo", detail: state.provider, active: true },
      { id: "runtime", label: "Programa no PC", detail: state.runtime, active: true },
      {
        id: "connected",
        label: "Conectados (visual)",
        detail: state.connectedProviders.join(", ") || "nenhum",
        active: state.connectedProviders.length > 0
      }
    )
  }

  if (source === "systemSettings") {
    dynamic.push(
      { id: "provider", label: "Provedor ativo", detail: state.provider, active: true },
      { id: "model", label: "Modelo ativo", detail: state.model, active: true }
    )
  }

  const rows: CommandMenuItem[] = [
    ...dynamic,
    ...INFO_PANELS[source].map(info => ({
      id: info.label,
      label: info.label,
      detail: info.detail,
      active: false
    }))
  ]

  const term = state.query.trim().toLowerCase()
  if (term.length === 0) return rows
  return rows.filter(row => `${row.label} ${row.detail}`.toLowerCase().includes(term))
}

export function visibleItems(state: CommandMenuState): CommandMenuItem[] {
  if (state.apiKey) return []

  if (state.path.length === 0) {
    return filterCommands(state.query).map(command => ({
      id: command.command,
      command: command.command,
      label: command.command,
      detail: command.description,
      active: false
    }))
  }

  const node = resolveNode(state.path)
  if (!node) return []

  if (node.kind === "group") {
    const term = state.query.trim().toLowerCase()
    // Dentro de um submenu o prefixo já está no cabeçalho: cada linha mostra
    // só o nome curto (list, connect, status...), com o caminho completo em command.
    const children = childrenOf(node).map(child => ({
      id: child.command,
      command: child.command,
      label: child.segment,
      detail: child.description,
      active: false
    }))

    if (term.length === 0) return children
    return children.filter(child =>
      `${child.command} ${child.label} ${child.detail}`.toLowerCase().includes(term))
  }

  if (node.kind === "local-add") {
    return [
      {
        id: LOCAL_ADD_OPTION.id,
        label: LOCAL_ADD_OPTION.label,
        detail: LOCAL_ADD_OPTION.detail,
        active: state.localAddBrowse
      }
    ]
  }

  if (node.kind === "info") {
    return infoItems(state, node.source as InfoSource)
  }

  const source = node.source as CatalogSource
  return filterCatalogItems(catalogItemsFor(state, source), state.query).map(item => ({
    id: item.id,
    label: item.label,
    detail: item.detail,
    active: isActive(state, source, item)
  }))
}

export function currentNode(state: CommandMenuState): CommandNode | undefined {
  return resolveNode(state.path)
}

export function selectionGroups(state: CommandMenuState): SelectionGroup[] {
  const node = resolveNode(state.path)
  if (!node || (node.kind !== "selection" && node.kind !== "connect")) return []
  return groupCatalogItems(catalogItemsFor(state, node.source as CatalogSource), state.query)
}

export function menuHeaderLabel(state: CommandMenuState): string {
  return `${pathLabel(state.path)} · ${visibleItems(state).length} item(ns)`
}

export function currentBreadcrumb(state: CommandMenuState): string {
  const filter = state.query.trim().length > 0 ? ` · filtro: "${state.query.trim()}"` : ""
  return `${menuHeaderLabel(state)}${filter}`
}

function openApiKey(state: CommandMenuState, selected: CommandMenuItem): CommandMenuState {
  const node = currentNode(state)
  const source = (node?.source ?? "providers") as CatalogSource
  const provider = catalogItemsFor(state, source).find(item => item.id === selected.id)
  const label = provider?.label ?? selected.label

  return {
    ...state,
    query: "",
    index: 0,
    apiKey: {
      provider: label,
      providerId: provider?.id ?? selected.id,
      value: "",
      returnPath: state.path
    }
  }
}

function submitApiKey(state: CommandMenuState): CommandMenuState {
  const key = state.apiKey
  if (!key) return state

  return {
    ...state,
    open: true,
    path: key.returnPath,
    query: "",
    index: 0,
    provider: key.provider,
    connectedProviders: [...new Set([...state.connectedProviders, key.providerId])],
    apiKey: undefined
  }
}

function cancelApiKey(state: CommandMenuState): CommandMenuState {
  const key = state.apiKey
  if (!key) return state

  return {
    ...state,
    open: true,
    path: key.returnPath,
    query: "",
    index: 0,
    apiKey: undefined
  }
}

export function reduceCommandMenu(
  state: CommandMenuState,
  action: CommandMenuAction
): CommandMenuState {
  switch (action.kind) {
    case "type":
      if (state.apiKey) return { ...state, apiKey: { ...state.apiKey, value: action.value } }
      return { ...state, open: true, query: action.value, index: 0 }

    case "apiKeyType":
      if (!state.apiKey) return state
      return { ...state, apiKey: { ...state.apiKey, value: action.value } }

    case "localAddType":
      return { ...state, localAddPath: action.value }

    case "apiKeySubmit":
      return submitApiKey(state)

    case "apiKeyCancel":
      return cancelApiKey(state)

    case "up":
    case "down":
      if (state.apiKey) return state
      return {
        ...state,
        index: moveSelection(state.index, visibleItems(state).length, action.kind === "up" ? -1 : 1)
      }

    case "enter": {
      if (state.apiKey) return submitApiKey(state)

      const items = visibleItems(state)
      const selected = items[state.index]
      if (!selected) return state

      const node = currentNode(state)
      if (!node) {
        const command = filterCommands(state.query).find(entry => entry.command === selected.id)
        if (!command) return state
        return { ...state, path: [command.segment], query: "", index: 0 }
      }

      if (node.kind === "group") {
        const child = childrenOf(node).find(entry => entry.command === selected.id)
        if (!child) return state
        return { ...state, path: [...state.path, child.segment], query: "", index: 0 }
      }

      if (node.kind === "connect") {
        return openApiKey(state, selected)
      }

      if (node.kind === "local-add") {
        return { ...state, localAddBrowse: !state.localAddBrowse }
      }

      if (node.kind === "selection") {
        const source = node.source as CatalogSource
        if (source === "providers") {
          return closed({ ...state, provider: selected.label })
        }
        if (source === "models" || source === "localModels") {
          return closed({ ...state, model: selected.label })
        }
        if (source === "programs") {
          return closed({ ...state, runtime: selected.label, provider: selected.label })
        }
      }

      // Painéis informativos apenas fecham: nada é enviado ao host.
      return closed(state)
    }

    case "escape":
      if (state.apiKey) return cancelApiKey(state)
      if (state.path.length > 0) {
        return {
          ...state,
          path: state.path.slice(0, -1),
          query: "",
          index: 0,
          localAddBrowse: false
        }
      }
      return closed(state)

    case "close":
      return closed(state)
  }
}
