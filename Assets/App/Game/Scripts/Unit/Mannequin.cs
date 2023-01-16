using UnityEngine;
using System;
using System.Collections;
using UnityEngine.AI;

[Serializable]
public class MannequinContext
{
    public GameObject GFX;
    public Transform BodyMiddleMark;
    public NavMeshAgent Agent;

    public Vector3 DirectionUponArrival { get; set; }
}

public class Mannequin : Unit, IAttack, IMovableUnit
{
    [SerializeField] MannequinMove move;
    [SerializeField] MannequinContext context;

    public override UnitType UnitType => UnitType.Mannequin;

    public override CoverState CoverState => CoverState.Standing;

    public override float LineOfSight { get { return 50; } set { } }
    public override float AttackDistance { get { return 50; } set { } }

    public float GroupSpacing => 2;

    public void Initialize(UnitDeath _unitDeath, RegionManager _regionManager, HeatMap _heatMap)
    {
        base.Initialize(_unitDeath, _regionManager, _heatMap);

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
        //artilleryMode();
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

    public void Register_CancleMove(Action onCancle)
    {

    }
    public void Register_OnArrival(Action onArrival)
    {

    }

    public void GoTo(Vector3[] destinations, Vector3 direction, bool charge = false)
    {
        move.GoTo(destinations[destinations.Length - 1], direction);
    }

    public Vector3 LastMoveDirection => context.DirectionUponArrival;
    public Vector3 WorldPosition => transform.position;


    #endregion

    #region IAttack

    public void Attack(ITargetable target)
    {
        throw new NotImplementedException();
    }

    public void Attack(Vector3 position)
    {
        //explodeAt(position, 10);
        //ParticleSystem effect = Instantiate(context.ExplosionEffect);
        //effect.Play();
        //
        //effect.transform.position = position + Vector3.up;
        //
        //Destroy(effect, 5);
    }

    #endregion
}

[Serializable]
public class MannequinMove
{
    MannequinContext context;
    public void Initialize(MannequinContext context)
    {
        this.context = context;
    }

    public void Charge(Vector3 destination)
    {
        throw new Exception("Not supported");
    }

    public void GoTo(Vector3 destination, Vector3 direction, bool charge = false)
    {
        context.Agent.SetDestination(destination);
    }

    public void Stop()
    {
        context.Agent.isStopped = true;
    }
}