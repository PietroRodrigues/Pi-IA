// Medidas compartilhadas entre o menu raiz e o painel de seleção. As linhas de
// respiro entram na altura para o item ativo nunca ficar fora da janela visível.
import type { SelectionGroup } from "./providerCatalog"

export const MAX_MENU_ITEMS = 7
export const MAX_SELECTION_ITEMS = 8
// Itens compactos; o respiro fica concentrado ao redor dos subtítulos.
export const ITEM_SPACER_LINES = 0
export const SECTION_SPACER_LINES = 1
export const ITEM_LINE_HEIGHT = 1 + ITEM_SPACER_LINES
export const PINNED_LINES = 3 + ITEM_SPACER_LINES
export const SELECTION_HEADER_LINES = 2
export const SIMPLE_HEADER_LINES = 3
export const PANEL_FOOTER_LINES = 1

export interface WindowBounds {
  readonly start: number
  readonly end: number
}

export interface GroupLayout {
  readonly groups: number
  readonly items: number
}

export function windowBounds(index: number, total: number, max: number): WindowBounds {
  if (total <= max) return { start: 0, end: total }

  const start = Math.max(0, Math.min(index - 2, total - max))
  return { start, end: start + max }
}

export function isInWindow(index: number, bounds: WindowBounds): boolean {
  return index >= bounds.start && index < bounds.end
}

export function visibleGroupLayout(
  groups: readonly SelectionGroup[],
  bounds: WindowBounds
): GroupLayout {
  let cursor = -1
  let visibleGroups = 0
  let visibleItems = 0

  for (const group of groups) {
    let groupHasVisibleItem = false

    for (let offset = 0; offset < group.items.length; offset += 1) {
      cursor += 1
      if (isInWindow(cursor, bounds)) {
        visibleItems += 1
        groupHasVisibleItem = true
      }
    }

    if (groupHasVisibleItem) visibleGroups += 1
  }

  return { groups: visibleGroups, items: visibleItems }
}

export function simpleMenuHeight(rowCount: number): number {
  return SIMPLE_HEADER_LINES + PANEL_FOOTER_LINES + Math.max(rowCount, 1) * ITEM_LINE_HEIGHT
}

export function selectionMenuHeight(layout: GroupLayout, pinned: boolean): number {
  const sectionLines = layout.groups * (SECTION_SPACER_LINES * 2 + 1)
  const itemLines = layout.items * ITEM_LINE_HEIGHT
  return (
    SELECTION_HEADER_LINES +
    PANEL_FOOTER_LINES +
    (pinned ? PINNED_LINES : 0) +
    sectionLines +
    itemLines
  )
}
