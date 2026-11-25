// Menu "/" em etapas: raiz e submenus em duas colunas; folhas de seleção usam
// o SelectionPanel. O campo de chat continua com o foco e controla o filtro.
import { createMemo, For, Show } from "solid-js"
import type { CommandMenuAction, CommandMenuState } from "./commandMenuState"
import { currentNode, menuHeaderLabel, selectionGroups, visibleItems } from "./commandMenuState"
import {
  ITEM_SPACER_LINES,
  MAX_MENU_ITEMS,
  MAX_SELECTION_ITEMS,
  selectionMenuHeight,
  simpleMenuHeight,
  visibleGroupLayout,
  windowBounds
} from "./menuLayout"
import { LOCAL_ADD_OPTION } from "./providerCatalog"
import { ApiKeyModal } from "./ApiKeyModal"
import { SelectionPanel } from "./SelectionPanel"
import { colors, LABEL_WIDTH, markerOf } from "./theme"

interface CommandMenuProps {
  readonly state: CommandMenuState
  readonly onAction: (action: CommandMenuAction) => void
}

export function CommandMenu(props: CommandMenuProps) {
  const items = createMemo(() => visibleItems(props.state))
  const node = createMemo(() => currentNode(props.state))
  const groups = createMemo(() => selectionGroups(props.state))
  const kind = () => node()?.kind ?? "root"
  const isSelection = () => kind() === "selection" || kind() === "connect" || kind() === "local-add"
  const pinned = () => (kind() === "local-add" ? LOCAL_ADD_OPTION : undefined)

  const bounds = createMemo(() =>
    windowBounds(props.state.index, items().length, MAX_MENU_ITEMS))
  const rows = createMemo(() => items().slice(bounds().start, bounds().end))

  const selectionBounds = createMemo(() => {
    const total = groups().reduce((sum, group) => sum + group.items.length, 0)
    return windowBounds(props.state.index, total, MAX_SELECTION_ITEMS)
  })
  const selectionLayout = createMemo(() => visibleGroupLayout(groups(), selectionBounds()))

  const height = () => {
    if (props.state.apiKey) return 6
    if (isSelection()) return selectionMenuHeight(selectionLayout(), Boolean(pinned()))
    return simpleMenuHeight(rows().length)
  }

  const connected = () => {
    const names = props.state.connectedProviders
    return names.length > 0 ? ` · conectado: ${names.join(", ")}` : ""
  }

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
      <Show when={props.state.apiKey}>
        {key => (
          <ApiKeyModal
            window={key()}
            onType={value => props.onAction({ kind: "apiKeyType", value })}
            onSubmit={() => props.onAction({ kind: "apiKeySubmit" })}
            onCancel={() => props.onAction({ kind: "apiKeyCancel" })}
          />
        )}
      </Show>

      <Show when={!props.state.apiKey && isSelection()}>
        <SelectionPanel
          title={`${menuHeaderLabel(props.state)}${connected()}`}
          query={props.state.query}
          index={props.state.index}
          groups={groups()}
          pinned={pinned()}
          pinnedPath={props.state.localAddPath}
          pinnedBrowse={props.state.localAddBrowse}
          onEscape={() => props.onAction({ kind: "escape" })}
        />
      </Show>

      <Show when={!props.state.apiKey && !isSelection()}>
        <text content="Comandos · navegação visual" fg={colors.accent} />
        <text content={`${menuHeaderLabel(props.state)}${connected()}`} fg={colors.muted} />

        <box style={{ width: "100%", height: 1, flexDirection: "row" }}>
          <text content="Search " fg={colors.muted} />
          <text content={props.state.query || "digite no campo abaixo..."} fg={colors.text} />
          <box style={{ flexGrow: 1 }} />
          <box style={{ width: 4, height: 1 }} onMouseDown={() => props.onAction({ kind: "escape" })}>
            <text content="esc" fg={colors.muted} />
          </box>
        </box>

        <Show
          when={rows().length > 0}
          fallback={<text content="Nenhum item corresponde ao filtro." fg={colors.muted} />}
        >
          <For each={rows()}>
            {(item, offset) => {
              const selected = () => bounds().start + offset() === props.state.index
              const color = () =>
                selected() ? colors.accent : item.active ? colors.success : colors.text

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
                        content={`${markerOf(item.active, selected())} ${item.label}`}
                        fg={color()}
                      />
                    </box>
                    <box style={{ flexGrow: 1, height: 1 }}>
                      <text
                        content={item.detail}
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
        </Show>

        <text
          content="↑↓ navegar · Enter abrir/selecionar · Esc voltar/fechar · demo sem envio"
          fg={colors.muted}
        />
      </Show>
    </box>
  )
}
