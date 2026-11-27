// Ponto único de import da TUI. Os valores estão divididos por domínio na pasta
// constants/; este arquivo apenas os reexporta para preservar `from "../constants"`.
// appsettings/.env continuam configuráveis: aqui ficam os padrões e nomes do código.
export * from "./constants/environment"
export * from "./constants/commands"
export * from "./constants/catalogs"
export * from "./constants/protocol"
export * from "./constants/runtime"
export * from "./constants/visual"
export * from "./constants/text"
