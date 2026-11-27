// Catálogo estático de demonstração visual. Nada aqui é enviado ao host.
// Ids, rótulos e limites vêm de constants.ts; aqui só a composição dos catálogos.
import {
  COMMANDS,
  GLYPHS,
  LIMITS,
  MODELS,
  PROVIDERS as PROVIDER_DATA,
  TEXT
} from "../constants"

export type CatalogSource =
  | typeof COMMANDS.catalogKinds.providers
  | typeof COMMANDS.catalogKinds.models
  | typeof COMMANDS.catalogKinds.localModels
  | typeof COMMANDS.catalogKinds.programs
  | typeof COMMANDS.catalogKinds.agents
  | typeof COMMANDS.catalogKinds.behaviors
  | typeof COMMANDS.catalogKinds.tools
  | typeof COMMANDS.catalogKinds.sessions
  | typeof COMMANDS.catalogKinds.appearances

export type InfoSource =
  | typeof COMMANDS.catalogKinds.providerStatus
  | typeof COMMANDS.catalogKinds.localScan
  | typeof COMMANDS.catalogKinds.toolSafety
  | typeof COMMANDS.catalogKinds.sessionNew
  | typeof COMMANDS.catalogKinds.sessionClear
  | typeof COMMANDS.catalogKinds.sessionSummarize
  | typeof COMMANDS.catalogKinds.systemSettings
  | typeof COMMANDS.catalogKinds.systemHealth

export interface CatalogItem {
  readonly id: string
  readonly label: string
  readonly detail: string
  readonly group: string
  readonly providerId?: string
}

export interface SelectionGroup {
  readonly title: string
  items: CatalogItem[]
}

export interface SettingInfo {
  readonly label: string
  readonly detail: string
}

export const PROVIDER_CATALOG: readonly CatalogItem[] = [
  {
    id: PROVIDER_DATA.ids.ollama,
    label: PROVIDER_DATA.labels.ollamaLocal,
    detail: PROVIDER_DATA.details.localDefault,
    group: PROVIDER_DATA.groups.local
  },
  {
    id: PROVIDER_DATA.ids.nvidia,
    label: PROVIDER_DATA.labels.nvidia,
    detail: PROVIDER_DATA.details.remoteDemo,
    group: PROVIDER_DATA.groups.remote
  },
  {
    id: PROVIDER_DATA.ids.opencode,
    label: PROVIDER_DATA.labels.openCode,
    detail: PROVIDER_DATA.details.remoteDemo,
    group: PROVIDER_DATA.groups.remote
  },
  {
    id: MODELS.ids.qwen38Local,
    label: MODELS.labels.qwen38Local,
    detail: PROVIDER_DATA.details.localModel,
    group: PROVIDER_DATA.groups.modelsPc,
    providerId: PROVIDER_DATA.ids.pc
  },
  {
    id: MODELS.ids.qwen36Iq2,
    label: MODELS.labels.qwen36Iq2,
    detail: PROVIDER_DATA.details.localIq2,
    group: PROVIDER_DATA.groups.modelsPc,
    providerId: PROVIDER_DATA.ids.pc
  },
  {
    id: MODELS.ids.qwen25_3b,
    label: MODELS.labels.qwen25_3b,
    detail: PROVIDER_DATA.details.ollamaModel,
    group: PROVIDER_DATA.groups.ollamaUnquantized,
    providerId: PROVIDER_DATA.ids.ollama
  },
  {
    id: MODELS.ids.qwen25_3bQ4,
    label: MODELS.labels.qwen25_3bQ4,
    detail: PROVIDER_DATA.details.ollamaQ4,
    group: PROVIDER_DATA.groups.ollamaQuantized,
    providerId: PROVIDER_DATA.ids.ollama
  },
  {
    id: MODELS.ids.llama32_3bQ4,
    label: MODELS.labels.llama32_3bQ4,
    detail: PROVIDER_DATA.details.ollamaQ40,
    group: PROVIDER_DATA.groups.ollamaQuantized,
    providerId: PROVIDER_DATA.ids.ollama
  },
  {
    id: MODELS.ids.llama32_3b,
    label: MODELS.labels.llama32_3b,
    detail: PROVIDER_DATA.details.ollamaModel,
    group: PROVIDER_DATA.groups.ollamaUnquantized,
    providerId: PROVIDER_DATA.ids.ollama
  },
  {
    id: MODELS.ids.deepseekR1Q4,
    label: MODELS.labels.deepseekR1Q4,
    detail: PROVIDER_DATA.details.ollamaQ4,
    group: PROVIDER_DATA.groups.ollamaQuantized,
    providerId: PROVIDER_DATA.ids.ollama
  },
  {
    id: MODELS.ids.phi4,
    label: MODELS.labels.phi4,
    detail: PROVIDER_DATA.details.ollamaModel,
    group: PROVIDER_DATA.groups.ollamaUnquantized,
    providerId: PROVIDER_DATA.ids.ollama
  }
]

