import { describe, expect, it } from "bun:test"
import { LAYOUT } from "../../src/constants"
import {
  isInWindow,
  selectionMenuHeight,
  simpleMenuHeight,
  visibleGroupLayout,
  windowBounds,
  type GroupLayout
} from "../../src/ui/menuLayout"
import type { SelectionGroup } from "../../src/ui/providerCatalog"

function group(title: string, count: number): SelectionGroup {
  return {
    title,
    items: Array.from({ length: count }, (_, index) => ({
      id: `${title}-${index}`,
      label: `item ${index}`,
      detail: "detalhe",
      group: title
    }))
  }
}

describe("altura e janela visível dos menus", () => {
  it("mantém o índice ativo dentro da janela deslizante", () => {
    const bounds = windowBounds(10, 20, LAYOUT.maxSelectionItems)

    expect(bounds).toEqual({ start: 8, end: 16 })
    expect(isInWindow(10, bounds)).toBe(true)
    expect(isInWindow(7, bounds)).toBe(false)

    for (let index = 0; index < 20; index += 1) {
      const current = windowBounds(index, 20, LAYOUT.maxSelectionItems)
      expect(isInWindow(index, current)).toBe(true)
      expect(current.end - current.start).toBeLessThanOrEqual(LAYOUT.maxSelectionItems)
    }
  })

  it("conta apenas grupos com itens visíveis na janela", () => {
    const groups = [group("A", 5), group("B", 5), group("C", 5)]
    const layout = visibleGroupLayout(groups, windowBounds(7, 15, 8))

    expect(layout).toEqual({ groups: 2, items: 8 })
  })

  it("usa linhas compactas nos itens e respiro moderado nos subtítulos", () => {
    expect(LAYOUT.itemSpacerLines).toBe(0)
    expect(LAYOUT.itemLineHeight).toBe(1)
    expect(LAYOUT.sectionSpacerLines).toBe(1)
    expect(LAYOUT.pinnedLines).toBe(3)

    expect(simpleMenuHeight(7, false) - simpleMenuHeight(1, false)).toBe(
      6 * LAYOUT.itemLineHeight
    )
  })

  it("reserva três linhas de margem no menu simples", () => {
    expect(LAYOUT.menuGapLines).toBe(1)
    expect(LAYOUT.simpleHeaderLines).toBe(4)
    expect(LAYOUT.subtitleIndent).toBe(2)
    expect(LAYOUT.subtitleLines).toBe(1)

    // Duas margens já estão em simpleHeaderLines; a terceira vem antes do rodapé.
    expect(simpleMenuHeight(1, false)).toBe(
      LAYOUT.simpleHeaderLines +
        LAYOUT.menuGapLines +
        LAYOUT.panelFooterLines +
        LAYOUT.itemLineHeight
    )
    expect(simpleMenuHeight(1, true) - simpleMenuHeight(1, false)).toBe(
      LAYOUT.subtitleLines + LAYOUT.menuGapLines
    )
  })

  it("inclui as linhas de respiro no cálculo da altura", () => {
    const base: GroupLayout = { groups: 3, items: 8 }
    const withPinned = selectionMenuHeight(base, true)
    const withoutPinned = selectionMenuHeight(base, false)

    expect(withPinned - withoutPinned).toBe(LAYOUT.pinnedLines)
    expect(selectionMenuHeight({ groups: 3, items: 16 }, false) - withoutPinned).toBe(
      8 * LAYOUT.itemLineHeight
    )
    expect(windowBounds(0, 5, LAYOUT.maxMenuItems)).toEqual({ start: 0, end: 5 })
  })
})
