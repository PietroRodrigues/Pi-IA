using NAudio.Wave;

namespace Pi.App.Voice
{
    public class AudioRecorder
    {
        public async Task<string> RecordAsync()
        {
            string outputPath = "recording.wav";

            WaveInEvent waveIn = new WaveInEvent();

            waveIn.WaveFormat = new WaveFormat(16000, 1);

            WaveFileWriter writer = new WaveFileWriter(
                outputPath,
                waveIn.WaveFormat
            );

            waveIn.DataAvailable += (object? sender, WaveInEventArgs e) =>
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            };

            Console.WriteLine("Gravando por 5 segundos...");

            waveIn.StartRecording();

            await Task.Delay(5000);

            waveIn.StopRecording();

            writer.Dispose();
            waveIn.Dispose();

            Console.WriteLine("Gravação finalizada.");

            return outputPath;
        }
    }
}