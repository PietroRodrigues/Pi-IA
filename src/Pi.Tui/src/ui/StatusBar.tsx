// Rodapé externo: onde o terminal foi aberto, branch Git e privilégio ADM na
// primeira linha; contexto e atalhos na segunda. Agente/modelo/status vivem
// dentro da caixa de entrada e o indicador de execução vive dentro do chat.
import { createSignal, onMount, Show } from "solid-js"
import type { SessionStore } from "../state/sessionStore"
import {
  detectEnvironment,
  emptyEnvironment,
  environmentLabel,
  SHORTCUTS_LABEL,
  type EnvironmentSnapshot
} from "./environment"

const colors = {
  panel: "#141414",
  text: "#E5E5E5",
  muted: "#A3A3A3",
  accent: "#F59E0B",
  success: "#34D399"
} as const

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
        width: "100%",
        height: 2,
        flexDirection: "column",
        backgroundColor: colors.panel,
        paddingLeft: 1,
        paddingRight: 1
      }}
    >
      <text content={environmentLabel(environment())} fg={colors.accent} />
      <box style={{ width: "100%", height: 1, flexDirection: "row" }}>
        <text content={`Contexto: ${props.store.context().percent.toFixed(0)}%`} fg={colors.text} />
        <Show when={props.notice}>
          <text content={`  ${props.notice}`} fg={colors.success} />
        </Show>
        <box style={{ flexGrow: 1 }} />
        <text content={SHORTCUTS_LABEL} fg={colors.muted} />
      </box>
    </box>
  )
}
