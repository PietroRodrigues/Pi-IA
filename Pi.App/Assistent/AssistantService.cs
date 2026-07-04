using Pi.App.Agent;
using Pi.App.Voice;

namespace Pi.App.Assistant
{
    public class AssistantService
    {

        private readonly AgentService _agent;
        private readonly SpeechToTextService _speech;
        private readonly AudioRecorder _recorder;
        private readonly AssistantMessageBus _messageBus;

        public AssistantService()
        {
            _agent = new AgentService();
            _speech = new SpeechToTextService();
            _recorder = new AudioRecorder();

            _messageBus = new AssistantMessageBus();
        }

        public async Task ProcessVoiceAsync()
        {
            string audioPath = await _recorder.RecordAsync();

            Console.WriteLine($"Áudio gravado em: {audioPath}");

            string recognizedText = await _speech.TranscribeAsync(audioPath);

            Console.WriteLine();
            Console.WriteLine($"Você: {recognizedText}");
            Console.WriteLine();

            string response = await _agent.AskAsync(recognizedText);
            Console.WriteLine($"Pi: {response}");
        }

        private async Task RunTextLoopAsync()
        {
            while (true)
            {
                Console.Write("\nVocê: ");

                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (input.Equals("sair", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                }

                AssistantMessage message = new AssistantMessage
                {
                    Text = input,
                    Source = AssistantMessageSource.Text
                };

                _messageBus.Publish(message);

                await Task.Yield();
            }
        }

        private async Task ProcessMessagesLoopAsync()
        {
            while (true)
            {
                if (!_messageBus.TryRead(out AssistantMessage? message))
                {
                    await Task.Delay(50);
                    continue;
                }

                if(message != null)
                {
                    string response = await _agent.AskAsync(message.Text);
                    Console.WriteLine();
                    Console.WriteLine($"Pi: {response}");
                }
            }

        }

        public async Task RunAsync()
        {
            Console.WriteLine("Pi iniciado.");

            while(true){

                await ProcessVoiceAsync();

            }

        }
        
    }

}