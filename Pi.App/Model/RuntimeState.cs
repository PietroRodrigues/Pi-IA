// Estado do ciclo de vida do pai. Somente o Controller escreve; a View lê.
namespace Pi.App.Model;

public enum RuntimePhase
{
    Idle,
    Preparing,
    Starting,
    Running,
    Stopping
}

public sealed class RuntimeState
{
    public RuntimePhase Phase { get; private set; } = RuntimePhase.Idle;
    public int? TuiProcessId { get; private set; }

    public void SetPhase(RuntimePhase phase) => Phase = phase;

    public void SetTuiProcess(int? processId) => TuiProcessId = processId;
}
