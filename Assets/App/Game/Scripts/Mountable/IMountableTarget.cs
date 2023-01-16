using UnityEngine;

public interface IMountableTarget
{
    //Remove? | general position of the mountable target
    Vector3 MountablePosition { get; }

    int Size { get; }
    int UsedSlots { get; }
    bool IsFull { get; }
    Transform UnitParent { get; }

    bool SnapsToParent { get; }
    bool Hides { get; }

    Vector3 RegisterUnit(Unit unit);
    void UnregisterUnit(Unit unit);

    void UnmountUnit(Unit unit);
    void UnmountAll();
}