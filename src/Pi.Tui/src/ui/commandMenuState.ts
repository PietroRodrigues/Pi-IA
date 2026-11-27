// Estado puramente local do menu "/". Nenhuma ação altera o host, o store de
// sessão ou o protocolo: tudo aqui é demonstração visual da TUI.
import {
  COMMANDS,
  GLYPHS,
  LAYOUT,
  LIMITS,
  MODELS as MODEL_DATA,
  TEXT,
  TOOLS
} from "../constants"
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
  readonly enabledTools: readonly string[]
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
  query: TEXT.common.empty,
  index: LIMITS.zero,
  provider: PROVIDERS[LIMITS.zero]?.label ?? MODEL_DATA.fallbacks.provider,
  model: LOCAL_MODELS[LIMITS.zero]?.label ?? MODEL_DATA.fallbacks.model,
  runtime: MODEL_DATA.fallbacks.runtime,
  connectedProviders: [],
  apiKey: undefined,
  localAddPath: TEXT.common.empty,
  localAddBrowse: false,
  enabledTools: [...TOOLS.enabledByDefault]
}

export type CommandMenuAction =
  | { kind: typeof COMMANDS.actionKinds.type; value: string }
  | { kind: typeof COMMANDS.actionKinds.up }
  | { kind: typeof COMMANDS.actionKinds.down }
  | { kind: typeof COMMANDS.actionKinds.enter }
  | { kind: typeof COMMANDS.actionKinds.escape }
  | { kind: typeof COMMANDS.actionKinds.close }
  | { kind: typeof COMMANDS.actionKinds.apiKeyType; value: string }
  | { kind: typeof COMMANDS.actionKinds.apiKeySubmit }
  | { kind: typeof COMMANDS.actionKinds.apiKeyCancel }
  | { kind: typeof COMMANDS.actionKinds.localAddType; value: string }

function closed(state: CommandMenuState): CommandMenuState {
  return {
    ...state,
    open: false,
    path: [],
    query: TEXT.common.empty,
    index: LIMITS.zero,
    apiKey: undefined,
    localAddBrowse: false
  }
}

function toggleTool(state: CommandMenuState, id: string): CommandMenuState {
  const enabled = state.enabledTools.includes(id)
  return {
    ...state,
    enabledTools: enabled
      ? state.enabledTools.filter(tool => tool !== id)
      : [...state.enabledTools, id]
  }
}

function catalogItemsFor(state: CommandMenuState, source: CatalogSource): CatalogItem[] {
  const items = [...catalogFor(source)]

  // /models/<provider> mostra apenas os modelos daquele provedor.
  if (
    source === COMMANDS.catalogKinds.models &&
    state.path[LIMITS.zero] === COMMANDS.segments.models &&
    state.path.length > LIMITS.one
  ) {
    const providerId = state.path[state.path.length - LIMITS.one]
    if (
      providerId !== COMMANDS.segments.active &&
      providerId !== COMMANDS.segments.providers
    ) {
      return items.filter(item => item.providerId === providerId)
    }
  }

  return items
}

function isActive(state: CommandMenuState, source: CatalogSource, item: CatalogItem): boolean {
  switch (source) {
    case COMMANDS.catalogKinds.providers:
      return item.label === state.provider || state.connectedProviders.includes(item.id)
    case COMMANDS.catalogKinds.models:
    case COMMANDS.catalogKinds.localModels:
      return item.label === state.model
    case COMMANDS.catalogKinds.programs:
      return item.label === state.runtime
    default:
      return false
  }
}

