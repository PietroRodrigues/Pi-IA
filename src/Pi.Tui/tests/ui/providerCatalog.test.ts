import { describe, expect, it } from "bun:test"
import {
  filterCatalogItems,
  filterProviderCatalog,
  groupCatalogItems,
  LOCAL_ADD_OPTION,
  LOCAL_MODELS,
  MODEL_PROVIDERS,
  PROGRAMS,
  PROVIDER_CATALOG,
  PROVIDERS,
  SETTINGS_INFO
} from "../../src/ui/providerCatalog"

describe("catálogo visual de provedores e modelos", () => {
  it("traz provedores e exemplos quantizados e não quantizados", () => {
    const labels = PROVIDER_CATALOG.map(item => item.label)

    expect(labels).toContain("Ollama (Local)")
    expect(labels).toContain("NVIDIA")
    expect(labels).toContain("OpenCode")
    expect(labels.some(label => label.toLowerCase().includes("iq2"))).toBe(true)
    expect(labels.some(label => label.toLowerCase().includes("q4"))).toBe(true)
    expect(labels.some(label => /^qwen2\.5:3b$/.test(label))).toBe(true)
  })

  it("filtra por provedor, quantização e caixa", () => {
    expect(filterProviderCatalog("nvidia").map(item => item.label)).toEqual(["NVIDIA"])
    expect(filterProviderCatalog("IQ2").map(item => item.label)).toEqual([
      "qwen3-6b-iq2-local"
    ])
    expect(filterProviderCatalog("q4").length).toBeGreaterThan(0)
    expect(filterProviderCatalog("")).toHaveLength(PROVIDER_CATALOG.length)
    expect(filterProviderCatalog("modelo-inexistente-xyz")).toEqual([])
  })

  it("agrupa por subtítulo depois de filtrar", () => {
    const groups = groupCatalogItems(PROVIDERS, "")
    expect(groups.map(group => group.title)).toEqual(["Provedores locais", "Provedores remotos"])

    const filtered = groupCatalogItems(LOCAL_MODELS, "IQ2")
    expect(filtered).toHaveLength(1)
    expect(filtered[0].title).toBe("Modelos no PC")
    expect(filtered[0].items[0].label).toBe("qwen3-6b-iq2-local")

    expect(groupCatalogItems(PROVIDER_CATALOG, "inexistente")).toEqual([])
  })

  it("expõe conjuntos separados para provedores, modelos, programas e configurações", () => {
    expect(PROVIDERS.map(item => item.label)).toEqual(["Ollama (Local)", "NVIDIA", "OpenCode"])
    expect(LOCAL_MODELS.every(item => item.group.includes("Modelos") || item.group.startsWith("Ollama"))).toBe(true)
    expect(PROGRAMS.map(item => item.label)).toEqual([
      "Ollama",
      "LM Studio",
      "llama.cpp",
      "ONNX Runtime"
    ])
    expect(MODEL_PROVIDERS.map(item => item.id)).toContain("pc")
    expect(SETTINGS_INFO.map(info => info.label)).toContain("Janela de contexto")
    expect(filterCatalogItems(LOCAL_MODELS, "q4").length).toBeGreaterThan(0)
    expect(LOCAL_ADD_OPTION.label).toBe("Adicionar modelo do PC")
  })
})
