// Árvore visual do menu "/" da TUI.
// Origem: menu de comandos exibido no terminal
// Finalidade: navegação local; nenhum comando é enviado ao host.
export const COMMAND_TREE = [
    {
      segment: "providers",
      command: "/providers",
      description: "Conectar e ver provedores",
      kind: "group",
      children: [
        {
          segment: "list",
          command: "/providers/list",
          description: "Ver provedores disponíveis",
          kind: "selection",
          source: "providers"
        },
        {
          segment: "connect",
          command: "/providers/connect",
          description: "Conectar um provedor",
          kind: "connect",
          source: "providers"
        },
        {
          segment: "status",
          command: "/providers/status",
          description: "Status dos provedores",
          kind: "info",
          source: "providerStatus"
        }
      ]
    },
    {
      segment: "models",
      command: "/models",
      description: "Escolher modelos e provedores",
      kind: "group",
      children: [
        {
          segment: "providers",
          command: "/models/providers",
          description: "Modelos por provedor",
          kind: "group",
          dynamic: "modelProviders"
        },
        {
          segment: "active",
          command: "/models/active",
          description: "Modelo ativo",
          kind: "selection",
          source: "models"
        }
      ]
    },
    {
      segment: "local-models",
      command: "/local-models",
      description: "Modelos e programas no PC",
      kind: "group",
      children: [
        {
          segment: "app",
          command: "/local-models/app",
          description: "Programa que roda a IA",
          kind: "selection",
          source: "programs"
        },
        {
          segment: "list",
          command: "/local-models/list",
          description: "Ver modelos locais",
          kind: "selection",
          source: "localModels"
        },
        {
          segment: "add",
          command: "/local-models/add",
          description: "Adicionar modelo do PC",
          kind: "local-add"
        },
        {
          segment: "scan",
          command: "/local-models/scan",
          description: "Procurar modelos (visual)",
          kind: "info",
          source: "localScan"
        }
      ]
    },
    {
      segment: "agents",
      command: "/agents",
      description: "Agentes e comportamento",
      kind: "group",
      children: [
        {
          segment: "list",
          command: "/agents/list",
          description: "Ver agentes",
          kind: "selection",
          source: "agents"
        },
        {
          segment: "behavior",
          command: "/agents/behavior",
          description: "Comportamento do agente",
          kind: "selection",
          source: "behaviors"
        }
      ]
    },
    {
      segment: "session",
      command: "/session",
      description: "Sessões da conversa",
      kind: "group",
      children: [
        {
          segment: "list",
          command: "/session/list",
          description: "Ver sessões",
          kind: "selection",
          source: "sessions"
        },
        {
          segment: "new",
          command: "/session/new",
          description: "Nova sessão (visual)",
          kind: "info",
          source: "sessionNew"
        },
        {
          segment: "clear",
          command: "/session/clear",
          description: "Limpar sessão (visual)",
          kind: "info",
          source: "sessionClear"
        },
        {
          segment: "summarize",
          command: "/session/summarize",
          description: "Resumir sessão (visual)",
          kind: "info",
          source: "sessionSummarize"
        }
      ]
    },
    {
      segment: "system",
      command: "/system",
      description: "Aparência e sistema",
      kind: "group",
      children: [
        {
          segment: "appearance",
          command: "/system/appearance",
          description: "Aparência da TUI",
          kind: "selection",
          source: "appearances"
        },
        {
          segment: "settings",
          command: "/system/settings",
          description: "Configurações e ferramentas",
          kind: "group",
          children: [
            {
              segment: "current",
              command: "/system/settings/current",
              description: "Configurações atuais",
              kind: "info",
              source: "systemSettings"
            },
            {
              segment: "tools",
              command: "/system/settings/tools",
              description: "Ferramentas e segurança",
              kind: "group",
              children: [
                {
                  segment: "list",
                  command: "/system/settings/tools/list",
                  description: "Ativar ou desativar ferramentas",
                  kind: "selection",
                  source: "tools"
                },
                {
                  segment: "safety",
                  command: "/system/settings/tools/safety",
                  description: "Regras de segurança",
                  kind: "info",
                  source: "toolSafety"
                }
              ]
            }
          ]
        },
        {
          segment: "health",
          command: "/system/health",
          description: "Saúde do sistema",
          kind: "info",
          source: "systemHealth"
        }
      ]
    }
] as const
