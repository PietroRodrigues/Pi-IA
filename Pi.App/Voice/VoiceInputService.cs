using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Pi.App.Voice
{
    public class VoiceInputService
    {
        public async Task<string> CaptureAsync(int durationMs = 5000)
        {
            string outputPath = "recording.wav";

            using var waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1) // 16kHz mono
            };

            WaveFileWriter? writer = null;
            var completion = new TaskCompletionSource<bool>();

            // Handlers nomeados para permitir unsubscribe seguro
            EventHandler<WaveInEventArgs> dataHandler = (s, e) =>
            {
                // Escreve bytes recebidos no arquivo
                writer?.Write(e.Buffer, 0, e.BytesRecorded);
            };

            EventHandler<StoppedEventArgs> stopHandler = (s, e) =>
            {
                // Garante que o writer seja finalizado quando parar
                try { writer?.Dispose(); } catch { }
                completion.TrySetResult(true);
            };

            waveIn.DataAvailable += dataHandler;
            waveIn.RecordingStopped += stopHandler;

            try
            {
                writer = new WaveFileWriter(outputPath, waveIn.WaveFormat);

                waveIn.StartRecording();

                // Aguarda o tempo de gravação e solicita parada
                await Task.Delay(durationMs);
                waveIn.StopRecording();

                // Aguarda o evento RecordingStopped finalizar a limpeza
                await completion.Task;

                return outputPath;
            }
            finally
            {
                // Remove handlers e garante dispose
                waveIn.DataAvailable -= dataHandler;
                waveIn.RecordingStopped -= stopHandler;
                writer?.Dispose();
            }
        }
    }
}