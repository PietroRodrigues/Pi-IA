import { SyntaxStyle, type ScrollBoxRenderable } from "@opentui/core"
import { useRenderer } from "@opentui/solid"
import { For, Show, createEffect, onCleanup } from "solid-js"
import { theme } from "../theme"
import type { ChatMessage, ChatState } from "../state"

interface MessageListProps {
  state: ChatState
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
  if (message.role === "system") return message.text
  return message.text
}

function MessageBubble(props: { message: ChatMessage; syntaxStyle: SyntaxStyle }) {
  const barColor = () => {
    switch (props.message.role) {
      case "user":
        return theme.accent
      case "system":
        return theme.error
      case "info":
        return theme.accent
      case "tool":
        return props.message.ok === false ? theme.error : theme.accent
      default:
        return theme.muted
    }
  }

  const textColor = () => {
    switch (props.message.role) {
      case "system":
        return theme.error
      case "info":
        return theme.muted
      case "tool":
        return props.message.ok === false ? theme.error : theme.success
      case "thought":
        return theme.muted
      case "user":
        return theme.text
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
        backgroundColor: props.message.role === "user" ? theme.panelAlt : theme.background,
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
            bg={theme.background}
            style={{ width: "100%", flexGrow: 1 }}
          />
        </Show>
      </box>
    </box>
  )
}

export function MessageList(props: MessageListProps) {
  const renderer = useRenderer()
  const syntaxStyle = SyntaxStyle.create(renderer.nativeScene)
  onCleanup(() => syntaxStyle.destroy())

  let scrollRef: ScrollBoxRenderable | undefined

  const updateJump = () => {
    const scroll = scrollRef
    if (!scroll) {
      props.state.setJump(false)
      return
    }

    const viewportHeight = scroll.viewport.height
    const atBottom = scroll.scrollTop + 2 >= Math.max(0, scroll.scrollHeight - viewportHeight)
    props.state.setJump(!atBottom)
  }

  createEffect(() => {
    props.state.messages()
    props.state.jump()

    queueMicrotask(() => {
      if (!props.state.jump()) {
        scrollRef?.scrollTo(Number.POSITIVE_INFINITY)
      }
    })
  })

  return (
    <box style={{ width: "100%", flexGrow: 1, flexDirection: "column", minHeight: 0 }}>
      <Show when={props.state.jump()}>
        <box
          style={{
            width: "100%",
            backgroundColor: theme.panelAlt,
            paddingLeft: 1,
            paddingRight: 1
          }}
          onMouseDown={() => props.onJump()}
        >
          <text fg={theme.accent}>Jump to latest ↓</text>
        </box>
      </Show>

      <scrollbox
        ref={scrollRef}
        focused
        style={{
          width: "100%",
          flexGrow: 1,
          scrollY: true,
          rootOptions: { backgroundColor: theme.background },
          wrapperOptions: { backgroundColor: theme.background },
          viewportOptions: { backgroundColor: theme.background },
          contentOptions: { backgroundColor: theme.background },
          scrollbarOptions: {
            trackOptions: {
              foregroundColor: theme.accent,
              backgroundColor: theme.panel
            }
          }
        }}
        onMouseScroll={() => queueMicrotask(updateJump)}
      >
        <Show when={props.state.messages().length === 0}>
          <box style={{ padding: 2, backgroundColor: theme.background }}>
            <text fg={theme.accent}>Pi</text>
            <text fg={theme.muted}>Assistente local · Ollama · digite abaixo para começar</text>
          </box>
        </Show>

        <For each={props.state.messages()}>
          {message => <MessageBubble message={message} syntaxStyle={syntaxStyle} />}
        </For>
      </scrollbox>
    </box>
  )
}
