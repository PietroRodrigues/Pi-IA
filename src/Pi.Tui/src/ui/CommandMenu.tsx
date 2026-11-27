// Menu "/" em etapas: raiz e submenus em duas colunas; folhas de seleção usam
// o SelectionPanel. O campo de chat continua com o foco e controla o filtro.
import { createMemo, For, Show } from "solid-js"
import { COLORS, COMMANDS, GLYPHS, LAYOUT, LIMITS, TEXT } from "../constants"
import type { CommandMenuAction, CommandMenuState } from "./commandMenuState"
import { currentNode, menuHeaderLabel, selectionGroups, visibleItems } from "./commandMenuState"
import {
  selectionMenuHeight,
  simpleMenuHeight,
  visibleGroupLayout,
  windowBounds
} from "./menuLayout"
import { LOCAL_ADD_OPTION } from "./providerCatalog"
import { ApiKeyModal } from "./ApiKeyModal"
import { SelectionPanel } from "./SelectionPanel"
import { markerOf } from "./theme"

interface CommandMenuProps {
  readonly state: CommandMenuState
  readonly onAction: (action: CommandMenuAction) => void
}

export function CommandMenu(props: CommandMenuProps) {
  const items = createMemo(() => visibleItems(props.state))
  const node = createMemo(() => currentNode(props.state))
  const groups = createMemo(() => selectionGroups(props.state))
  const kind = () => node()?.kind ?? COMMANDS.nodeKinds.root
  const isSelection = () =>
    kind() === COMMANDS.nodeKinds.selection ||
    kind() === COMMANDS.nodeKinds.connect ||
    kind() === COMMANDS.nodeKinds.localAdd
  const pinned = () => (kind() === COMMANDS.nodeKinds.localAdd ? LOCAL_ADD_OPTION : undefined)

  const bounds = createMemo(() =>
    windowBounds(props.state.index, items().length, LAYOUT.maxMenuItems))
  const rows = createMemo(() => items().slice(bounds().start, bounds().end))

  const selectionBounds = createMemo(() => {
    const total = groups().reduce((sum, group) => sum + group.items.length, 0)
    return windowBounds(props.state.index, total, LAYOUT.maxSelectionItems)
  })
  const selectionLayout = createMemo(() => visibleGroupLayout(groups(), selectionBounds()))

  const height = () => {
    if (props.state.apiKey) return LAYOUT.modalHeight
    if (isSelection()) return selectionMenuHeight(selectionLayout(), Boolean(pinned()))
    return simpleMenuHeight(rows().length, Boolean(subtitle()))
  }

  const connected = () => {
    const names = props.state.connectedProviders
    return names.length > 0
      ? `${TEXT.menu.connectedSuffix}${names.join(TEXT.common.commaSpace)}`
      : TEXT.common.empty
  }

  const subtitle = (): string | undefined => {
    const path = props.state.path
    if (path.length === LIMITS.zero) return undefined
    const segment = path[path.length - LIMITS.one]
    return TEXT.menu.subtitles[segment as keyof typeof TEXT.menu.subtitles] ?? segment
  }

  const isToolList = () => node()?.source === COMMANDS.catalogKinds.tools

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
      <Show when={props.state.apiKey}>
        {key => (
          <ApiKeyModal
            window={key()}
            onType={value => props.onAction({ kind: COMMANDS.actionKinds.apiKeyType, value })}
            onSubmit={() => props.onAction({ kind: COMMANDS.actionKinds.apiKeySubmit })}
            onCancel={() => props.onAction({ kind: COMMANDS.actionKinds.apiKeyCancel })}
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
          enabledIds={isToolList() ? props.state.enabledTools : undefined}
          onEscape={() => props.onAction({ kind: COMMANDS.actionKinds.escape })}
        />
      </Show>

      <Show when={!props.state.apiKey && !isSelection()}>
        <text content={TEXT.menu.title} fg={COLORS.accent} />
        <box style={{ height: LAYOUT.menuGapLines }} />

        <box style={{ width: LAYOUT.fullWidth, height: LAYOUT.lineHeight, flexDirection: COMMANDS.apiValues.row }}>
          <text content={TEXT.menu.search} fg={COLORS.muted} />
          <text
            content={props.state.query || TEXT.menu.searchPlaceholder}
            fg={COLORS.text}
          />
          <box style={{ flexGrow: LAYOUT.one }} />
          <box
            style={{ width: LAYOUT.searchBoxWidth, height: LAYOUT.lineHeight }}
            onMouseDown={() => props.onAction({ kind: COMMANDS.actionKinds.escape })}
          >
            <text content={TEXT.menu.esc} fg={COLORS.muted} />
          </box>
        </box>
        <box style={{ height: LAYOUT.menuGapLines }} />

        <Show when={subtitle()}>
          {label => (
            <>
              <box
                style={{
                  width: LAYOUT.fullWidth,
                  height: LAYOUT.subtitleLines,
                  paddingLeft: LAYOUT.subtitleIndent
                }}
              >
                <text content={label()} fg={COLORS.accent} />
              </box>
              <box style={{ height: LAYOUT.menuGapLines }} />
            </>
          )}
        </Show>

        <Show
          when={rows().length > 0}
          fallback={<text content={TEXT.menu.noMatches} fg={COLORS.muted} />}
        >
          <For each={rows()}>
            {(item, offset) => {
              const selected = () => bounds().start + offset() === props.state.index
              const color = () =>
                selected() ? COLORS.accent : item.active ? COLORS.success : COLORS.text

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
                        content={`${markerOf(item.active, selected())}${GLYPHS.space}${item.label}`}
                        fg={color()}
                      />
                    </box>
                    <box style={{ flexGrow: LAYOUT.one, height: LAYOUT.lineHeight }}>
                      <text
                        content={item.detail}
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
        </Show>

        <box style={{ height: LAYOUT.menuGapLines }} />
        <text content={TEXT.menu.footer} fg={COLORS.muted} />
      </Show>
    </box>
  )
}
