using UnityEngine;
using System;
using System.Collections;
using UnityEngine.AI;

[Serializable]
public class MortarContext
{
    public Transform MainTransform;

    public GameObject GFX;
    public Transform BodyMiddleMark;
    public NavMeshAgent Agent;

    public GameObject ProjectilePrefab;
    public FlareProjectile FlareProjectile;

    public Transform Hinge;

    public Vector3 VelocityVector { get; set; }

    public Vector3 DirectionUponArrival { get; set; }

    public float ProjectileAirTime; //How long should a projectile be in air = doesn't correspond to 

}

//public delegate void EnableArtilleryMode();
public delegate void ExplodeAt(Vector3 position, float radius);

public class Mortar : Unit, IAttack, IMovableUnit, IMountableUnit
{
    [SerializeField] MortarMove move;
    [SerializeField] MortarContext context;

    [SerializeField] int explodeRadius;
    [SerializeField] float range;
    [SerializeField] float lineOfSight;
    [SerializeField] float attackSpeed;
    
    ExplodeAt explodeAt;

    [SerializeField] float projectileAirTime;

    public override UnitType UnitType => UnitType.Mortar;

    public override CoverState CoverState => CoverState.Standing;

    public float GroupSpacing => 5;

    public override float LineOfSight { get { return lineOfSight; } set { } }
    public override float AttackDistance { get { return range; } set { } }

    public bool IsInMortarMode;

    public void Initialize(ExplodeAt explodeAt, UnitDeath _unitDeath, RegionManager _regionManager, HeatMap _heatMap)
    {
        base.Initialize(_unitDeath, _regionManager, _heatMap);
        
        this.explodeAt = explodeAt;
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
        IsInMortarMode = false;

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

    public void UpdateTargetPosition(Vector3 groundPos)
    {
        move.RotateTowardsTarget(groundPos);
    }

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

    float attackTime;

    public void Attack(ITargetable target)
    {
        throw new NotImplementedException();
    }

    public void Attack(Vector3 position)
    {
        if (attackTime > Time.time)
            return;

        //if (Vector3.Distance(transform.position, position) > range)
        //    return;
        
        attackTime = Time.time + attackSpeed;

        var projectile = Instantiate(context.ProjectilePrefab, context.Hinge.position, Quaternion.LookRotation(context.Hinge.forward));

        var velVec = move.CalculateTrajectoryVelocity(context.Hinge.position, position, projectileAirTime);

        var rb = projectile.GetComponent<Rigidbody>();
        rb.angularDrag = 0;
        rb.drag = 0;
        rb.velocity = velVec;

        var time = projectileAirTime;

        Destroy(projectile, time);
        StartCoroutine(ExplodeAt(time, position, explodeRadius));
    }

    public void FireFlare()
    {
        UpdateTargetPosition(transform.position + transform.forward * 50);
        var flare = Instantiate(context.FlareProjectile, context.Hinge.position, Quaternion.LookRotation(context.Hinge.forward));
        flare.Kick(context.VelocityVector);

        var time = move.CurrentProjectileAirTime / 2f;

        flare.IgniteIn(time);
    }

    IEnumerator ExplodeAt(float delay, Vector3 position, float radius)
    {
        yield return new WaitForSeconds(delay);

        explodeAt(position, radius);
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
public class MortarMove
{
    [SerializeField] float timeCoeficient;

    MortarContext context;
    public void Initialize(MortarContext context)
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

    public float CurrentProjectileAirTime { get; set; }
    public void RotateTowardsTarget(Vector3 target)
    {
        var origin = context.Hinge.position;
        var t = context.ProjectileAirTime;

        var distance = Vector3.Distance(origin, target);
        var time = t * (distance / timeCoeficient);
        CurrentProjectileAirTime = time;

        var velVec = CalculateTrajectoryVelocity(origin, target, time);
        
        context.Hinge.rotation = Quaternion.LookRotation(velVec);
        context.VelocityVector = velVec;
    }

    public Vector3 CalculateTrajectoryVelocity(Vector3 origin, Vector3 target, float time)
    {
        float vx = (target.x - origin.x) / time;
        float vz = (target.z - origin.z) / time;
        float vy = ((target.y - origin.y) - 0.5f * Physics.gravity.y * time * time) / time;
        return new Vector3(vx, vy, vz);
    }


}