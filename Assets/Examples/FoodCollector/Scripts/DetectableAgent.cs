using MBaske.Sensors.Grid;

public class DetectableAgent : DetectableGameObject
{
    public bool IsFrozen;

    float GetIsFrozen() => IsFrozen ? 1 : 0;

    public override void AddObservables()
    {
        Observables.Add("Frozen", GetIsFrozen);
    }
}
