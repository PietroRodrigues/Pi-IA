// Resumo exibido ao final de cada resposta do assistente. O host não fornece
// tempo/tok/s nesta demo: medimos localmente e usamos fallback seguro.
import type { ChatMessage } from "../state/reducers"

export interface ResponseSummaryInput {
  readonly agent: string
  readonly model: string
  readonly elapsedMs: number
  readonly tokens: number
}

export function formatDuration(ms: number): string {
  if (!Number.isFinite(ms) || ms < 0) return "—"
  if (ms < 1000) return `${Math.round(ms)} ms`
  return `${(ms / 1000).toFixed(1)} s`
}

export function tokenDelta(used: number, start: number | undefined): number {
  if (!Number.isFinite(used) || start === undefined || !Number.isFinite(start)) return 0
  return Math.max(0, used - start)
}

export function tokensPerSecond(tokens: number, elapsedMs: number): string {
  if (
    !Number.isFinite(tokens) ||
    !Number.isFinite(elapsedMs) ||
    tokens <= 0 ||
    elapsedMs <= 0
  ) {
    return "0.0"
  }
  return (tokens / (elapsedMs / 1000)).toFixed(1)
}

export function responseSummaryLine(input: ResponseSummaryInput): string {
  const agent = input.agent.trim() || "agente"
  const model = input.model.trim() || "modelo"
  return `${agent} · ${model} · ${formatDuration(input.elapsedMs)} · ${tokensPerSecond(input.tokens, input.elapsedMs)} tok/s`
}

export function streamingResponseId(messages: readonly ChatMessage[]): string | undefined {
  return messages.find(message => message.role === "assistant" && message.streaming)?.id
}

// Só devolve a mensagem quando o streaming terminou: nunca exibimos resumo
// no meio da geração nem para mensagens antigas vindas do snapshot.
export function finishedResponse(
  activeId: string | undefined,
  messages: readonly ChatMessage[]
): ChatMessage | undefined {
  if (!activeId) return undefined
  return messages.find(
    message => message.id === activeId && message.role === "assistant" && !message.streaming
  )
}
