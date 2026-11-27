// Textos exibidos na interface e mensagens de log da TUI.

// Origem: rótulos, avisos e mensagens de UI/log
// Finalidade: todas as strings em português usadas pelos componentes.
export const TEXT = {
  common: {
    empty: "",
    separator: " · ",
    commaSpace: ", ",
    ellipsis: "…",
    slash: "/",
    quote: '"',
    openBracket: "[",
    closeBracket: "]",
    newline: "\n"
  },
  ids: {
    separator: "-",
    prefixes: {
      user: "user",
      assistant: "assistant",
      system: "system",
      thought: "thought",
      tool: "tool",
      snapshot: "snapshot"
    }
  },
  environment: {
    shortcuts: "Ctrl+C copiar/cancelar · /sair encerrar · Ctrl+L limpar · Ctrl+P foco",
    currentDirectory: ".",
    headRefPattern: "^ref:\\s*refs/heads/(.+)$",
    multilineFlag: "m",
    unknownBranch: "—",
    adminBadge: "ADM",
    standardUser: "Usuário padrão",
    gitPrefix: " · git: ",
    headRefPrefix: "ref: refs/heads/",
    detachedPrefix: "HEAD@",
    detachedCommentPrefix: "#"
  },
  logs: {
    tuiMounted: "TUI montada",
    tuiStopping: "encerrando TUI",
    tuiStopped: "TUI encerrada",
    shutdownFailure: "falha no encerramento",
    backendStarted: "backend iniciado",
    backendStopping: "encerrando backend",
    backendExited: "backend encerrado",
    backendStartFailure: "falha ao iniciar o backend",
    backendError: "erro do backend",
    sending: "enviando",
    sessionStarted: "sessão iniciada",
    uncaughtException: "uncaughtException",
    unhandledRejection: "unhandledRejection",
    stderrPrefix: "[backend stderr]",
    timestampOpen: "[",
    timestampClose: "] "
  },
  errors: {
    backendNotFound: "Backend C# não encontrado: ",
    buildHint: "Rode: dotnet build Pi.sln --configuration Debug",
    backendStartFailed: "Falha ao iniciar o backend: ",
    invalidBackendLine: "Linha inválida do backend: "
  },
  session: {
    name: "Sessão Pi",
    errorTextPrefix: "[",
    errorTextSeparator: "] "
  },
  decisions: {
    approved: "aprovado pelo usuário",
    denied: "negado pelo usuário"
  },
  chat: {
    thoughtPrefix: "pensamento · ",
    toolFallbackName: "ferramenta",
    jumpLatest: "Jump to latest ↓",
    emptyTitle: "Pi",
    emptyHint: "Assistente local · Ollama · digite abaixo para começar",
    thinkingPrefix: "pensando",
    markdown: {
      fence: "```",
      tablePattern: "(^|\\n)\\s*\\|.*\\|\\s*(\\n|$)",
      headingPattern: "(^|\\n)\\s{0,3}#{1,6}\\s"
    }
  },
  input: {
    placeholderBusy: "Gerando… Esc para cancelar",
    placeholderIdle: "Digite sua mensagem..."
  },
  statusBar: {
    contextLabel: "Contexto: ",
    percent: "%",
    noticePrefix: "  "
  },
  menu: {
    title: "Menu visual",
    search: "Search ",
    searchPlaceholder: "digite no campo abaixo...",
    esc: "esc",
    noMatches: "Nenhum item corresponde ao filtro.",
    footer: "↑↓ navegar · ← voltar · → confirmar · Enter abrir · Esc fechar · demo sem envio",
    connectedSuffix: " · conectado: ",
    headerSuffix: " item(ns)",
    filterPrefix: ' · filtro: "',
    pathLabel: "Caminho: ",
    pinnedBrowse: "Busca visual · nenhum arquivo lido",
    pinnedIdle: "Enter: buscar diretório (visual)",
    selectionFooter: "↑↓ navegar · Enter selecionar · Esc voltar · filtro no campo",
    toolsFooter: "↑↓ navegar · Enter/→ alterna · Esc voltar",
    toolEnabled: "ativada",
    toolDisabled: "desativada",
    subtitles: {
      providers: "Provedores",
      models: "Modelos",
      localModels: "Modelos locais",
      "local-models": "Modelos locais",
      agents: "Agentes",
      session: "Sessões",
      system: "Sistema",
      settings: "Configurações",
      tools: "Ferramentas",
      current: "Configurações atuais",
      safety: "Regras de segurança"
    },
    sectionPrefix: "─ ",
    dynamicDescriptionPrefix: "Modelos de "
  },
  menuInfo: {
    activeProvider: "Provedor ativo",
    pcProgram: "Programa no PC",
    connectedVisual: "Conectados (visual)",
    activeModel: "Modelo ativo",
    none: "nenhum",
    connection: "Conexão",
    connectionDetail: "demonstração visual",
    hostSend: "Envio ao host",
    hostSendDetail: "nenhum por enquanto",
    scan: "Varredura",
    scanDetail: "simulada na TUI",
    filesRead: "Arquivos lidos",
    sandbox: "Sandbox",
    sandboxDetail: "visual · sem execução",
    approval: "Aprovação",
    approvalDetail: "Y / N / Esc",
    newSession: "Nova sessão",
    clearSession: "Limpar",
    summarizeSession: "Resumo",
    demoOnly: "apenas demonstração visual",
    history: "Histórico mantido",
    messagesUnit: " mensagens",
    numberLocale: "de-DE",
    contextWindow: "Janela de contexto",
    contextWindowDetail: " tokens (visual)",
    iterations: "Iterações de ferramenta",
    iterationsDetail: " por turno",
    tools: "Ferramentas",
    transport: "Transporte",
    transportDetail: "NDJSON v1 · demonstração, sem envio",
    tui: "TUI",
    tuiDetail: "ativa",
    detailSeparator: " - ",
    backend: "Backend",
    backendDetail: "não consultado nesta demo"
  },
  apiKey: {
    close: "esc",
    label: "API key ",
    placeholder: "digite apenas para a demonstração",
    submit: " Submit ",
    footer: "Enter confirma · nada é enviado ao host"
  },
  approval: {
    title: "Permitir esta ferramenta?",
    hint: "[Y] aprovar   [N/Esc] negar",
    riskLabels: {
      ReadOnly: "somente leitura",
      Write: "escrita",
      Shell: "comando"
    }
  },
  notices: {
    copied: "Copiado!",
    copyFailed: "Não foi possível copiar",
    turnCancelled: "Turno cancelado",
    exitHint: "Use /sair para encerrar",
    backendStoppedWithSignalClose: ")"
  },
  metrics: {
    fallbackDuration: "—",
    fallbackAgent: "agente",
    fallbackModel: "modelo",
    fallbackRate: "0.0",
    millisecondsUnit: " ms",
    secondsUnit: " s",
    tokensPerSecondUnit: " tok/s"
  }
} as const
