// Área da conversa: bolhas, markdown só quando há conteúdo rico e atalho
// para voltar ao fim quando o usuário subiu o scroll.
import { SyntaxStyle, type ScrollBoxRenderable } from "@opentui/core"
import { useRenderer } from "@opentui/solid"
import { For, Show, createEffect, createSignal, onCleanup } from "solid-js"
import type { ChatMessage } from "../state/reducers"
import type { SessionStore } from "../state/sessionStore"
import { isThinkingTurn, thinkingText } from "./chatIndicators"
import {
  finishedResponse,
  responseSummaryLine,
  streamingResponseId,
  tokenDelta
} from "./responseMetrics"
import {
  CHAT_LAYOUT,
  hasChatSideBar,
  summarySpacerLines,
  THIN_BAR_GLYPH,
  USER_BAR_COLOR,
  userBarGlyphs
} from "./theme"

const colors = {
  background: "#0A0A0A",
  panelAlt: "#1E1E1E",
  panel: "#141414",
  text: "#E5E5E5",
  muted: "#A3A3A3",
  accent: "#F59E0B",
  error: "#F87171",
  success: "#34D399"
} as const

interface ChatViewProps {
  store: SessionStore
  onJump: () => void
}

function hasRichContent(text: string): boolean {
  return (
    text.includes("```") ||
    /(^|\n)\s*\|.*\|\s*(\n|$)/.test(text) ||
    /(^|\n)\s{0,3}#{1,6}\s/.test(text)
  )
}

function labelOf(message: ChatMessage): string {
  if (message.role === "thought") return `pensamento · ${message.text}`
  if (message.role === "tool") {
    const name = message.name ?? "ferramenta"
    if (message.ok === undefined) return `⚙ ${name} · ${message.text}`
    return `${message.ok ? "✓" : "✗"} ${name} · ${message.text}`
  }
  return message.text
}

function MessageBubble(props: {
  message: ChatMessage
  syntaxStyle: SyntaxStyle
  summary?: string
  barWidth: number
  thinking?: string
}) {
  const barColor = () => {
    switch (props.message.role) {
      case "user":
        return colors.accent
      case "system":
        return colors.error
      case "info":
        return colors.accent
      case "tool":
        return props.message.ok === false ? colors.error : colors.accent
      default:
        return colors.muted
    }
  }

  const textColor = () => {
    switch (props.message.role) {
      case "system":
        return colors.error
      case "info":
        return colors.muted
      case "tool":
        return props.message.ok === false ? colors.error : colors.success
      case "thought":
        return colors.muted
      case "user":
        return colors.text
      default:
        return "#D4D4D4"
    }
  }

  const rich = () =>
    props.message.role === "assistant" &&
    !props.message.streaming &&
    hasRichContent(props.message.text)

  const isUser = () => props.message.role === "user"
  const showBar = () => hasChatSideBar(props.message.role)
  const barGlyphs = () =>
    userBarGlyphs(
      props.message.text,
      summarySpacerLines(Boolean(props.summary)) + (props.summary ? 1 : 0),
      props.barWidth
    )

  return (
    <box
      style={{
        width: "100%",
        flexDirection: "row",
        backgroundColor: isUser() ? colors.panelAlt : colors.background,
        paddingLeft: 1,
        paddingRight: 2,
        paddingTop: 1,
        paddingBottom: 1,
        marginTop: isUser() ? CHAT_LAYOUT.userMarginTop : 0,
        marginBottom: isUser() ? CHAT_LAYOUT.userMarginBottom : CHAT_LAYOUT.marginBottom
      }}
    >
      <Show when={showBar()}>
        <>
          <box style={{ width: 1, flexDirection: "column" }}>
            <For each={barGlyphs()}>
              {glyph => <text content={glyph} fg={USER_BAR_COLOR} />}
            </For>
          </box>
          <box style={{ width: 1 }} />
        </>
      </Show>
      <Show when={!showBar() && props.message.role !== "assistant"}>
        <box style={{ width: 1, height: 1 }}>
          <text content={THIN_BAR_GLYPH} fg={barColor()} />
        </box>
        <box style={{ width: 1 }} />
      </Show>
      <box style={{ flexGrow: 1, minWidth: 0, paddingRight: 1 }}>
        <Show when={props.thinking}>
          <text content={props.thinking!} fg={colors.muted} />
        </Show>
        <Show when={!props.thinking && rich()}>
          <markdown
            content={props.message.text}
            streaming={props.message.streaming}
            syntaxStyle={props.syntaxStyle}
            fg={textColor()}
            bg={colors.background}
            style={{ width: "100%", flexGrow: 1 }}
          />
        </Show>
        <Show
          when={
            !props.thinking &&
            !rich() &&
            (props.message.text.length > 0 || props.message.role !== "assistant")
          }
        >
          <text
            content={labelOf(props.message) + (props.message.streaming ? " ▌" : "")}
            fg={textColor()}
          />
        </Show>

        <Show when={props.summary}>
          <>
            <box style={{ height: summarySpacerLines(true) }} />
            <text content={props.summary!} fg={colors.muted} />
          </>
        </Show>
      </box>
    </box>
  )
}