export const PROVIDERS: readonly CatalogItem[] = PROVIDER_CATALOG.filter(
  item => item.group.startsWith(PROVIDER_DATA.groups.providerPrefix)
)

export const LOCAL_MODELS: readonly CatalogItem[] = PROVIDER_CATALOG.filter(
  item =>
    item.group.includes(PROVIDER_DATA.groups.modelsMarker) ||
    item.group.startsWith(PROVIDER_DATA.groups.ollamaMarker)
)

export const PROGRAMS: readonly CatalogItem[] = [
  {
    id: PROVIDER_DATA.ids.ollama,
    label: PROVIDER_DATA.labels.ollama,
    detail: PROVIDER_DATA.details.ollamaServer,
    group: PROVIDER_DATA.groups.programs
  },
  {
    id: PROVIDER_DATA.ids.lmStudio,
    label: PROVIDER_DATA.labels.lmStudio,
    detail: PROVIDER_DATA.details.lmStudio,
    group: PROVIDER_DATA.groups.programs
  },
  {
    id: PROVIDER_DATA.ids.llamaCpp,
    label: PROVIDER_DATA.labels.llamaCpp,
    detail: PROVIDER_DATA.details.llamaCpp,
    group: PROVIDER_DATA.groups.programs
  },
  {
    id: PROVIDER_DATA.ids.onnx,
    label: PROVIDER_DATA.labels.onnx,
    detail: PROVIDER_DATA.details.onnx,
    group: PROVIDER_DATA.groups.programs
  }
]

export const MODEL_PROVIDERS: readonly CatalogItem[] = [
  ...PROVIDERS,
  {
    id: PROVIDER_DATA.ids.pc,
    label: PROVIDER_DATA.labels.pc,
    detail: PROVIDER_DATA.details.pcModels,
    group: PROVIDER_DATA.groups.local
  }
]

const CATALOGS: Record<CatalogSource, readonly CatalogItem[]> = {
  providers: PROVIDERS,
  models: PROVIDER_CATALOG.filter(item => item.providerId !== undefined),
  localModels: LOCAL_MODELS,
  programs: PROGRAMS,
  agents: PROVIDER_DATA.catalog.agents.map(item => ({
    ...item,
    group: PROVIDER_DATA.groups.agents
  })),
  behaviors: PROVIDER_DATA.catalog.behaviors.map(item => ({
    ...item,
    group: PROVIDER_DATA.groups.behaviors
  })),
  tools: PROVIDER_DATA.catalog.tools.map(item => ({
    ...item,
    group: PROVIDER_DATA.groups.tools
  })),
  sessions: PROVIDER_DATA.catalog.sessions.map(item => ({
    ...item,
    group: PROVIDER_DATA.groups.sessions
  })),
  appearances: PROVIDER_DATA.catalog.appearances.map(item => ({
    ...item,
    group: PROVIDER_DATA.groups.appearances
  }))
}

