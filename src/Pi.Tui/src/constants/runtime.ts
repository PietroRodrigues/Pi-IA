// Valores de execução da TUI: store, statuses, tempo e limites.

// Origem: mensagens do modelo e políticas de exibição
// Finalidade: papéis e status usados no store e na barra de estado.
export const ROLES = {
  user: "user",
  assistant: "assistant",
  system: "system",
  info: "info",
  thought: "thought",
  tool: "tool"
} as const

// Origem: ciclo de vida de provedores e ferramentas
// Finalidade: rótulos de status exibidos na interface.
export const STATUSES = {
  connecting: "conectando…",
  ready: "pronto",
  thinking: "pensando…",
  cancelling: "cancelando…",
  cancelled: "cancelado",
  contextCleared: "contexto limpo",
  error: "erro",
  approval: "aprovação",
  tool: "ferramenta",
  toolDone: "concluída",
  toolFailed: "falhou",
  approvalRegistered: "aprovação registrada",
  backendStopped: "backend encerrado",
  backendStoppedWithSignal: "backend encerrou (",
  signalFallback: "sinal",
  thinkingMatch: "pensand",
  cancelledMarker: "[cancelado]"
} as const

// Origem: temporizadores da interface e do clipboard
// Finalidade: delays, debounce e intervalos em milissegundos.
export const TIME = {
  millisecondsPerSecond: 1000,
  backendKillDelayMs: 1000,
  rendererCloseTimeoutMs: 1000,
  environmentTimeoutMs: 800,
  clipboardTimeoutMs: 800,
  noticeDurationMs: 1800,
  ctrlCDebounceMs: 80,
  thinkingIntervalMs: 350,
  targetFps: 30
} as const

// Origem: limites de contexto, busca e saída
// Finalidade: tetos numéricos usados nos reducers e na telemetria.
export const LIMITS = {
  contextTokens: 32768,
  historyMessages: 20,
  toolIterationsPerTurn: 4,
  visibleToolRows: 3,
  stderrTailChars: 4000,
  systemTailChars: 500,
  approvalInputChars: 160,
  gitHashChars: 8,
  scrollBottomTolerance: 2,
  decimalPlaces: 1,
  minimumRows: 1,
  zero: 0,
  one: 1,
  two: 2,
  seven: 7,
  eight: 8,
  twenty: 20,
  oneHundred: 100,
  fourThousand: 4000
} as const