function infoItems(state: CommandMenuState, source: InfoSource): CommandMenuItem[] {
  const dynamic: CommandMenuItem[] = []

  if (source === COMMANDS.catalogKinds.providerStatus) {
    dynamic.push(
      {
        id: COMMANDS.catalogKinds.provider,
        label: TEXT.menuInfo.activeProvider,
        detail: state.provider,
        active: true
      },
      {
        id: COMMANDS.catalogKinds.runtime,
        label: TEXT.menuInfo.pcProgram,
        detail: state.runtime,
        active: true
      },
      {
        id: COMMANDS.catalogKinds.connected,
        label: TEXT.menuInfo.connectedVisual,
        detail: state.connectedProviders.join(TEXT.common.commaSpace) || TEXT.menuInfo.none,
        active: state.connectedProviders.length > LIMITS.zero
      }
    )
  }

  if (source === COMMANDS.catalogKinds.systemSettings) {
    dynamic.push(
      {
        id: COMMANDS.catalogKinds.provider,
        label: TEXT.menuInfo.activeProvider,
        detail: state.provider,
        active: true
      },
      {
        id: COMMANDS.catalogKinds.model,
        label: TEXT.menuInfo.activeModel,
        detail: state.model,
        active: true
      }
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
  if (term.length === LIMITS.zero) return rows
  return rows.filter(row =>
    `${row.label}${GLYPHS.space}${row.detail}`.toLowerCase().includes(term))
}

export function visibleItems(state: CommandMenuState): CommandMenuItem[] {
  if (state.apiKey) return []

  if (state.path.length === LIMITS.zero) {
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

  if (node.kind === COMMANDS.nodeKinds.group) {
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

    if (term.length === LIMITS.zero) return children
    return children.filter(child =>
      `${child.command}${GLYPHS.space}${child.label}${GLYPHS.space}${child.detail}`
        .toLowerCase()
        .includes(term))
  }

  if (node.kind === COMMANDS.nodeKinds.localAdd) {
    return [
      {
        id: LOCAL_ADD_OPTION.id,
        label: LOCAL_ADD_OPTION.label,
        detail: LOCAL_ADD_OPTION.detail,
        active: state.localAddBrowse
      }
    ]
  }

  if (node.kind === COMMANDS.nodeKinds.info) {
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
  if (
    !node ||
    (node.kind !== COMMANDS.nodeKinds.selection && node.kind !== COMMANDS.nodeKinds.connect)
  ) {
    return []
  }
  return groupCatalogItems(catalogItemsFor(state, node.source as CatalogSource), state.query)
}

export function menuHeaderLabel(state: CommandMenuState): string {
  return `${pathLabel(state.path)}${TEXT.common.separator}${visibleItems(state).length}${TEXT.menu.headerSuffix}`
}

export function currentBreadcrumb(state: CommandMenuState): string {
  const filter =
    state.query.trim().length > LIMITS.zero
      ? `${TEXT.menu.filterPrefix}${state.query.trim()}${TEXT.common.quote}`
      : TEXT.common.empty
  return `${menuHeaderLabel(state)}${filter}`
}

function openApiKey(state: CommandMenuState, selected: CommandMenuItem): CommandMenuState {
  const node = currentNode(state)
  const source = (node?.source ?? COMMANDS.catalogKinds.providers) as CatalogSource
  const provider = catalogItemsFor(state, source).find(item => item.id === selected.id)
  const label = provider?.label ?? selected.label

  return {
    ...state,
    query: TEXT.common.empty,
    index: LIMITS.zero,
    apiKey: {
      provider: label,
      providerId: provider?.id ?? selected.id,
      value: TEXT.common.empty,
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
    query: TEXT.common.empty,
    index: LIMITS.zero,
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
    query: TEXT.common.empty,
    index: LIMITS.zero,
    apiKey: undefined
  }
}

export function reduceCommandMenu(
  state: CommandMenuState,
  action: CommandMenuAction
): CommandMenuState {
  switch (action.kind) {
    case COMMANDS.actionKinds.type:
      if (state.apiKey) return { ...state, apiKey: { ...state.apiKey, value: action.value } }
      return { ...state, open: true, query: action.value, index: LIMITS.zero }

    case COMMANDS.actionKinds.apiKeyType:
      if (!state.apiKey) return state
      return { ...state, apiKey: { ...state.apiKey, value: action.value } }

    case COMMANDS.actionKinds.localAddType:
      return { ...state, localAddPath: action.value }

    case COMMANDS.actionKinds.apiKeySubmit:
      return submitApiKey(state)

    case COMMANDS.actionKinds.apiKeyCancel:
      return cancelApiKey(state)

    case COMMANDS.actionKinds.up:
    case COMMANDS.actionKinds.down:
      if (state.apiKey) return state
      return {
        ...state,
        index: moveSelection(
          state.index,
          visibleItems(state).length,
          action.kind === COMMANDS.actionKinds.up ? LAYOUT.cursorStart : LIMITS.one
        )
      }

    case COMMANDS.actionKinds.enter: {
      if (state.apiKey) return submitApiKey(state)

      const items = visibleItems(state)
      const selected = items[state.index]
      if (!selected) return state

      const node = currentNode(state)
      if (!node) {
        const command = filterCommands(state.query).find(entry => entry.command === selected.id)
        if (!command) return state
        return {
          ...state,
          path: [command.segment],
          query: TEXT.common.empty,
          index: LIMITS.zero
        }
      }

      if (node.kind === COMMANDS.nodeKinds.group) {
        const child = childrenOf(node).find(entry => entry.command === selected.id)
        if (!child) return state
        return {
          ...state,
          path: [...state.path, child.segment],
          query: TEXT.common.empty,
          index: LIMITS.zero
        }
      }

      if (node.kind === COMMANDS.nodeKinds.connect) {
        return openApiKey(state, selected)
      }

      if (node.kind === COMMANDS.nodeKinds.localAdd) {
        return { ...state, localAddBrowse: !state.localAddBrowse }
      }

      if (node.kind === COMMANDS.nodeKinds.selection) {
        const source = node.source as CatalogSource
        if (source === COMMANDS.catalogKinds.tools) {
          return toggleTool(state, selected.id)
        }
        if (source === COMMANDS.catalogKinds.providers) {
          return closed({ ...state, provider: selected.label })
        }
        if (
          source === COMMANDS.catalogKinds.models ||
          source === COMMANDS.catalogKinds.localModels
        ) {
          return closed({ ...state, model: selected.label })
        }
        if (source === COMMANDS.catalogKinds.programs) {
          return closed({ ...state, runtime: selected.label, provider: selected.label })
        }
      }

      // Painéis informativos apenas fecham: nada é enviado ao host.
      return closed(state)
    }

    case COMMANDS.actionKinds.escape:
      if (state.apiKey) return cancelApiKey(state)
      if (state.path.length > LIMITS.zero) {
        return {
          ...state,
          path: state.path.slice(LIMITS.zero, LAYOUT.cursorStart),
          query: TEXT.common.empty,
          index: LIMITS.zero,
          localAddBrowse: false
        }
      }
      return closed(state)

    case COMMANDS.actionKinds.close:
      return closed(state)
  }
}
