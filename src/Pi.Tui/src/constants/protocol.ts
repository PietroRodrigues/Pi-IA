// Espelho TypeScript do contrato v1 definido em Pi.Contracts.Protocol.
import {
  COMMAND_TYPES,
  ERROR_CODES,
  EVENT_TYPES,
  PROTOCOL_VERSION
} from "../protocol/schema"

// Ponto único de import: os nomes originais também saem daqui, sem redefini-los.
export { COMMAND_TYPES, ERROR_CODES, EVENT_TYPES, PROTOCOL_VERSION } from "../protocol/schema"

// Origem: envelopes NDJSON trocados com Pi.Host
// Finalidade: chaves, códigos e razões usadas no transporte.
export const PROTOCOL = {
  version: PROTOCOL_VERSION,
  commandTypes: COMMAND_TYPES,
  eventTypes: EVENT_TYPES,
  errorCodes: ERROR_CODES,
  envelopeKeys: {
    version: "v",
    id: "id",
    correlationId: "correlationId",
    timestamp: "timestamp",
    type: "type",
    payload: "payload"
  },
  cancelReasons: {
    user: "user_cancel",
    shutdown: "shutdown"
  },
  io: {
    encoding: "utf8",
    newline: "\n",
    stdio: ["pipe", "pipe", "pipe"] as ["pipe", "pipe", "pipe"],
    clipboardStdio: ["pipe", "ignore", "ignore"] as ["pipe", "ignore", "ignore"],
    data: "data",
    error: "error",
    exit: "exit",
    close: "close",
    windowsHide: true,
    clipboardCommandExitCode: 0
  }
} as const
