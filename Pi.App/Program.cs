using Pi.App.Agent;
using Pi.App.Voice;

var agent = new AgentService();

Console.WriteLine("Pi iniciado.");
Console.WriteLine("Digite algo:");
Console.WriteLine("Digite 'sair' para encerrar.");

var speech = new SpeechToTextService();

speech.Teste();

AudioRecorder recorder = new AudioRecorder();

string audioPath = await recorder.RecordAsync();

Console.WriteLine($"Áudio gravado em: {audioPath}");

while (true)
{
    Console.Write("\nVocê: ");

    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.ToLower() == "sair")
        break;

    Console.WriteLine("Pi está pensando...");

    var response = await agent.AskAsync(input);

    Console.WriteLine($"\nPi: {response}");
}     
    