/// Anything that can be "wrong": room items now, ads later.
public interface IAnomalous
{
    bool IsActive { get; }
    AnomalyDefinition Current { get; }
    void Trigger(AnomalyDefinition def);
    void Resolve();
}
