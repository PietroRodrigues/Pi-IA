// Medidas compartilhadas entre o menu raiz e o painel de seleção. As linhas de
// respiro entram na altura para o item ativo nunca ficar fora da janela visível.
// Os valores estão em constants.ts; aqui fica apenas o cálculo.
import { LAYOUT, LIMITS } from "../constants"
import type { SelectionGroup } from "./providerCatalog"

export interface WindowBounds {
  readonly start: number
  readonly end: number
}

export interface GroupLayout {
  readonly groups: number
  readonly items: number
}

export function windowBounds(index: number, total: number, max: number): WindowBounds {
  if (total <= max) return { start: LAYOUT.zero, end: total }

  const start = Math.max(
    LAYOUT.zero,
    Math.min(index - LAYOUT.windowBackOffset, total - max)
  )
  return { start, end: start + max }
}

export function isInWindow(index: number, bounds: WindowBounds): boolean {
  return index >= bounds.start && index < bounds.end
}

export function visibleGroupLayout(
  groups: readonly SelectionGroup[],
  bounds: WindowBounds
): GroupLayout {
  let cursor = LAYOUT.cursorStart
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

export function simpleMenuHeight(rowCount: number, hasSubtitle: boolean): number {
  return (
    LAYOUT.simpleHeaderLines +
    LAYOUT.menuGapLines +
    (hasSubtitle ? LAYOUT.subtitleLines + LAYOUT.menuGapLines : LAYOUT.zero) +
    LAYOUT.panelFooterLines +
    Math.max(rowCount, LIMITS.minimumRows) * LAYOUT.itemLineHeight
  )
}

export function selectionMenuHeight(layout: GroupLayout, pinned: boolean): number {
  const sectionLines =
    layout.groups * (LAYOUT.sectionSpacerLines * LAYOUT.two + LAYOUT.one)
  const itemLines = layout.items * LAYOUT.itemLineHeight
  return (
    LAYOUT.selectionHeaderLines +
    LAYOUT.panelFooterLines +
    (pinned ? LAYOUT.pinnedLines : LAYOUT.zero) +
    sectionLines +
    itemLines
  )
}
