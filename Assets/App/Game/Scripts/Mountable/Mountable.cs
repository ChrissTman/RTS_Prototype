using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mountable : MonoBehaviour, IMountableTarget, IInteractable, ISelectable
{
    [SerializeField] MountSpot[] spots;
    public int Size => spots.Length;
    public int UsedSlots { get; private set; }
    public bool IsFull => Size - UsedSlots <= 0;

    public Transform UnitParent => transform;

    public Vector3 MountablePosition => transform.position;

    public bool SnapsToParent => false;
    public bool Hides => false;

    public string Info { get; set; }
    
    public void Interact(Squad squad)
    {
        //Debug.Log($"{squad.ID} => {gameObject.name}");
    }
    
    public Vector3 RegisterUnit(Unit unit)
    {
        if (!(unit is IMountableUnit))
            throw new System.Exception("Unit is not IMountableUnit");

        for (int i = 0; i < spots.Length; i++)
        {
            if(!spots[i].Used)
            {
                spots[i].Used = true;
                spots[i].Unit = unit;
                var point = spots[i].Point;

                //var mountable = unit as IMountableUnit;
                //mountable.Mount(this, SnapsToParent, Hides);

                //print("Mounting unit: " + unit.GetType());


                UsedSlots++;
                return point.position;
            }
        }

        return Vector3.zero;
    }
    
    public void UnregisterUnit(Unit unit)
    {
        for (int i = 0; i < spots.Length; i++)
        {
            if (spots[i].Unit == unit)
            {
                spots[i].Unit = null;
                spots[i].Used = false;
                UsedSlots--;
                break;
            }
        }
    }

    public void UnmountUnit(Unit unit)
    {
        UnregisterUnit(unit);
        (unit as IMountableUnit).Unmount();
    }

    public void UnmountAll()
    {
        for (int i = 0; i < spots.Length; i++)
        {
            if (spots[i].Used)
            {
                spots[i].Used = false;
                var unit = spots[i].Unit;
                (unit as IMountableUnit).Unmount();
                UsedSlots--;

                spots[i].Unit = null;
            }
        }

    }

    public Vector3 WorldPosition => transform.position;
    public float Percentage => (Size / (float)UsedSlots) / 100f;
    public void OnSelect()
    {
        Info =$"{Size}/{UsedSlots}";
    }
}

[System.Serializable]
public class MountSpot
{
    public bool Used { get; set; }
    public Unit Unit { get; set; }
    public Transform Point;
}