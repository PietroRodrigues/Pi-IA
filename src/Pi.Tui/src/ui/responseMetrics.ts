// Resumo exibido ao final de cada resposta do assistente. O host não fornece
// tempo/tok/s nesta demo: medimos localmente e usamos fallback seguro.
import { LIMITS, ROLES, TEXT, TIME } from "../constants"
import type { ChatMessage } from "../state/reducers"

export interface ResponseSummaryInput {
  readonly agent: string
  readonly model: string
  readonly elapsedMs: number
  readonly tokens: number
}

export function formatDuration(ms: number): string {
  if (!Number.isFinite(ms) || ms < 0) return TEXT.metrics.fallbackDuration
  if (ms < TIME.millisecondsPerSecond) {
    return `${Math.round(ms)}${TEXT.metrics.millisecondsUnit}`
  }
  return `${(ms / TIME.millisecondsPerSecond).toFixed(LIMITS.decimalPlaces)}${TEXT.metrics.secondsUnit}`
}

export function tokenDelta(used: number, start: number | undefined): number {
  if (!Number.isFinite(used) || start === undefined || !Number.isFinite(start)) return LIMITS.zero
  return Math.max(LIMITS.zero, used - start)
}

export function tokensPerSecond(tokens: number, elapsedMs: number): string {
  if (
    !Number.isFinite(tokens) ||
    !Number.isFinite(elapsedMs) ||
    tokens <= LIMITS.zero ||
    elapsedMs <= LIMITS.zero
  ) {
    return TEXT.metrics.fallbackRate
  }
  return (tokens / (elapsedMs / TIME.millisecondsPerSecond)).toFixed(LIMITS.decimalPlaces)
}

export function responseSummaryLine(input: ResponseSummaryInput): string {
  const agent = input.agent.trim() || TEXT.metrics.fallbackAgent
  const model = input.model.trim() || TEXT.metrics.fallbackModel
  return [
    agent,
    model,
    formatDuration(input.elapsedMs),
    `${tokensPerSecond(input.tokens, input.elapsedMs)}${TEXT.metrics.tokensPerSecondUnit}`
  ].join(TEXT.common.separator)
}

export function streamingResponseId(messages: readonly ChatMessage[]): string | undefined {
  return messages.find(
    message => message.role === ROLES.assistant && message.streaming
  )?.id
}

// Só devolve a mensagem quando o streaming terminou: nunca exibimos resumo
// no meio da geração nem para mensagens antigas vindas do snapshot.
export function finishedResponse(
  activeId: string | undefined,
  messages: readonly ChatMessage[]
): ChatMessage | undefined {
  if (!activeId) return undefined
  return messages.find(
    message =>
      message.id === activeId && message.role === ROLES.assistant && !message.streaming
  )
}
