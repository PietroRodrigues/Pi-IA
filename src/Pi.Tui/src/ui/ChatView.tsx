// Área da conversa: bolhas, markdown só quando há conteúdo rico e atalho
// para voltar ao fim quando o usuário subiu o scroll.
import { SyntaxStyle, type ScrollBoxRenderable } from "@opentui/core"
import { useRenderer } from "@opentui/solid"
import { For, Show, createEffect, onCleanup } from "solid-js"
import type { ChatMessage } from "../state/reducers"
import type { SessionStore } from "../state/sessionStore"

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

function MessageBubble(props: { message: ChatMessage; syntaxStyle: SyntaxStyle }) {
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

  return (
    <box
      style={{
        width: "100%",
        flexDirection: "row",
        backgroundColor: props.message.role === "user" ? colors.panelAlt : colors.background,
        paddingLeft: 1,
        paddingRight: 1,
        paddingTop: 0,
        paddingBottom: 0,
        marginBottom: 1
      }}
    >
      <text fg={barColor()}>▎</text>
      <box style={{ flexGrow: 1, minWidth: 0, paddingLeft: 1, paddingRight: 1 }}>
        <Show
          when={rich()}
          fallback={
            <text
              content={labelOf(props.message) + (props.message.streaming ? " ▌" : "")}
              fg={textColor()}
            />
          }
        >
          <markdown
            content={props.message.text}
            streaming={props.message.streaming}
            syntaxStyle={props.syntaxStyle}
            fg={textColor()}
            bg={colors.background}
            style={{ width: "100%", flexGrow: 1 }}
          />
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
          contentOptions: { backgroundColor: colors.background },
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
          {message => <MessageBubble message={message} syntaxStyle={syntaxStyle} />}
        </For>
      </scrollbox>
    </box>
  )
}
