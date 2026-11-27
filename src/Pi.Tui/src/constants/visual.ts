// Tema e medidas da interface. Valores padrão da apresentação visual.

// Origem: paleta de cores da TUI
// Finalidade: cores de fundo, painel, texto, acento e feedback.
export const COLORS = {
  background: "#0A0A0A",
  panel: "#141414",
  panelAlt: "#1E1E1E",
  text: "#E5E5E5",
  muted: "#A3A3A3",
  accent: "#F59E0B",
  // Verde do tema original; não usado pelos componentes atuais.
  accentAlt: "#0bf52a",
  success: "#34D399",
  error: "#F87171",
  assistantText: "#D4D4D4"
} as const

// Origem: glifos usados nos indicadores
// Finalidade: marcadores, setas e frames de pensamento.
export const GLYPHS = {
  thinBar: "▏",
  inputBar: "▎",
  userBar: "▎",
  streamCursor: "▌",
  check: "✓",
  cross: "✗",
  gear: "⚙",
  play: "▶",
  dot: "●",
  emDash: "—",
  section: "─",
  space: " ",
  thinkingFrames: [".", "..", "..."]
} as const

// Origem: layout do terminal
// Finalidade: alturas, larguras, margens e respiros dos componentes.
export const LAYOUT = {
  screenMode: "alternate-screen",
  fullWidth: "100%",
  fullHeight: "100%",
  autoWidth: "auto",
  zero: 0,
  one: 1,
  two: 2,
  lineHeight: 1,
  columnWidth: 1,
  gutterWidth: 1,
  searchBoxWidth: 4,
  statusBarHeight: 2,
  modalHeight: 6,
  labelWidth: 34,
  maxMenuItems: 7,
  maxSelectionItems: 8,
  itemSpacerLines: 0,
  menuGapLines: 1,
  subtitleIndent: 2,
  subtitleLines: 1,
  sectionSpacerLines: 1,
  itemLineHeight: 1,
  pinnedLines: 3,
  selectionHeaderLines: 2,
  simpleHeaderLines: 4,
  panelFooterLines: 1,
  windowBackOffset: 2,
  cursorStart: -1,
  input: {
    gapLines: 2,
    paddingTop: 1,
    paddingBottom: 0,
    paddingRight: 1,
    marginTop: 1,
    marginX: 1,
    marginRight: 1,
    marginBottom: 1
  },
  chat: {
    marginX: 1,
    marginRight: 1,
    marginBottom: 1,
    userMarginTop: 1,
    userMarginBottom: 1,
    summaryGapLines: 1,
    bubblePadding: 2,
    bubblePaddingRight: 2,
    textPaddingRight: 1
  },
  userBar: {
    defaultWidth: 40,
    minWidth: 8,
    widthReduction: 6
  }
} as const
