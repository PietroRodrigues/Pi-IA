// Indicador de execução: as reticências são animadas dentro do chat e o status
// "pensando…" do store não aparece duplicado no rodapé.
import { COMMANDS, GLYPHS, ROLES, STATUSES, TEXT } from "../constants"
import type { ChatMessage } from "../state/reducers"

export function thinkingFrame(tick: number): string {
  const frames = GLYPHS.thinkingFrames
  const length = frames.length
  return frames[((tick % length) + length) % length]
}

export function thinkingLabel(tick: number): string {
  return `${TEXT.chat.thinkingPrefix}${thinkingFrame(tick)}`
}

export function footerStatus(status: string, busy: boolean): string {
  return (
    busy && new RegExp(STATUSES.thinkingMatch, COMMANDS.apiValues.regexCaseInsensitive).test(status)
  )
    ? TEXT.common.empty
    : status
}

// O indicador ocupa a linha inicial da resposta: existe enquanto o turno está
// ocupado e a mensagem do assistente ainda não recebeu o primeiro token.
export function hasStreamingAssistant(messages: readonly ChatMessage[]): boolean {
  return messages.some(
    message =>
      message.role === ROLES.assistant && message.streaming && !message.text.trim()
  )
}

export function isThinkingTurn(busy: boolean, messages: readonly ChatMessage[]): boolean {
  return busy && hasStreamingAssistant(messages)
}

export function thinkingText(
  busy: boolean,
  messages: readonly ChatMessage[],
  tick: number
): string {
  return isThinkingTurn(busy, messages) ? thinkingLabel(tick) : TEXT.common.empty
}

// Linha dentro da caixa de entrada: apenas os valores, sem rótulos. O agente
// recebe a cor de destaque na View; aqui só preparamos os segmentos.
export interface InputContextSegments {
  readonly agent: string
  readonly model: string
  readonly status: string
}

export function inputContextSegments(
  agent: string,
  model: string,
  status: string,
  busy: boolean
): InputContextSegments {
  return {
    agent: agent.trim() || GLYPHS.emDash,
    model: model.trim() || GLYPHS.emDash,
    status: footerStatus(status, busy).trim()
  }
}

export function inputContextLine(
  agent: string,
  model: string,
  status: string,
  busy: boolean
): string {
  const parts = inputContextSegments(agent, model, status, busy)
  return [parts.agent, parts.model, parts.status]
    .filter(part => part.length > 0)
    .join(TEXT.common.separator)
}
