// View raiz: monta a cena, repassa a interação ao Controller e desenha o Model.
// Nenhuma decisão de negócio (eventos, teclado, saída) fica mais aqui.
import type { InputRenderable } from "@opentui/core"
import { useKeyboard, useRenderer } from "@opentui/solid"
import { onCleanup, onMount } from "solid-js"
import { AppController, type ControllerViewPort } from "../controller/AppController"
import { createChatModel } from "../model/ChatModel"
import { Composer } from "./Composer"
import { Footer } from "./Footer"
import { MessageList } from "./MessageList"
import { theme } from "./theme"

export function App() {
  const renderer = useRenderer()
  const model = createChatModel()

  let inputRef: InputRenderable | undefined

  const viewPort: ControllerViewPort = {
    focusInput: () => inputRef?.focus(),
    close: async () => {
      renderer.destroy()
      // closed é necessário antes de process.exit(); o timeout evita travar
      // caso a cena nativa não conclua a restauração do terminal.
      await Promise.race([
        renderer.closed.catch(() => undefined),
        new Promise<void>(resolve => setTimeout(resolve, 1000))
      ])
    }
  }

  const controller = new AppController({ model, view: viewPort })

  useKeyboard(key => controller.onKey(key))

  onMount(() => controller.start())
  onCleanup(() => controller.dispose())

  return (
    <box
      style={{
        width: "100%",
        height: "100%",
        flexDirection: "column",
        backgroundColor: theme.background
      }}
    >
      <box style={{ width: "100%", flexGrow: 1, flexDirection: "column", minHeight: 0 }}>
        <MessageList model={model} onJump={() => model.setJump(false)} />
        <Composer onSubmit={text => controller.submit(text)} busy={model.busy} onRef={input => (inputRef = input)} />
      </box>

      <Footer model={model} />
    </box>
  )
}
