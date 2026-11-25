// Painel de seleção: o campo de chat continua sendo a fonte de digitação; aqui
// apenas espelhamos Search, mostramos "esc" e agrupamos os itens com respiro.
import { createMemo, For, Show } from "solid-js"
import {
  ITEM_SPACER_LINES,
  MAX_SELECTION_ITEMS,
  SECTION_SPACER_LINES,
  selectionMenuHeight,
  visibleGroupLayout,
  windowBounds,
  type GroupLayout
} from "./menuLayout"
import type { CatalogItem, SelectionGroup } from "./providerCatalog"
import { colors, LABEL_WIDTH, markerOf } from "./theme"

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
  readonly onEscape: () => void
}

export function SelectionPanel(props: SelectionPanelProps) {
  const total = createMemo(() => props.groups.reduce((sum, group) => sum + group.items.length, 0))
  const bounds = createMemo(() => windowBounds(props.index, total(), MAX_SELECTION_ITEMS))
  const layout = createMemo<GroupLayout>(() => visibleGroupLayout(props.groups, bounds()))

  const visible = createMemo<VisibleGroup[]>(() => {
    let cursor = -1

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
        width: "100%",
        height: height(),
        flexDirection: "column",
        backgroundColor: colors.panel,
        paddingLeft: 1,
        paddingRight: 1
      }}
    >
      <text content={props.title} fg={colors.accent} />

      <box style={{ width: "100%", height: 1, flexDirection: "row" }}>
        <text content="Search " fg={colors.muted} />
        <text content={props.query || "digite no campo abaixo..."} fg={colors.text} />
        <box style={{ flexGrow: 1 }} />
        <box style={{ width: 4, height: 1 }} onMouseDown={props.onEscape}>
          <text content="esc" fg={colors.muted} />
        </box>
      </box>

      <Show when={props.pinned}>
        {pinned => (
          <>
            <box
              style={{
                width: "100%",
                height: 1,
                flexDirection: "row",
                backgroundColor: colors.panelAlt,
                paddingLeft: 1,
                paddingRight: 1
              }}
            >
              <box style={{ width: LABEL_WIDTH, height: 1 }}>
                <text content={`▶ ${pinned().label}`} fg={colors.accent} />
              </box>
              <box style={{ flexGrow: 1, height: 1 }}>
                <text
                  content={pinned().detail}
                  fg={colors.muted}
                  textAlign="right"
                  style={{ width: "100%" }}
                />
              </box>
            </box>
            <text
              content={`Caminho: ${props.pinnedPath || "cole um caminho ou busque um diretório"}`}
              fg={colors.muted}
            />
            <text
              content={props.pinnedBrowse ? "Busca visual · nenhum arquivo lido" : "Enter: buscar diretório (visual)"}
              fg={props.pinnedBrowse ? colors.success : colors.muted}
            />
            <Show when={ITEM_SPACER_LINES > 0}>
              <box style={{ height: ITEM_SPACER_LINES }} />
            </Show>
          </>
        )}
      </Show>

      <For each={visible()}>
        {group => (
          <>
            <box style={{ height: SECTION_SPACER_LINES }} />
            <text content={`─ ${group.title}`} fg={colors.accent} />
            <For each={group.rows}>
              {row => {
                const selected = () => props.index === row.index
                const color = () => (selected() ? colors.accent : colors.text)

                return (
                  <>
                    <box
                      style={{
                        width: "100%",
                        height: 1,
                        flexDirection: "row",
                        backgroundColor: selected() ? colors.panelAlt : colors.panel,
                        paddingLeft: 1,
                        paddingRight: 1
                      }}
                    >
                      <box style={{ width: LABEL_WIDTH, height: 1 }}>
                        <text
                          content={`${markerOf(false, selected())} ${row.item.label}`}
                          fg={color()}
                        />
                      </box>
                      <box style={{ flexGrow: 1, height: 1 }}>
                        <text
                          content={row.item.detail}
                          fg={selected() ? colors.text : colors.muted}
                          textAlign="right"
                          style={{ width: "100%" }}
                        />
                      </box>
                    </box>
                    <Show when={ITEM_SPACER_LINES > 0}>
              <box style={{ height: ITEM_SPACER_LINES }} />
            </Show>
                  </>
                )
              }}
            </For>
            <box style={{ height: SECTION_SPACER_LINES }} />
          </>
        )}
      </For>

      <text content="↑↓ navegar · Enter selecionar · Esc voltar · filtro no campo" fg={colors.muted} />
    </box>
  )
}
