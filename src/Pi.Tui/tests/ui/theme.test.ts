import { describe, expect, it } from "bun:test"
import { COLORS, GLYPHS, LAYOUT } from "../../src/constants"
import {
  hasChatSideBar,
  inputBlockHeight,
  inputInnerLines,
  summarySpacerLines,
  userBarGlyphs
} from "../../src/ui/theme"

describe("layout da caixa de entrada e do chat", () => {
  it("usa barra lateral fina no chat e um traço mais largo na entrada", () => {
    expect(GLYPHS.thinBar).toBe("▏")
    expect(GLYPHS.inputBar).toBe("▎")
    expect(GLYPHS.inputBar).not.toBe(GLYPHS.thinBar)
    expect(GLYPHS.inputBar).not.toBe("█")
    expect(COLORS.accent).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it("reserva margem direita visível, sem alterar a esquerda da barra", () => {
    expect(LAYOUT.input.marginRight).toBeGreaterThan(0)
    expect(LAYOUT.input.marginRight).toBe(1)
    expect(LAYOUT.input.marginRight).toBe(LAYOUT.input.marginX)
    expect(LAYOUT.input.marginTop).toBe(1)
    expect(LAYOUT.input.marginBottom).toBe(1)
    expect(LAYOUT.chat.marginX).toBe(1)
    expect(LAYOUT.chat.marginRight).toBe(1)
    expect(LAYOUT.chat.marginBottom).toBe(1)
  })

  it("reserva uma linha entre a resposta e suas métricas", () => {
    expect(LAYOUT.chat.summaryGapLines).toBe(1)
    expect(summarySpacerLines(true)).toBe(1)
    expect(summarySpacerLines(false)).toBe(0)
    expect(summarySpacerLines(true, 2)).toBe(2)
  })

  it("dá respiro superior e inferior ao bloco enviado", () => {
    expect(LAYOUT.chat.userMarginTop).toBe(1)
    expect(LAYOUT.chat.userMarginBottom).toBe(1)
    expect(LAYOUT.chat.marginBottom).toBe(1)
  })

  it("usa barra laranja mais larga somente na mensagem enviada", () => {
    expect(GLYPHS.userBar).toBe("▎")
    expect(GLYPHS.userBar).not.toBe(GLYPHS.thinBar)
    expect(COLORS.accent).toBe("#F59E0B")
    expect(hasChatSideBar("user")).toBe(true)
    expect(hasChatSideBar("assistant")).toBe(false)
  })

  it("cobre quebras de linha e o resumo sem recortar o texto", () => {
    const glyphs = userBarGlyphs("linha 1\nlinha 2", 2)
    expect(glyphs.length).toBeGreaterThanOrEqual(4)
    expect(glyphs.every(glyph => glyph === GLYPHS.userBar)).toBe(true)
    expect(userBarGlyphs("x".repeat(120)).length).toBeGreaterThan(1)
  })

  it("mantém a barra contínua em toda a altura do bloco", () => {
    expect(LAYOUT.input.gapLines).toBe(2)
    expect(LAYOUT.input.paddingTop).toBe(1)
    expect(LAYOUT.input.paddingBottom).toBe(0)
    expect(inputInnerLines()).toBe(4)
    expect(inputBlockHeight()).toBe(
      LAYOUT.input.paddingTop + inputInnerLines() + LAYOUT.input.paddingBottom
    )
    expect(inputBlockHeight()).toBe(5)
    expect(inputBlockHeight(2)).toBe(5)
    expect(inputBlockHeight(0)).toBe(3)
  })

  it("coloca a linha de contexto junto à borda inferior do bloco", () => {
    expect(LAYOUT.input.paddingTop + inputInnerLines()).toBe(
      inputBlockHeight() - LAYOUT.input.paddingBottom
    )
    expect(LAYOUT.input.paddingBottom).toBe(0)
  })
})
