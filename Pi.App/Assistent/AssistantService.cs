using Pi.App.Agent;
using Pi.App.Voice;

namespace Pi.App.Assistant
{
    public class AssistantService
    {
        private Task? _textTask;
        private Task? _processTask;
        private Task? _voiceTask;

        private readonly AgentService _agent;
        private readonly AssistantMessageBus _messageBus;
        private readonly VoiceInputService _voiceInput;
        private readonly SpeechToTextService _speech;

        public AssistantService()
        {
            _agent = new AgentService();
            _messageBus = new AssistantMessageBus();
            _voiceInput = new VoiceInputService();
            _speech = new SpeechToTextService();
        }

        private void StartWorkers()
        {
            _textTask = Task.Run(RunTextLoopAsync);
            _voiceTask = Task.Run(RunVoiceLoopAsync);
            _processTask = Task.Run(ProcessMessagesLoopAsync);
        }

        public async Task RunAsync()
        {
            Console.WriteLine("\nPi iniciado.");
            
            StartWorkers();

            await Task.WhenAll(
                _textTask!,
                _voiceTask!,
                _processTask!
            );
        }

        private async Task ProcessMessagesLoopAsync()
        {
            while (true)
            {
                AssistantMessage message = await _messageBus.ReadAsync();
                await ProcessMessageAsync(message);
            }

        }

        private async Task RunTextLoopAsync()
        {
            while (true)
            {
                try{
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

                    await _messageBus.PublishAsync(message);

                } catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar entrada de texto: {ex.Message}");
                }
            }
        }

        private async Task RunVoiceLoopAsync()
        {
            while (true)
            {
                try
                {
                    string audioPath = await _voiceInput.CaptureAsync();

                    string recognizedText = await _speech.TranscribeAsync(audioPath);
                    
                    recognizedText = recognizedText.Trim();

                    if (string.IsNullOrWhiteSpace(recognizedText))
                        continue;
                
                    if (recognizedText.StartsWith("[") && recognizedText.EndsWith("]"))
                        continue;

                    AssistantMessage message = new AssistantMessage
                    {
                        Text = recognizedText,
                        Source = AssistantMessageSource.Voice
                    };

                    await _messageBus.PublishAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar entrada de voz: {ex.Message}");
                }
            }
        }

        private async Task ProcessMessageAsync(AssistantMessage message)
        {
            try
            {
                string response = await _agent.AskAsync(message.Text);
            
                Console.WriteLine($"\nPi: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar mensagem: {ex.Message}");
            }
        }
        
    }

}