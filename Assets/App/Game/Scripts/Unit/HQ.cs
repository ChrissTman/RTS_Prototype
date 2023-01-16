using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public delegate void TurnOnHQUI();


[Serializable]
public class HQContext
{
    public GameObject GFX;
    public Transform BodyMiddleMark;
    public NavMeshAgent Agent;

    public Transform LandZone;

    public GameObject Packed;
    public GameObject Unpacked;

    public Vector3 DirectionUponArrival { get; set; }
}

public class HQ : Unit, IMovableUnit, IMountableUnit
{
    [SerializeField] HQMove move;
    [SerializeField] HQContext context;

    public Vector3 LZ => context.LandZone.position;

    public override UnitType UnitType => UnitType.HQ;

    public override CoverState CoverState => CoverState.Standing;

    public override float LineOfSight { get { return 50; } set { } }
    public override float AttackDistance { get { return 0; } set { } }

    public float GroupSpacing => 2;

    public bool IsUnpacked;

    TurnOnHQUI turnOnHQUI;

    public void Initialize(TurnOnHQUI turnOnHQUI, UnitDeath _unitDeath, RegionManager _regionManager, HeatMap _heatMap)
    {
        base.Initialize(_unitDeath, _regionManager, _heatMap);

        this.turnOnHQUI = turnOnHQUI;
        move.Initialize(context);
    }

    bool isVisible;
    public override bool IsVisible => isVisible;
    public override void SetVisibility(bool visible)
    {
        isVisible = visible;
        context.GFX.SetActive(visible);
    }

    public override void OnSelect()
    {
        if (IsUnpacked)
            turnOnHQUI();
    }

    public void Unpack()
    {
        IsUnpacked = true;
        context.Agent.enabled = false;
        context.Packed.SetActive(false);
        context.Unpacked.SetActive(true);
    }

    #region IPoolable

    protected override void Die()
    {
        base.Die();
    }

    public override void ResetState()
    {
        base.ResetState();
    }

    public override void Activate()
    {
        base.Activate();

        context.GFX.SetActive(true);
        context.Agent.enabled = true;
    }

    public override void Deactivate()
    {
        base.Deactivate();

        context.GFX.SetActive(false);
        context.Agent.enabled = false;
    }

    #endregion

    #region ITargetable

    public override Vector3 BodyMiddle => context.BodyMiddleMark.position;

    #endregion

    #region IMovable

    public Vector3 LastMoveDirection => context.DirectionUponArrival;
    public Vector3 WorldPosition => transform.position;

    public void Register_CancleMove(Action onCancle)
    {

    }
    public void Register_OnArrival(Action onArrival)
    {

    }

    public void GoTo(Vector3[] destinations, Vector3 direction, bool charge = false)
    {
        if (IsUnpacked)
            return;

        move.GoTo(destinations[destinations.Length - 1], direction);
    }

    #endregion

    #region IMountableUnit

    public bool IsMounted { get; set; }
    Transform originalParent;
    public void Mount(IMountableTarget target, bool snapToParent, bool hide)
    {
        context.Agent.enabled = false;


        if (hide)
            SetVisibility(false);
        if (snapToParent)
        {
            originalParent = transform.parent;
            transform.SetParent(target.UnitParent);
        }

        CoverState = CoverState.Prone;
        IsMounted = true;
    }

    public void Unmount(Vector3 pos, Vector3 dir)
    {
        Unmount();
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(dir);

        IsMounted = false;
    }

    public void Unmount()
    {   
        if (IsMounted)
        {
            SetVisibility(true);
            transform.SetParent(originalParent);

            CoverState = CoverState.Standing;
        }
        context.Agent.enabled = true;

        IsMounted = false;
    }
    
    #endregion

}

[Serializable]
public class HQMove
{
    HQContext context;
    public void Initialize(HQContext context)
    {
        this.context = context;
    }

    public void Charge(Vector3 destination)
    {
        throw new Exception("Not supported");
    }

    public void GoTo(Vector3 destination, Vector3 direction, bool charge = false)
    {
        var agent = context.Agent;
        if (!agent.isOnNavMesh || !agent.enabled)
        {
            agent.enabled = false;
            agent.enabled = true;
        }
        agent.SetDestination(destination);
    }

    public void Stop()
    {
        context.Agent.isStopped = true;
    }
}