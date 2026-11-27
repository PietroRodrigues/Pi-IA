// Catálogos demonstrativos exibidos nos painéis de seleção.

// Endpoints de referência dos provedores demonstrados no catálogo visual.
// A TUI não envia requisições: os valores servem como documentação central.
// Origem: documentação dos provedores
// Finalidade: URLs de referência exibidas no catálogo visual.
export const ENDPOINTS = {
  hosts: {
    ollama: "http://127.0.0.1:11434",
    nvidia: "https://integrate.api.nvidia.com/v1",
    openCode: "http://127.0.0.1:3000",
    lmStudio: "http://127.0.0.1:1234",
    llamaCpp: "http://127.0.0.1:8080"
  },
  ports: {
    ollama: 11434,
    openCode: 3000,
    lmStudio: 1234,
    llamaCpp: 8080
  },
  paths: {
    tags: "/api/tags",
    models: "/v1/models"
  }
} as const

// Origem: catálogo de provedores demonstrados na TUI
// Finalidade: ids, rótulos e agrupamentos exibidos no menu.
export const PROVIDERS = {
  ids: {
    ollama: "ollama",
    nvidia: "nvidia",
    opencode: "opencode",
    pc: "pc",
    lmStudio: "lm-studio",
    llamaCpp: "llama-cpp",
    onnx: "onnx",
    qwenLocal: "qwen-local",
    qwenIq2: "qwen-iq2",
    qwen3b: "qwen-3b",
    qwen3bQ4: "qwen-3b-q4",
    llamaQ4: "llama-q4",
    llama3b: "llama-3b",
    deepseekQ4: "deepseek-q4",
    phi4: "phi4"
  },
  labels: {
    ollamaLocal: "Ollama (Local)",
    ollama: "Ollama",
    nvidia: "NVIDIA",
    openCode: "OpenCode",
    pc: "PC (local)",
    lmStudio: "LM Studio",
    llamaCpp: "llama.cpp",
    onnx: "ONNX Runtime"
  },
  groups: {
    providerPrefix: "Provedores",
    modelsMarker: "Modelos",
    ollamaMarker: "Ollama",
    local: "Provedores locais",
    remote: "Provedores remotos",
    modelsPc: "Modelos no PC",
    ollamaQuantized: "Ollama · quantizados",
    ollamaUnquantized: "Ollama · não quantizados",
    programs: "Programas no PC",
    agents: "Agentes",
    behaviors: "Comportamento",
    tools: "Ferramentas",
    sessions: "Sessões",
    appearances: "Aparência",
    localAdd: "No PC"
  },
  details: {
    localDefault: "provedor local · padrão",
    remoteDemo: "provedor remoto · demonstração",
    localModel: "modelo local · não quantizado",
    localIq2: "modelo local · IQ2",
    ollamaModel: "Ollama · não quantizado",
    ollamaQ4: "Ollama · Q4_K_M",
    ollamaQ40: "Ollama · Q4_0",
    pcModels: "modelos baixados no PC",
    ollamaServer: "servidor local",
    lmStudio: "interface local",
    llamaCpp: "execução direta",
    onnx: "modelos ONNX"
  },
  riskKeys: {
    readOnly: "ReadOnly",
    write: "Write",
    shell: "Shell"
  },
  catalog: {
    agents: [
      { id: "leitor", label: "Leitor", detail: "só leitura" },
      { id: "escritor", label: "Escritor", detail: "edita arquivos" },
      { id: "revisor", label: "Revisor", detail: "revisa mudanças" }
    ],
    behaviors: [
      { id: "curto", label: "Curto", detail: "respostas breves" },
      { id: "equilibrado", label: "Equilibrado", detail: "padrão visual" },
      { id: "detalhado", label: "Detalhado", detail: "mais contexto" }
    ],
    tools: [
      { id: "read_file", label: "read_file", detail: "leitura" },
      { id: "edit_file", label: "edit_file", detail: "escrita" },
      { id: "search_code", label: "search_code", detail: "busca" },
      { id: "execute_shell", label: "execute_shell", detail: "desativada" }
    ],
    sessions: [
      { id: "atual", label: "Atual", detail: "em uso" },
      { id: "anterior", label: "Anterior", detail: "visual" },
      { id: "arquivada", label: "Arquivada", detail: "visual" }
    ],
    appearances: [
      { id: "ambar", label: "Âmbar", detail: "padrão" },
      { id: "cinza", label: "Cinza", detail: "neutro" },
      { id: "contraste", label: "Alto contraste", detail: "acessível" }
    ],
    localAdd: {
      id: "add-local-model",
      label: "Adicionar modelo do PC",
      detail: "cole um caminho ou busque um diretório"
    }
  }
} as const

// Ferramentas demonstradas no menu visual: o estado fica somente na sessão da TUI.
// Origem: menu visual de Ferramentas
// Finalidade: ids padrão e estado inicial dos toggles na sessão.
export const TOOLS = {
  ids: {
    readFile: "read_file",
    editFile: "edit_file",
    searchCode: "search_code",
    executeShell: "execute_shell"
  },
  enabledByDefault: ["read_file", "edit_file", "search_code"]
} as const

// Origem: modelos disponíveis no catálogo visual
// Finalidade: ids, rótulos e valores de fallback usados pela TUI.
export const MODELS = {
  ids: {
    qwen38Local: "qwen-local",
    qwen36Iq2: "qwen-iq2",
    qwen25_3b: "qwen-3b",
    qwen25_3bQ4: "qwen-3b-q4",
    llama32_3b: "llama-3b",
    llama32_3bQ4: "llama-q4",
    deepseekR1Q4: "deepseek-q4",
    phi4: "phi4"
  },
  labels: {
    qwen38Local: "qwen3.8-2b-local",
    qwen36Iq2: "qwen3-6b-iq2-local",
    qwen25_3b: "qwen2.5:3b",
    qwen25_3bQ4: "qwen2.5:3b-q4_K_M",
    llama32_3b: "llama3.2:3b",
    llama32_3bQ4: "llama3.2:3b-q4_0",
    deepseekR1Q4: "deepseek-r1:7b-q4_K_M",
    phi4: "phi4:14b"
  },
  fallbacks: {
    provider: "Ollama (Local)",
    model: "qwen3.8-2b-local",
    runtime: "Ollama"
  }
} as const