export function ChatView(props: ChatViewProps) {
  const renderer = useRenderer()
  const syntaxStyle = SyntaxStyle.create(renderer.nativeScene)
  onCleanup(() => syntaxStyle.destroy())

  let scrollRef: ScrollBoxRenderable | undefined
  let thinkingTimer: ReturnType<typeof setInterval> | undefined
  const [tick, setTick] = createSignal(0)

  const stopThinking = () => {
    if (thinkingTimer === undefined) return
    clearInterval(thinkingTimer)
    thinkingTimer = undefined
  }

  createEffect(() => {
    if (!isThinkingTurn(props.store.busy(), props.store.messages())) {
      stopThinking()
      return
    }

    if (thinkingTimer !== undefined) return
    setTick(0)
    thinkingTimer = setInterval(() => setTick(current => current + 1), 350)
  })
  onCleanup(stopThinking)

  // Medição local do turno: começa quando o campo entra em streaming e só
  // exibe o resumo quando a mesma mensagem termina. Sem delta válido, 0.0 tok/s.
  const [summaries, setSummaries] = createSignal<Readonly<Record<string, string>>>({})
  let responseId: string | undefined
  let startedAt: number | undefined
  let startTokens: number | undefined

  createEffect(() => {
    const busy = props.store.busy()
    const messages = props.store.messages()

    if (busy) {
      if (startedAt === undefined) {
        responseId = streamingResponseId(messages)
        startedAt = Date.now()
        startTokens = props.store.context().used
      }
      return
    }

    const finished = finishedResponse(responseId, messages)
    if (!finished) return

    const elapsedMs = startedAt === undefined ? 0 : Math.max(0, Date.now() - startedAt)
    const tokens = tokenDelta(props.store.context().used, startTokens)
    const session = props.store.session()
    const line = responseSummaryLine({
      agent: session.agent,
      model: session.model,
      elapsedMs,
      tokens
    })

    setSummaries(current => ({ ...current, [finished.id]: line }))
    responseId = undefined
    startedAt = undefined
    startTokens = undefined
  })

  const updateJump = () => {
    const scroll = scrollRef
    if (!scroll) {
      props.store.dispatch({ kind: "jump", jump: false })
      return
    }

    const viewportHeight = scroll.viewport.height
    const atBottom = scroll.scrollTop + 2 >= Math.max(0, scroll.scrollHeight - viewportHeight)
    props.store.dispatch({ kind: "jump", jump: !atBottom })
  }

  createEffect(() => {
    props.store.messages()
    props.store.jump()

    queueMicrotask(() => {
      if (!props.store.jump()) {
        scrollRef?.scrollTo(Number.POSITIVE_INFINITY)
      }
    })
  })

  return (
    <box style={{ width: "100%", flexGrow: 1, flexDirection: "column", minHeight: 0 }}>
      <Show when={props.store.jump()}>
        <box
          style={{
            width: "100%",
            backgroundColor: colors.panelAlt,
            paddingLeft: 1,
            paddingRight: 1
          }}
          onMouseDown={props.onJump}
        >
          <text fg={colors.accent}>Jump to latest ↓</text>
        </box>
      </Show>

      <scrollbox
        ref={scrollRef}
        focused
        style={{
          width: "100%",
          flexGrow: 1,
          scrollY: true,
          rootOptions: { backgroundColor: colors.background },
          wrapperOptions: { backgroundColor: colors.background },
          viewportOptions: { backgroundColor: colors.background },
          contentOptions: {
            backgroundColor: colors.background,
            paddingLeft: CHAT_LAYOUT.marginX,
            paddingRight: CHAT_LAYOUT.marginRight
          },
          scrollbarOptions: {
            trackOptions: {
              foregroundColor: colors.accent,
              backgroundColor: colors.panel
            }
          }
        }}
        onMouseScroll={() => queueMicrotask(updateJump)}
      >
        <Show when={props.store.messages().length === 0}>
          <box style={{ padding: 2, backgroundColor: colors.background }}>
            <text fg={colors.accent}>Pi</text>
            <text fg={colors.muted}>Assistente local · Ollama · digite abaixo para começar</text>
          </box>
        </Show>

        <For each={props.store.messages()}>
          {message => (
            <MessageBubble
              message={message}
              syntaxStyle={syntaxStyle}
              summary={summaries()[message.id]}
              barWidth={renderer.width}
              thinking={thinkingText(Boolean(props.store.busy()), [message], tick())}
            />
          )}
        </For>
      </scrollbox>
    </box>
  )
}
