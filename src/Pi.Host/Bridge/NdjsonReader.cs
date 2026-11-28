// Leitura assíncrona de stdin com buffer limitado: uma linha NDJSON por mensagem.
using System.Text;
using Pi.Contracts.Constants;

namespace Pi.Host.Bridge;

public sealed class NdjsonReader(
    TextReader reader,
    int maxLineLength = PiConstants.Limits.MaxNdjsonLineLength)
{
    private readonly char[] _chunk = new char[PiConstants.Limits.NdjsonChunkSize];
    private readonly StringBuilder _line = new();
    private int _start;
    private int _end;

    public async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        _line.Clear();

        while (true)
        {
            if (_start == _end)
            {
                _end = await reader.ReadAsync(_chunk, cancellationToken);
                _start = 0;

                if (_end == 0)
                {
                    if (_line.Length == 0)
                    {
                        return null;
                    }

                    EnsureWithinLimit();
                    return TakeLine();
                }
            }

            var newline = Array.IndexOf(_chunk, PiConstants.Protocol.LineFeed, _start, _end - _start);
            if (newline >= 0)
            {
                _line.Append(_chunk, _start, newline - _start);
                EnsureWithinLimit();
                _start = newline + 1;
                return TakeLine();
            }

            _line.Append(_chunk, _start, _end - _start);
            _start = _end;
            EnsureWithinLimit();
        }
    }

    // Mede apenas o conteúdo da linha atual, em bytes UTF-8, nos três caminhos de saída.
    private void EnsureWithinLimit()
    {
        if (_line.Length * PiConstants.Limits.MaxUtf8BytesPerChar <= maxLineLength)
        {
            return;
        }

        if (Encoding.UTF8.GetByteCount(_line.ToString()) > maxLineLength)
        {
            throw new InvalidDataException(string.Format(
                PiConstants.Messages.NdjsonLineTooLongTemplate,
                maxLineLength));
        }
    }

    private string TakeLine()
    {
        var value = _line.ToString().TrimEnd(PiConstants.Protocol.CarriageReturn);
        _line.Clear();
        return value;
    }
}
