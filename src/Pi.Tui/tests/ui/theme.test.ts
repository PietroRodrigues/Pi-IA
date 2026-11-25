import { describe, expect, it } from "bun:test"
import {
  CHAT_LAYOUT,
  colors,
  hasChatSideBar,
  INPUT_BAR_GLYPH,
  INPUT_LAYOUT,
  inputBlockHeight,
  inputInnerLines,
  summarySpacerLines,
  THIN_BAR_GLYPH,
  USER_BAR_COLOR,
  USER_BAR_GLYPH,
  userBarGlyphs
} from "../../src/ui/theme"

describe("layout da caixa de entrada e do chat", () => {
  it("usa barra lateral fina no chat e um traço mais largo na entrada", () => {
    expect(THIN_BAR_GLYPH).toBe("▏")
    expect(INPUT_BAR_GLYPH).toBe("▎")
    expect(INPUT_BAR_GLYPH).not.toBe(THIN_BAR_GLYPH)
    expect(INPUT_BAR_GLYPH).not.toBe("█")
    expect(colors.accent).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it("reserva margem direita visível, sem alterar a esquerda da barra", () => {
    expect(INPUT_LAYOUT.marginRight).toBeGreaterThan(0)
    expect(INPUT_LAYOUT.marginRight).toBe(1)
    expect(INPUT_LAYOUT.marginRight).toBe(INPUT_LAYOUT.marginX)
    expect(INPUT_LAYOUT.marginTop).toBe(1)
    expect(INPUT_LAYOUT.marginBottom).toBe(1)
    expect(CHAT_LAYOUT.marginX).toBe(1)
    expect(CHAT_LAYOUT.marginRight).toBe(1)
    expect(CHAT_LAYOUT.marginBottom).toBe(1)
  })

  it("reserva uma linha entre a resposta e suas métricas", () => {
    expect(CHAT_LAYOUT.summaryGapLines).toBe(1)
    expect(summarySpacerLines(true)).toBe(1)
    expect(summarySpacerLines(false)).toBe(0)
    expect(summarySpacerLines(true, 2)).toBe(2)
  })

  it("dá respiro superior e inferior ao bloco enviado", () => {
    expect(CHAT_LAYOUT.userMarginTop).toBe(1)
    expect(CHAT_LAYOUT.userMarginBottom).toBe(1)
    expect(CHAT_LAYOUT.marginBottom).toBe(1)
  })

  it("usa barra laranja mais larga somente na mensagem enviada", () => {
    expect(USER_BAR_GLYPH).toBe("▎")
    expect(USER_BAR_GLYPH).not.toBe(THIN_BAR_GLYPH)
    expect(USER_BAR_COLOR).toBe("#F59E0B")
    expect(hasChatSideBar("user")).toBe(true)
    expect(hasChatSideBar("assistant")).toBe(false)
  })

  it("cobre quebras de linha e o resumo sem recortar o texto", () => {
    const glyphs = userBarGlyphs("linha 1\nlinha 2", 2)
    expect(glyphs.length).toBeGreaterThanOrEqual(4)
    expect(glyphs.every(glyph => glyph === USER_BAR_GLYPH)).toBe(true)
    expect(userBarGlyphs("x".repeat(120)).length).toBeGreaterThan(1)
  })

  it("mantém a barra contínua em toda a altura do bloco", () => {
    expect(INPUT_LAYOUT.gapLines).toBe(2)
    expect(INPUT_LAYOUT.paddingTop).toBe(1)
    expect(INPUT_LAYOUT.paddingBottom).toBe(0)
    expect(inputInnerLines()).toBe(4)
    expect(inputBlockHeight()).toBe(
      INPUT_LAYOUT.paddingTop + inputInnerLines() + INPUT_LAYOUT.paddingBottom
    )
    expect(inputBlockHeight()).toBe(5)
    expect(inputBlockHeight(2)).toBe(5)
    expect(inputBlockHeight(0)).toBe(3)
  })

  it("coloca a linha de contexto junto à borda inferior do bloco", () => {
    expect(INPUT_LAYOUT.paddingTop + inputInnerLines()).toBe(
      inputBlockHeight() - INPUT_LAYOUT.paddingBottom
    )
    expect(INPUT_LAYOUT.paddingBottom).toBe(0)
  })
})
