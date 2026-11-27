// Comandos, teclas e identificadores do menu "/".

import { COMMAND_TREE } from "./commandTree"

// Origem: atalhos do menu e ações internas
// Finalidade: teclas, sinais, fontes e ações do menu local.
export const COMMANDS = {
  slash: {
    prefix: "/",
    root: "/",
    providers: "/providers",
    providersList: "/providers/list",
    providersConnect: "/providers/connect",
    providersStatus: "/providers/status",
    models: "/models",
    modelsProviders: "/models/providers",
    modelsActive: "/models/active",
    localModels: "/local-models",
    localModelsApp: "/local-models/app",
    localModelsList: "/local-models/list",
    localModelsAdd: "/local-models/add",
    localModelsScan: "/local-models/scan",
    agents: "/agents",
    agentsList: "/agents/list",
    agentsBehavior: "/agents/behavior",
    session: "/session",
    sessionList: "/session/list",
    sessionNew: "/session/new",
    sessionClear: "/session/clear",
    sessionSummarize: "/session/summarize",
    system: "/system",
    systemAppearance: "/system/appearance",
    systemSettings: "/system/settings",
    systemSettingsCurrent: "/system/settings/current",
    systemSettingsTools: "/system/settings/tools",
    systemSettingsToolsList: "/system/settings/tools/list",
    systemSettingsToolsSafety: "/system/settings/tools/safety",
    systemHealth: "/system/health",
    exit: ["/sair", "/exit"]
  },
  segments: {
    providers: "providers",
    list: "list",
    connect: "connect",
    status: "status",
    models: "models",
    active: "active",
    localModels: "local-models",
    app: "app",
    add: "add",
    scan: "scan",
    agents: "agents",
    behavior: "behavior",
    tools: "tools",
    safety: "safety",
    current: "current",
    session: "session",
    new: "new",
    clear: "clear",
    summarize: "summarize",
    appearance: "appearance",
    settings: "settings",
    health: "health"
  },
  adminSources: {
    windows: "windows-net-session",
    posix: "posix-uid",
    unknown: "unknown"
  },
  platforms: {
    windows: "win32"
  },
  process: {
    dotnet: "dotnet",
    build: "build",
    configurationFlag: "--configuration",
    debugConfiguration: "Debug",
    bridge: "--bridge",
    stdio: "stdio",
    net: "net",
    netSession: "session",
    clip: "clip"
  },
  ctrlC: {
    copy: "copy",
    cancel: "cancel",
    none: "none"
  },
  keys: {
    approve: "y",
    approveUpper: "Y",
    deny: "n",
    denyUpper: "N",
    escape: "escape",
    esc: "esc",
    focusInput: "p",
    clearContext: "l",
    copy: "c",
    up: "up",
    down: "down",
    left: "left",
    right: "right"
  },
  signals: {
    interrupt: "SIGINT",
    terminate: "SIGTERM",
    hangup: "SIGHUP",
    break: "SIGBREAK",
    kill: "SIGKILL",
    exitSignals: ["SIGINT", "SIGTERM", "SIGHUP", "SIGBREAK"]
  },
  // Identificadores internos de ações/kinds: literais de código, não textos de UI.
  actionKinds: {
    event: "event",
    submit: "submit",
    cancel: "cancel",
    decision: "decision",
    clear: "clear",
    status: "status",
    jump: "jump",
    system: "system",
    type: "type",
    up: "up",
    down: "down",
    enter: "enter",
    escape: "escape",
    close: "close",
    apiKeyType: "apiKeyType",
    apiKeySubmit: "apiKeySubmit",
    apiKeyCancel: "apiKeyCancel",
    localAddType: "localAddType"
  },
  nodeKinds: {
    group: "group",
    selection: "selection",
    connect: "connect",
    localAdd: "local-add",
    info: "info",
    root: "root"
  },
  catalogKinds: {
    modelProviders: "modelProviders",
    providers: "providers",
    models: "models",
    localModels: "localModels",
    programs: "programs",
    agents: "agents",
    behaviors: "behaviors",
    tools: "tools",
    sessions: "sessions",
    appearances: "appearances",
    providerStatus: "providerStatus",
    localScan: "localScan",
    toolSafety: "toolSafety",
    sessionNew: "sessionNew",
    sessionClear: "sessionClear",
    sessionSummarize: "sessionSummarize",
    systemSettings: "systemSettings",
    systemHealth: "systemHealth",
    provider: "provider",
    agent: "agent",
    runtime: "runtime",
    connected: "connected",
    model: "model"
  },
  nodeEvents: {
    uncaughtException: "uncaughtException",
    unhandledRejection: "unhandledRejection"
  },
  // Valores aceitos por APIs externas (OpenTUI/Node) e por chaves dinâmicas.
  apiValues: {
    column: "column",
    row: "row",
    flexEnd: "flex-end",
    right: "right",
    single: "single",
    regexCaseInsensitive: "i",
    functionType: "function"
  },
  tree: COMMAND_TREE
} as const
