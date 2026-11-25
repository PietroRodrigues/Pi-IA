// Paleta compartilhada dos painéis visuais: fundos escuros e acento âmbar.
export const colors = {
  background: "#0A0A0A",
  panel: "#141414",
  panelAlt: "#1E1E1E",
  text: "#E5E5E5",
  muted: "#A3A3A3",
  accent: "#0bf52a",
  success: "#34D399"
} as const

// Barra lateral fina do chat: glifo de bloco estreito, discreto.
export const THIN_BAR_GLYPH = "▏"

// Barra da entrada: traço um pouco mais largo que o do chat. Como o terminal
// trabalha com células inteiras, ▎ é o incremento prático mais próximo.
export const INPUT_BAR_GLYPH = "▎"

// marginX é o recuo esquerdo (a barra fica rente a ele); marginRight é o
// respiro visível entre o fundo da caixa e a borda direita do terminal.
// O respiro entre o campo e a linha de contexto fica acima dela; abaixo não
// há padding, para que o texto fique junto à borda inferior do bloco.
export const INPUT_LAYOUT = {
  gapLines: 2,
  paddingTop: 1,
  paddingBottom: 0,
  marginTop: 1,
  marginX: 1,
  marginRight: 1,
  marginBottom: 1
} as const

// As margens laterais são aplicadas como padding do conteúdo do scrollbox:
// assim os blocos usam toda a largura disponível sem estourar à direita.
export const CHAT_LAYOUT = {
  marginX: 1,
  marginRight: 1,
  marginBottom: 1,
  userMarginTop: 1,
  userMarginBottom: 1,
  summaryGapLines: 1
} as const

// Barra da mensagem enviada: traço ▎ (mais largo que o ▏ do chat) em laranja.
export const USER_BAR_GLYPH = "▎"
export const USER_BAR_COLOR = "#F59E0B"

export function hasChatSideBar(role: string): boolean {
  return role === "user"
}

// Estima linhas suficientes para cobrir o bloco, incluindo quebras e o resumo.
// Linhas excedentes apenas ampliam a coluna da barra: nunca recortam o texto.
export function userBarGlyphs(
  text: string,
  extraLines = 0,
  width = 40
): readonly string[] {
  const usable = Math.max(8, width - 6)
  const lines = text.split("\n").reduce(
    (total, line) => total + Math.max(1, Math.ceil(line.length / usable)),
    0
  )
  const count = Math.max(1, lines + extraLines)
  return Array.from({ length: count }, () => USER_BAR_GLYPH)
}

// Respiro entre o texto final da resposta e a linha de métricas.
export function summarySpacerLines(
  hasSummary: boolean,
  gapLines: number = CHAT_LAYOUT.summaryGapLines
): number {
  return hasSummary ? gapLines : 0
}

// Linhas internas: campo, respiro e linha de agente/modelo/status.
export function inputInnerLines(gapLines: number = INPUT_LAYOUT.gapLines): number {
  return 2 + gapLines
}

// A barra lateral ocupa toda a altura do bloco, incluindo o respiro vertical
// interno; o conteúdo usa esse total menos o padding superior/inferior.
export function inputBlockHeight(gapLines: number = INPUT_LAYOUT.gapLines): number {
  return INPUT_LAYOUT.paddingTop + inputInnerLines(gapLines) + INPUT_LAYOUT.paddingBottom
}

export const LABEL_WIDTH = 34

export function markerOf(active: boolean, selected: boolean): string {
  if (selected) return "▶"
  if (active) return "●"
  return " "
}
