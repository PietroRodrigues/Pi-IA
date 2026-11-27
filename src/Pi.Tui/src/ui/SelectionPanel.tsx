// Painel de seleção: o campo de chat continua sendo a fonte de digitação; aqui
// apenas espelhamos Search, mostramos "esc" e agrupamos os itens com respiro.
import { createMemo, For, Show } from "solid-js"
import { COLORS, COMMANDS, GLYPHS, LAYOUT, TEXT } from "../constants"
import {
  selectionMenuHeight,
  visibleGroupLayout,
  windowBounds,
  type GroupLayout
} from "./menuLayout"
import type { CatalogItem, SelectionGroup } from "./providerCatalog"
import { markerOf } from "./theme"

interface VisibleGroup {
  readonly title: string
  readonly rows: readonly { readonly item: CatalogItem; readonly index: number }[]
}

interface SelectionPanelProps {
  readonly title: string
  readonly query: string
  readonly index: number
  readonly groups: readonly SelectionGroup[]
  readonly pinned?: CatalogItem
  readonly pinnedPath?: string
  readonly pinnedBrowse?: boolean
  readonly enabledIds?: readonly string[]
  readonly onEscape: () => void
}

export function SelectionPanel(props: SelectionPanelProps) {
  const total = createMemo(() => props.groups.reduce((sum, group) => sum + group.items.length, 0))
  const bounds = createMemo(() => windowBounds(props.index, total(), LAYOUT.maxSelectionItems))
  const layout = createMemo<GroupLayout>(() => visibleGroupLayout(props.groups, bounds()))

  const visible = createMemo<VisibleGroup[]>(() => {
    let cursor = LAYOUT.cursorStart

    return props.groups.flatMap(group => {
      const rows: { item: CatalogItem; index: number }[] = []
      for (const item of group.items) {
        cursor += 1
        if (cursor >= bounds().start && cursor < bounds().end) {
          rows.push({ item, index: cursor })
        }
      }
      return rows.length > 0 ? [{ title: group.title, rows }] : []
    })
  })

  const height = () => selectionMenuHeight(layout(), Boolean(props.pinned))

  return (
    <box
      style={{
        width: LAYOUT.fullWidth,
        height: height(),
        flexDirection: COMMANDS.apiValues.column,
        backgroundColor: COLORS.panel,
        paddingLeft: LAYOUT.one,
        paddingRight: LAYOUT.one
      }}
    >
      <text content={props.title} fg={COLORS.accent} />

      <box style={{ width: LAYOUT.fullWidth, height: LAYOUT.lineHeight, flexDirection: COMMANDS.apiValues.row }}>
        <text content={TEXT.menu.search} fg={COLORS.muted} />
        <text content={props.query || TEXT.menu.searchPlaceholder} fg={COLORS.text} />
        <box style={{ flexGrow: LAYOUT.one }} />
        <box
          style={{ width: LAYOUT.searchBoxWidth, height: LAYOUT.lineHeight }}
          onMouseDown={props.onEscape}
        >
          <text content={TEXT.menu.esc} fg={COLORS.muted} />
        </box>
      </box>

      <Show when={props.pinned}>
        {pinned => (
          <>
            <box
              style={{
                width: LAYOUT.fullWidth,
                height: LAYOUT.lineHeight,
                flexDirection: COMMANDS.apiValues.row,
                backgroundColor: COLORS.panelAlt,
                paddingLeft: LAYOUT.one,
                paddingRight: LAYOUT.one
              }}
            >
              <box style={{ width: LAYOUT.labelWidth, height: LAYOUT.lineHeight }}>
                <text
                  content={`${GLYPHS.play}${GLYPHS.space}${pinned().label}`}
                  fg={COLORS.accent}
                />
              </box>
              <box style={{ flexGrow: LAYOUT.one, height: LAYOUT.lineHeight }}>
                <text
                  content={pinned().detail}
                  fg={COLORS.muted}
                  textAlign={COMMANDS.apiValues.right}
                  style={{ width: LAYOUT.fullWidth }}
                />
              </box>
            </box>
            <text
              content={`${TEXT.menu.pathLabel}${props.pinnedPath || pinned().detail}`}
              fg={COLORS.muted}
            />
            <text
              content={
                props.pinnedBrowse ? TEXT.menu.pinnedBrowse : TEXT.menu.pinnedIdle
              }
              fg={props.pinnedBrowse ? COLORS.success : COLORS.muted}
            />
            <Show when={LAYOUT.itemSpacerLines > LAYOUT.zero}>
              <box style={{ height: LAYOUT.itemSpacerLines }} />
            </Show>
          </>
        )}
      </Show>

      <For each={visible()}>
        {group => (
          <>
            <box style={{ height: LAYOUT.sectionSpacerLines }} />
            <text
              content={`${GLYPHS.section}${GLYPHS.space}${group.title}`}
              fg={COLORS.accent}
            />
            <For each={group.rows}>
              {row => {
                const selected = () => props.index === row.index
                const enabled = () => props.enabledIds?.includes(row.item.id) ?? true
                const active = () => (props.enabledIds ? enabled() : false)
                const detail = () =>
                  props.enabledIds
                    ? enabled()
                      ? TEXT.menu.toolEnabled
                      : TEXT.menu.toolDisabled
                    : row.item.detail
                const color = () =>
                  selected()
                    ? COLORS.accent
                    : props.enabledIds && !enabled()
                      ? COLORS.muted
                      : COLORS.text

                return (
                  <>
                    <box
                      style={{
                        width: LAYOUT.fullWidth,
                        height: LAYOUT.lineHeight,
                        flexDirection: COMMANDS.apiValues.row,
                        backgroundColor: selected() ? COLORS.panelAlt : COLORS.panel,
                        paddingLeft: LAYOUT.one,
                        paddingRight: LAYOUT.one
                      }}
                    >
                      <box style={{ width: LAYOUT.labelWidth, height: LAYOUT.lineHeight }}>
                        <text
                          content={`${markerOf(active(), selected())}${GLYPHS.space}${row.item.label}`}
                          fg={color()}
                        />
                      </box>
                      <box style={{ flexGrow: LAYOUT.one, height: LAYOUT.lineHeight }}>
                        <text
                          content={detail()}
                          fg={selected() ? COLORS.text : COLORS.muted}
                          textAlign={COMMANDS.apiValues.right}
                          style={{ width: LAYOUT.fullWidth }}
                        />
                      </box>
                    </box>
                    <Show when={LAYOUT.itemSpacerLines > LAYOUT.zero}>
                      <box style={{ height: LAYOUT.itemSpacerLines }} />
                    </Show>
                  </>
                )
              }}
            </For>
            <box style={{ height: LAYOUT.sectionSpacerLines }} />
          </>
        )}
      </For>

      <text
        content={props.enabledIds ? TEXT.menu.toolsFooter : TEXT.menu.selectionFooter}
        fg={COLORS.muted}
      />
    </box>
  )
}
