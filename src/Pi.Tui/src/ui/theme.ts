// Paleta compartilhada dos painéis visuais: fundos escuros e acento âmbar.
// Valores e medidas vivem em constants.ts; aqui só a lógica que os consome.
import { GLYPHS, LAYOUT, LIMITS, ROLES, TEXT } from "../constants"

export function hasChatSideBar(role: string): boolean {
  return role === ROLES.user
}

// Estima linhas suficientes para cobrir o bloco, incluindo quebras e o resumo.
// Linhas excedentes apenas ampliam a coluna da barra: nunca recortam o texto.
export function userBarGlyphs(
  text: string,
  extraLines = 0,
  width: number = LAYOUT.userBar.defaultWidth
): readonly string[] {
  const usable = Math.max(LAYOUT.userBar.minWidth, width - LAYOUT.userBar.widthReduction)
  const lines = text.split(TEXT.common.newline).reduce<number>(
    (total, line) => total + Math.max(LAYOUT.one, Math.ceil(line.length / usable)),
    LIMITS.zero
  )
  const count = Math.max(LAYOUT.one, lines + extraLines)
  return Array.from({ length: count }, () => GLYPHS.userBar)
}

// Respiro entre o texto final da resposta e a linha de métricas.
export function summarySpacerLines(
  hasSummary: boolean,
  gapLines: number = LAYOUT.chat.summaryGapLines
): number {
  return hasSummary ? gapLines : LAYOUT.zero
}

// Linhas internas: campo, respiro e linha de agente/modelo/status.
export function inputInnerLines(gapLines: number = LAYOUT.input.gapLines): number {
  return LAYOUT.two + gapLines
}

// A barra lateral ocupa toda a altura do bloco, incluindo o respiro vertical
// interno; o conteúdo usa esse total menos o padding superior/inferior.
export function inputBlockHeight(gapLines: number = LAYOUT.input.gapLines): number {
  return (
    LAYOUT.input.paddingTop + inputInnerLines(gapLines) + LAYOUT.input.paddingBottom
  )
}

export function markerOf(active: boolean, selected: boolean): string {
  if (selected) return GLYPHS.play
  if (active) return GLYPHS.dot
  return GLYPHS.space
}
