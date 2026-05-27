using Whisper.net;

namespace Pi.App.Voice
{
    public class SpeechToTextService
    {
        private readonly string _modelPath = Path.Combine("VoiceModels", "ggml-tiny.bin");

        public void Teste()
        {
            using var factory =
                WhisperFactory.FromPath(
                    _modelPath
                );

            Console.WriteLine(
                "Whisper carregado."
            );
        }
    }
}