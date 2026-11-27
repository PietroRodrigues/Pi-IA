// Rodapé externo: onde o terminal foi aberto, branch Git e privilégio ADM na
// primeira linha; contexto e atalhos na segunda. Agente/modelo/status vivem
// dentro da caixa de entrada e o indicador de execução vive dentro do chat.
import { createSignal, onMount, Show } from "solid-js"
import { COLORS, COMMANDS, LAYOUT, LIMITS, TEXT } from "../constants"
import type { SessionStore } from "../state/sessionStore"
import {
  detectEnvironment,
  emptyEnvironment,
  environmentLabel,
  type EnvironmentSnapshot
} from "./environment"

export interface StatusBarProps {
  readonly store: SessionStore
  readonly notice?: string
}

export function StatusBar(props: StatusBarProps) {
  // Ambiente é best-effort: se a leitura falhar, mantemos o valor seguro.
  const [environment, setEnvironment] = createSignal<EnvironmentSnapshot>(emptyEnvironment)
  onMount(() => {
    void detectEnvironment().then(setEnvironment)
  })

  return (
    <box
      style={{
        width: LAYOUT.fullWidth,
        height: LAYOUT.statusBarHeight,
        flexDirection: COMMANDS.apiValues.column,
        backgroundColor: COLORS.panel,
        paddingLeft: LAYOUT.one,
        paddingRight: LAYOUT.one
      }}
    >
      <text content={environmentLabel(environment())} fg={COLORS.accent} />
      <box style={{ width: LAYOUT.fullWidth, height: LAYOUT.lineHeight, flexDirection: COMMANDS.apiValues.row }}>
        <text
          content={`${TEXT.statusBar.contextLabel}${props.store.context().percent.toFixed(LIMITS.zero)}${TEXT.statusBar.percent}`}
          fg={COLORS.text}
        />
        <Show when={props.notice}>
          <text content={`${TEXT.statusBar.noticePrefix}${props.notice}`} fg={COLORS.success} />
        </Show>
        <box style={{ flexGrow: LAYOUT.one }} />
        <text content={TEXT.environment.shortcuts} fg={COLORS.muted} />
      </box>
    </box>
  )
}
