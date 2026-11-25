// Catálogo estático de demonstração visual. Nada aqui é enviado ao host.
export type CatalogSource =
  | "providers"
  | "models"
  | "localModels"
  | "programs"
  | "agents"
  | "behaviors"
  | "tools"
  | "sessions"
  | "appearances"

export type InfoSource =
  | "providerStatus"
  | "localScan"
  | "toolSafety"
  | "sessionNew"
  | "sessionClear"
  | "sessionSummarize"
  | "systemSettings"
  | "systemHealth"

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
  { id: "ollama", label: "Ollama (Local)", detail: "provedor local · padrão", group: "Provedores locais" },
  { id: "nvidia", label: "NVIDIA", detail: "provedor remoto · demonstração", group: "Provedores remotos" },
  { id: "opencode", label: "OpenCode", detail: "provedor remoto · demonstração", group: "Provedores remotos" },
  {
    id: "qwen-local",
    label: "qwen3.8-2b-local",
    detail: "modelo local · não quantizado",
    group: "Modelos no PC",
    providerId: "pc"
  },
  {
    id: "qwen-iq2",
    label: "qwen3-6b-iq2-local",
    detail: "modelo local · IQ2",
    group: "Modelos no PC",
    providerId: "pc"
  },
  {
    id: "qwen-3b",
    label: "qwen2.5:3b",
    detail: "Ollama · não quantizado",
    group: "Ollama · não quantizados",
    providerId: "ollama"
  },
  {
    id: "qwen-3b-q4",
    label: "qwen2.5:3b-q4_K_M",
    detail: "Ollama · Q4_K_M",
    group: "Ollama · quantizados",
    providerId: "ollama"
  },
  {
    id: "llama-q4",
    label: "llama3.2:3b-q4_0",
    detail: "Ollama · Q4_0",
    group: "Ollama · quantizados",
    providerId: "ollama"
  },
  {
    id: "llama-3b",
    label: "llama3.2:3b",
    detail: "Ollama · não quantizado",
    group: "Ollama · não quantizados",
    providerId: "ollama"
  },
  {
    id: "deepseek-q4",
    label: "deepseek-r1:7b-q4_K_M",
    detail: "Ollama · Q4_K_M",
    group: "Ollama · quantizados",
    providerId: "ollama"
  },
  {
    id: "phi4",
    label: "phi4:14b",
    detail: "Ollama · não quantizado",
    group: "Ollama · não quantizados",
    providerId: "ollama"
  }
]

export const PROVIDERS: readonly CatalogItem[] = PROVIDER_CATALOG.filter(
  item => item.group.startsWith("Provedores")
)

export const LOCAL_MODELS: readonly CatalogItem[] = PROVIDER_CATALOG.filter(
  item => item.group.includes("Modelos") || item.group.startsWith("Ollama")
)

export const PROGRAMS: readonly CatalogItem[] = [
  { id: "ollama", label: "Ollama", detail: "servidor local", group: "Programas no PC" },
  { id: "lm-studio", label: "LM Studio", detail: "interface local", group: "Programas no PC" },
  { id: "llama-cpp", label: "llama.cpp", detail: "execução direta", group: "Programas no PC" },
  { id: "onnx", label: "ONNX Runtime", detail: "modelos ONNX", group: "Programas no PC" }
]

export const MODEL_PROVIDERS: readonly CatalogItem[] = [
  ...PROVIDERS,
  { id: "pc", label: "PC (local)", detail: "modelos baixados no PC", group: "Provedores locais" }
]

const CATALOGS: Record<CatalogSource, readonly CatalogItem[]> = {
  providers: PROVIDERS,
  models: PROVIDER_CATALOG.filter(item => item.providerId !== undefined),
  localModels: LOCAL_MODELS,
  programs: PROGRAMS,
  agents: [
    { id: "leitor", label: "Leitor", detail: "só leitura", group: "Agentes" },
    { id: "escritor", label: "Escritor", detail: "edita arquivos", group: "Agentes" },
    { id: "revisor", label: "Revisor", detail: "revisa mudanças", group: "Agentes" }
  ],
  behaviors: [
    { id: "curto", label: "Curto", detail: "respostas breves", group: "Comportamento" },
    { id: "equilibrado", label: "Equilibrado", detail: "padrão visual", group: "Comportamento" },
    { id: "detalhado", label: "Detalhado", detail: "mais contexto", group: "Comportamento" }
  ],
  tools: [
    { id: "read_file", label: "read_file", detail: "leitura", group: "Ferramentas" },
    { id: "edit_file", label: "edit_file", detail: "escrita", group: "Ferramentas" },
    { id: "search_code", label: "search_code", detail: "busca", group: "Ferramentas" },
    { id: "execute_shell", label: "execute_shell", detail: "desativada", group: "Ferramentas" }
  ],
  sessions: [
    { id: "atual", label: "Atual", detail: "em uso", group: "Sessões" },
    { id: "anterior", label: "Anterior", detail: "visual", group: "Sessões" },
    { id: "arquivada", label: "Arquivada", detail: "visual", group: "Sessões" }
  ],
  appearances: [
    { id: "ambar", label: "Âmbar", detail: "padrão", group: "Aparência" },
    { id: "cinza", label: "Cinza", detail: "neutro", group: "Aparência" },
    { id: "contraste", label: "Alto contraste", detail: "acessível", group: "Aparência" }
  ]
}

export const INFO_PANELS: Record<InfoSource, readonly SettingInfo[]> = {
  providerStatus: [
    { label: "Conexão", detail: "demonstração visual" },
    { label: "Envio ao host", detail: "nenhum por enquanto" }
  ],
  localScan: [
    { label: "Varredura", detail: "simulada na TUI" },
    { label: "Arquivos lidos", detail: "nenhum" }
  ],
  toolSafety: [
    { label: "Sandbox", detail: "visual · sem execução" },
    { label: "Aprovação", detail: "Y / N / Esc" }
  ],
  sessionNew: [{ label: "Nova sessão", detail: "apenas demonstração visual" }],
  sessionClear: [{ label: "Limpar", detail: "apenas demonstração visual" }],
  sessionSummarize: [{ label: "Resumo", detail: "apenas demonstração visual" }],
  systemSettings: [
    { label: "Histórico mantido", detail: "20 mensagens" },
    { label: "Janela de contexto", detail: "128.000 tokens (visual)" },
    { label: "Iterações de ferramenta", detail: "4 por turno" },
    { label: "Ferramentas", detail: "read_file, edit_file, search_code" },
    { label: "Transporte", detail: "NDJSON v1 · demonstração, sem envio" }
  ],
  systemHealth: [
    { label: "TUI", detail: "ativa" },
    { label: "Backend", detail: "não consultado nesta demo" }
  ]
}

export const SETTINGS_INFO: readonly SettingInfo[] = INFO_PANELS.systemSettings

export const LOCAL_ADD_OPTION: CatalogItem = {
  id: "add-local-model",
  label: "Adicionar modelo do PC",
  detail: "cole um caminho ou busque um diretório",
  group: "No PC"
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
    `${item.label} ${item.detail} ${item.group}`.toLowerCase().includes(term))
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
