namespace SerialSavant.Infrastructure;

/// <summary>
/// Readiness gate for the llama-server process.
/// Callers await <see cref="WaitForReadyAsync"/> before making HTTP calls.
/// </summary>
public interface ILlamaServerGate
{
    Task WaitForReadyAsync(CancellationToken cancellationToken);
}