export const INFO_PANELS: Record<InfoSource, readonly SettingInfo[]> = {
  providerStatus: [
    { label: TEXT.menuInfo.connection, detail: TEXT.menuInfo.connectionDetail },
    { label: TEXT.menuInfo.hostSend, detail: TEXT.menuInfo.hostSendDetail }
  ],
  localScan: [
    { label: TEXT.menuInfo.scan, detail: TEXT.menuInfo.scanDetail },
    { label: TEXT.menuInfo.filesRead, detail: TEXT.menuInfo.none }
  ],
  toolSafety: [
    { label: TEXT.menuInfo.sandbox, detail: TEXT.menuInfo.sandboxDetail },
    { label: TEXT.menuInfo.approval, detail: TEXT.menuInfo.approvalDetail }
  ],
  sessionNew: [{ label: TEXT.menuInfo.newSession, detail: TEXT.menuInfo.demoOnly }],
  sessionClear: [{ label: TEXT.menuInfo.clearSession, detail: TEXT.menuInfo.demoOnly }],
  sessionSummarize: [{ label: TEXT.menuInfo.summarizeSession, detail: TEXT.menuInfo.demoOnly }],
  systemSettings: [
    {
      label: TEXT.menuInfo.history,
      detail: `${LIMITS.historyMessages}${TEXT.menuInfo.messagesUnit}`
    },
    {
      label: TEXT.menuInfo.contextWindow,
      detail: `${LIMITS.contextTokens.toLocaleString(TEXT.menuInfo.numberLocale)}${TEXT.menuInfo.contextWindowDetail}`
    },
    {
      label: TEXT.menuInfo.iterations,
      detail: `${LIMITS.toolIterationsPerTurn}${TEXT.menuInfo.iterationsDetail}`
    },
    {
      label: TEXT.menuInfo.tools,
      detail: PROVIDER_DATA.catalog.tools
        .slice(0, LIMITS.visibleToolRows)
        .map(tool => tool.label)
        .join(TEXT.common.commaSpace)
    },
    { label: TEXT.menuInfo.transport, detail: TEXT.menuInfo.transportDetail }
  ],
  systemHealth: [
    { label: TEXT.menuInfo.tui, detail: TEXT.menuInfo.tuiDetail },
    { label: TEXT.menuInfo.backend, detail: TEXT.menuInfo.backendDetail }
  ]
}

export const SETTINGS_INFO: readonly SettingInfo[] = INFO_PANELS.systemSettings

export const LOCAL_ADD_OPTION: CatalogItem = {
  id: PROVIDER_DATA.catalog.localAdd.id,
  label: PROVIDER_DATA.catalog.localAdd.label,
  detail: PROVIDER_DATA.catalog.localAdd.detail,
  group: PROVIDER_DATA.groups.localAdd
}

export function catalogFor(source: CatalogSource): readonly CatalogItem[] {
  return CATALOGS[source]
}

export function filterCatalogItems(
  items: readonly CatalogItem[],
  query: string
): CatalogItem[] {
  const term = query.trim().toLowerCase()
  if (term.length === 0) {
    return [...items]
  }

  return items.filter(item =>
    `${item.label}${GLYPHS.space}${item.detail}${GLYPHS.space}${item.group}`
      .toLowerCase()
      .includes(term))
}

export function filterProviderCatalog(query: string): CatalogItem[] {
  return filterCatalogItems(PROVIDER_CATALOG, query)
}

// Agrupa depois de filtrar: a ordem dos subtítulos segue a primeira ocorrência.
export function groupCatalogItems(
  items: readonly CatalogItem[],
  query: string
): SelectionGroup[] {
  const groups: SelectionGroup[] = []

  for (const item of filterCatalogItems(items, query)) {
    let group = groups.find(entry => entry.title === item.group)
    if (!group) {
      group = { title: item.group, items: [] }
      groups.push(group)
    }
    group.items.push(item)
  }

  return groups
}
