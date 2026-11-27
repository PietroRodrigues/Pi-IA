// Área da conversa: bolhas, markdown só quando há conteúdo rico e atalho
// para voltar ao fim quando o usuário subiu o scroll.
import { SyntaxStyle, type ScrollBoxRenderable } from "@opentui/core"
import { useRenderer } from "@opentui/solid"
import { For, Show, createEffect, createSignal, onCleanup } from "solid-js"
import {
  COLORS,
  COMMANDS,
  GLYPHS,
  LAYOUT,
  LIMITS,
  ROLES,
  TEXT,
  TIME
} from "../constants"
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
  hasChatSideBar,
  summarySpacerLines,
  userBarGlyphs
} from "./theme"

interface ChatViewProps {
  store: SessionStore
  onJump: () => void
}

function hasRichContent(text: string): boolean {
  return (
    text.includes(TEXT.chat.markdown.fence) ||
    new RegExp(TEXT.chat.markdown.tablePattern).test(text) ||
    new RegExp(TEXT.chat.markdown.headingPattern).test(text)
  )
}

function labelOf(message: ChatMessage): string {
  if (message.role === ROLES.thought) {
    return `${TEXT.chat.thoughtPrefix}${message.text}`
  }
  if (message.role === ROLES.tool) {
    const name = message.name ?? TEXT.chat.toolFallbackName
    if (message.ok === undefined) {
      return `${GLYPHS.gear}${GLYPHS.space}${name}${TEXT.common.separator}${message.text}`
    }
    return `${message.ok ? GLYPHS.check : GLYPHS.cross}${GLYPHS.space}${name}${TEXT.common.separator}${message.text}`
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
      case ROLES.user:
        return COLORS.accent
      case ROLES.system:
        return COLORS.error
      case ROLES.info:
        return COLORS.accent
      case ROLES.tool:
        return props.message.ok === false ? COLORS.error : COLORS.accent
      default:
        return COLORS.muted
    }
  }

  const textColor = () => {
    switch (props.message.role) {
      case ROLES.system:
        return COLORS.error
      case ROLES.info:
        return COLORS.muted
      case ROLES.tool:
        return props.message.ok === false ? COLORS.error : COLORS.success
      case ROLES.thought:
        return COLORS.muted
      case ROLES.user:
        return COLORS.text
      default:
        return COLORS.assistantText
    }
  }

  const rich = () =>
    props.message.role === ROLES.assistant &&
    !props.message.streaming &&
    hasRichContent(props.message.text)

  const isUser = () => props.message.role === ROLES.user
  const showBar = () => hasChatSideBar(props.message.role)
  const barGlyphs = () =>
    userBarGlyphs(
      props.message.text,
      summarySpacerLines(Boolean(props.summary)) + (props.summary ? LAYOUT.one : LAYOUT.zero),
      props.barWidth
    )

  return (
    <box
      style={{
        width: LAYOUT.fullWidth,
        flexDirection: COMMANDS.apiValues.row,
        backgroundColor: isUser() ? COLORS.panelAlt : COLORS.background,
        paddingLeft: LAYOUT.one,
        paddingRight: LAYOUT.chat.bubblePaddingRight,
        paddingTop: LAYOUT.one,
        paddingBottom: LAYOUT.one,
        marginTop: isUser() ? LAYOUT.chat.userMarginTop : LAYOUT.zero,
        marginBottom: isUser() ? LAYOUT.chat.userMarginBottom : LAYOUT.chat.marginBottom
      }}
    >
      <Show when={showBar()}>
        <>
          <box style={{ width: LAYOUT.columnWidth, flexDirection: COMMANDS.apiValues.column }}>
            <For each={barGlyphs()}>
              {glyph => <text content={glyph} fg={COLORS.accent} />}
            </For>
          </box>
          <box style={{ width: LAYOUT.gutterWidth }} />
        </>
      </Show>
      <Show when={!showBar() && props.message.role !== ROLES.assistant}>
        <box style={{ width: LAYOUT.columnWidth, height: LAYOUT.lineHeight }}>
          <text content={GLYPHS.thinBar} fg={barColor()} />
        </box>
        <box style={{ width: LAYOUT.gutterWidth }} />
      </Show>
      <box style={{ flexGrow: LAYOUT.one, minWidth: LAYOUT.zero, paddingRight: LAYOUT.one }}>
        <Show when={props.thinking}>
          <text content={props.thinking!} fg={COLORS.muted} />
        </Show>
        <Show when={!props.thinking && rich()}>
          <markdown
            content={props.message.text}
            streaming={props.message.streaming}
            syntaxStyle={props.syntaxStyle}
            fg={textColor()}
            bg={COLORS.background}
            style={{ width: LAYOUT.fullWidth, flexGrow: LAYOUT.one }}
          />
        </Show>
        <Show
          when={
            !props.thinking &&
            !rich() &&
            (props.message.text.length > 0 || props.message.role !== ROLES.assistant)
          }
        >
          <text
            content={
              labelOf(props.message) +
              (props.message.streaming
                ? `${GLYPHS.space}${GLYPHS.streamCursor}`
                : TEXT.common.empty)
            }
            fg={textColor()}
          />
        </Show>

        <Show when={props.summary}>
          <>
            <box style={{ height: summarySpacerLines(true) }} />
            <text content={props.summary!} fg={COLORS.muted} />
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
  const [tick, setTick] = createSignal<number>(LIMITS.zero)

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
    setTick(LIMITS.zero)
    thinkingTimer = setInterval(
      () => setTick(current => current + LAYOUT.one),
      TIME.thinkingIntervalMs
    )
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

    const elapsedMs =
      startedAt === undefined ? LIMITS.zero : Math.max(LIMITS.zero, Date.now() - startedAt)
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
      props.store.dispatch({ kind: COMMANDS.actionKinds.jump, jump: false })
      return
    }

    const viewportHeight = scroll.viewport.height
    const atBottom =
      scroll.scrollTop + LIMITS.scrollBottomTolerance >=
      Math.max(LIMITS.zero, scroll.scrollHeight - viewportHeight)
    props.store.dispatch({ kind: COMMANDS.actionKinds.jump, jump: !atBottom })
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
    <box
      style={{
        width: LAYOUT.fullWidth,
        flexGrow: LAYOUT.one,
        flexDirection: COMMANDS.apiValues.column,
        minHeight: LAYOUT.zero
      }}
    >
      <Show when={props.store.jump()}>
        <box
          style={{
            width: LAYOUT.fullWidth,
            backgroundColor: COLORS.panelAlt,
            paddingLeft: LAYOUT.one,
            paddingRight: LAYOUT.one
          }}
          onMouseDown={props.onJump}
        >
          <text fg={COLORS.accent}>{TEXT.chat.jumpLatest}</text>
        </box>
      </Show>

      <scrollbox
        ref={scrollRef}
        focused
        style={{
          width: LAYOUT.fullWidth,
          flexGrow: LAYOUT.one,
          scrollY: true,
          rootOptions: { backgroundColor: COLORS.background },
          wrapperOptions: { backgroundColor: COLORS.background },
          viewportOptions: { backgroundColor: COLORS.background },
          contentOptions: {
            backgroundColor: COLORS.background,
            paddingLeft: LAYOUT.chat.marginX,
            paddingRight: LAYOUT.chat.marginRight
          },
          scrollbarOptions: {
            trackOptions: {
              foregroundColor: COLORS.accent,
              backgroundColor: COLORS.panel
            }
          }
        }}
        onMouseScroll={() => queueMicrotask(updateJump)}
      >
        <Show when={props.store.messages().length === LIMITS.zero}>
          <box style={{ padding: LAYOUT.chat.bubblePadding, backgroundColor: COLORS.background }}>
            <text fg={COLORS.accent}>{TEXT.chat.emptyTitle}</text>
            <text fg={COLORS.muted}>{TEXT.chat.emptyHint}</text>
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
