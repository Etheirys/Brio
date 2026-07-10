namespace Brio.Core;

public interface IHistoryCompatible
{
    object CaptureInitialState();
    void Snapshot();
    void ApplyState(object state);
}
