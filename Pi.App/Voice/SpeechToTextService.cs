using System.Text;
using Whisper.net;
using System.Diagnostics;

namespace Pi.App.Voice
{
    public class SpeechToTextService
    {
        private readonly string _modelPath = Path.Combine("VoiceModels", "ggml-base.bin");


        public async Task<string> TranscribeAsync(string audioPath){

            Stopwatch stopwatch = Stopwatch.StartNew();

            using WhisperFactory factory = WhisperFactory.FromPath(_modelPath);

            Console.WriteLine($"Factory: {stopwatch.ElapsedMilliseconds} ms");

            using FileStream audioStream = File.OpenRead(audioPath);

            using var processor = factory
                .CreateBuilder()
                .WithLanguage("pt")
                .WithThreads(Environment.ProcessorCount)
                .Build();

            Console.WriteLine($"Processor: {stopwatch.ElapsedMilliseconds} ms");

            StringBuilder result = new StringBuilder();

            Stopwatch processWatch = Stopwatch.StartNew();

            await foreach (var segment in processor.ProcessAsync(audioStream))
            {
                Console.WriteLine($"Segmento: {processWatch.ElapsedMilliseconds} ms");
                result.Append(segment.Text);
            }

            Console.WriteLine($"Total: {stopwatch.ElapsedMilliseconds} ms");

            return result.ToString();

        }
    }
}