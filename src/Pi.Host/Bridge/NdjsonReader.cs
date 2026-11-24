// Leitura assíncrona de stdin com buffer limitado: uma linha NDJSON por mensagem.
using System.Text;

namespace Pi.Host.Bridge;

public sealed class NdjsonReader(TextReader reader, int maxLineLength = 1_048_576)
{
    private readonly char[] _chunk = new char[4096];
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
                    return _line.Length == 0 ? null : TakeLine();
                }
            }

            var newline = Array.IndexOf(_chunk, '\n', _start, _end - _start);
            if (newline >= 0)
            {
                _line.Append(_chunk, _start, newline - _start);
                _start = newline + 1;
                return TakeLine();
            }

            _line.Append(_chunk, _start, _end - _start);
            _start = _end;

            if (_line.Length > maxLineLength)
            {
                throw new InvalidDataException($"Linha NDJSON acima do limite de {maxLineLength} caracteres.");
            }
        }
    }

    private string TakeLine()
    {
        var value = _line.ToString().TrimEnd('\r');
        _line.Clear();
        return value;
    }
}
