using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TransportHelicopter : MonoBehaviour, IMountableTarget
{
    [SerializeField] HelicopterContext context;

    [SerializeField] HelicopterMove move;
    [SerializeField] Transform marker;

    [SerializeField] float unmountSideGoDistance;
    [SerializeField] float unmountMainGoDistance;
    [SerializeField] float unmountDelay;

    public void Initialize()
    {
        move.Initialize(context);
        //move.MoveTo(marker.position, Vector3.forward, true);
        //context.OnArrival = () =>
        //{
        //    UnmountAll();
        //    //move.MoveTo(Vector3.zero, Vector3.forward, false);
        //};
    }

    public Action OnArrival { get => context.OnArrival; set => context.OnArrival = value; }

    public void Move(Vector3 pos, Vector3 dir, bool land)
    {
        move.MoveTo(pos, dir, land);
    }

    private void Update()
    {
        move.UpdateMove();
    }


    public void Land()
    {
        if (!move.IsLanding)
        {
            move.MoveTo(context.GroundPosition, context.Gfx.transform.forward, true);
        }
    }
    public void LiftOff()
    {
        if (move.IsLanding)
        {
            move.MoveTo(context.GroundPosition, transform.forward, false);
        }
    }


    #region IMountableTarget
    public int Size => 100;
    public int UsedSlots => registeredUnits.Count;
    public bool IsFull { get { return UsedSlots >= Size; } }

    //TODO: Unit slots
    public Transform UnitParent => transform;

    public Vector3 MountablePosition => context.GroundPosition;

    List<Unit> registeredUnits = new List<Unit>();

    public Action OnUnmountAll;

    public bool SnapsToParent => true;
    public bool Hides => true;

    //public void MountUnit(Unit unit) {
    //    var mountable = (unit as IMountableUnit);
    //    if (mountable != null && !mountable.IsMounted) {
    //        mountable.Mount(this, true, true);
    //        garisonedUnits.Add(unit);
    //    }
    //}

    public void UnmountUnit(Unit unit)
    {
        var mountable = (unit as IMountableUnit);
        if (mountable != null && mountable.IsMounted)
        {
            mountable.Unmount(context.GroundPosition, Vector3.forward);
            UnregisterUnit(unit);
        }
    }

    public void UnmountAll()
    {
        if (registeredUnits.Count > 0)
            StartCoroutine(UnmoutAllSequencial());
    }

    bool unmoutSideToggle;
    IEnumerator UnmoutAllSequencial()
    {
        var mangarContext = ManagerContext.Instance;

        var trans = context.Gfx.transform;
        var mainDir = NoY(transform.forward) * unmountMainGoDistance;
        var mainDest = context.GroundPosition + mainDir;


        var spacing = 0.25f;

        var unitsToIterate = registeredUnits.ToArray();

        var numberOfUnits = registeredUnits.Count;
        var points = mangarContext.MovementManager.GetPoints(numberOfUnits, spacing, mainDest, mainDir, FormationType.Box);

        for (int i = 0; i < numberOfUnits; i++)
        {
            var unit = unitsToIterate[i];
            var point = points[i];

            var sideDir = trans.right * unmountSideGoDistance * (unmoutSideToggle ? 1 : -1);
            var sideDest = context.GroundPosition + sideDir;
            unmoutSideToggle = !unmoutSideToggle;

            UnmountUnit(unit);
            if (unit is IMovableUnit)
            {
                var movableUnit = unit as IMovableUnit;
                movableUnit.GoTo(new Vector3[] { sideDest, point }, mainDir);
            }
            yield return new WaitForSeconds(unmountDelay);
        }

        OnUnmountAll?.Invoke();
        OnUnmountAll = null;
        //LiftOff();
    }


    public Vector3 RegisterUnit(Unit unit)
    {
        if (!(unit is IMountableUnit))
            throw new System.Exception("Unit is not IMountableUnit");

        if (registeredUnits.Contains(unit))
            throw new Exception("Unit is already registered. Temp err");

        //Land();
        registeredUnits.Add(unit);

        if (!IsInvoking("EvaluateWait"))
            InvokeRepeating("EvaluateWait", 0, 1);

        return context.GroundPosition;
    }

    public void UnregisterUnit(Unit unit)
    {
        registeredUnits.Remove(unit);
        if (registeredUnits.Count == 0)
            CancelInvoke("EvaluateWait");
    }

    void EvaluateWait()
    {
        bool areAllGerisoned = true;

        foreach (var x in registeredUnits)
        {
            if (x is IMountableUnit)
            {
                if (!(x as IMountableUnit).IsMounted)
                {
                    areAllGerisoned = false;
                    break;
                }
            }
        }

        if (areAllGerisoned && move.IsLanding)
        {
            //LiftOff();
        }
        else if (areAllGerisoned && !move.IsLanding)
        {
            CancelInvoke("EvaluateWait");
        }
    }

    Vector3 NoY(Vector3 v3)
    {
        return new Vector3(v3.x, 0, v3.z);
    }

    #endregion
}
