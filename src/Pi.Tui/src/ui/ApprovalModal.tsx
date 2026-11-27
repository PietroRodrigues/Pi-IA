// Modal de aprovação de ferramentas. Não rouba o foco: o Ctrl/Y/N/Esc é
// tratado pelo controlador e a resposta volta como tool_decision.
import { Show } from "solid-js"
import { COLORS, COMMANDS, LAYOUT, LIMITS, PROVIDERS, TEXT } from "../constants"
import type { SessionStore } from "../state/sessionStore"

const riskLabel: Record<string, string> = {
  [PROVIDERS.riskKeys.readOnly]: TEXT.approval.riskLabels.ReadOnly,
  [PROVIDERS.riskKeys.write]: TEXT.approval.riskLabels.Write,
  [PROVIDERS.riskKeys.shell]: TEXT.approval.riskLabels.Shell
}

export function ApprovalModal(props: { store: SessionStore }) {
  return (
    <Show when={props.store.pendingToolRequest()}>
      {request => (
        <box
          style={{
            width: LAYOUT.fullWidth,
            height: LAYOUT.modalHeight,
            flexDirection: COMMANDS.apiValues.column,
            backgroundColor: COLORS.panelAlt,
            paddingLeft: LAYOUT.one,
            paddingRight: LAYOUT.one,
            paddingTop: LAYOUT.one
          }}
        >
          <text content={TEXT.approval.title} fg={COLORS.accent} />
          <text
            content={`${request().name}${TEXT.common.separator}${riskLabel[request().risk] ?? request().risk}`}
            fg={COLORS.text}
          />
          <text
            content={request().input.slice(0, LIMITS.approvalInputChars)}
            fg={COLORS.muted}
          />
          <text content={TEXT.approval.hint} fg={COLORS.error} />
        </box>
      )}
    </Show>
  )
}
