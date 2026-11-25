import { describe, expect, it } from "bun:test"
import {
  isInWindow,
  ITEM_LINE_HEIGHT,
  ITEM_SPACER_LINES,
  MAX_MENU_ITEMS,
  MAX_SELECTION_ITEMS,
  PINNED_LINES,
  SECTION_SPACER_LINES,
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
    const bounds = windowBounds(10, 20, MAX_SELECTION_ITEMS)

    expect(bounds).toEqual({ start: 8, end: 16 })
    expect(isInWindow(10, bounds)).toBe(true)
    expect(isInWindow(7, bounds)).toBe(false)

    for (let index = 0; index < 20; index += 1) {
      const current = windowBounds(index, 20, MAX_SELECTION_ITEMS)
      expect(isInWindow(index, current)).toBe(true)
      expect(current.end - current.start).toBeLessThanOrEqual(MAX_SELECTION_ITEMS)
    }
  })

  it("conta apenas grupos com itens visíveis na janela", () => {
    const groups = [group("A", 5), group("B", 5), group("C", 5)]
    const layout = visibleGroupLayout(groups, windowBounds(7, 15, 8))

    expect(layout).toEqual({ groups: 2, items: 8 })
  })

  it("usa linhas compactas nos itens e respiro moderado nos subtítulos", () => {
    expect(ITEM_SPACER_LINES).toBe(0)
    expect(ITEM_LINE_HEIGHT).toBe(1)
    expect(SECTION_SPACER_LINES).toBe(1)
    expect(PINNED_LINES).toBe(3)

    expect(simpleMenuHeight(7) - simpleMenuHeight(1)).toBe(6 * ITEM_LINE_HEIGHT)
  })

  it("inclui as linhas de respiro no cálculo da altura", () => {
    const base: GroupLayout = { groups: 3, items: 8 }
    const withPinned = selectionMenuHeight(base, true)
    const withoutPinned = selectionMenuHeight(base, false)

    expect(withPinned - withoutPinned).toBe(PINNED_LINES)
    expect(selectionMenuHeight({ groups: 3, items: 16 }, false) - withoutPinned).toBe(
      8 * ITEM_LINE_HEIGHT
    )
    expect(windowBounds(0, 5, MAX_MENU_ITEMS)).toEqual({ start: 0, end: 5 })
  })
})
