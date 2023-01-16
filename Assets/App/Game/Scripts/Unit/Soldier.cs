using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using Random = UnityEngine.Random;

public enum SoldierState
{
    none = 0,
    Chase = 1,
    GoTo = 2,
    Attack = 3,
    Idle = 4,
}

public class EmptyTarget : ITargetable
{
    Vector3 position;
    public Vector3 Position => position;

    public Vector3 BodyMiddle => position + Vector3.up;

    public int HP => 999;

    public int MaxHP { get; set; } = 999;

    public bool IsAlive => true;

    public CoverState CoverState { get; set; }

    public float LineOfSight { get; set; }

    public EmptyTarget(Vector3 pos)
    {
        position = pos;
    }

    public void DealDamage(int amount, ITargetable from)
    {
        
    }
}


public interface IStateControl
{
    SoldierState UnitState { get; }
    void StateUpdated();
}


public interface IFireableUnit
{
    void Fire(ITargetable target);
    void CoverFire(Vector3 area);
}

public interface IMountableUnit
{
    bool IsMounted { get; }

    void Mount(IMountableTarget target, bool snapToParent, bool hide);
    void Unmount(Vector3 pos, Vector3 dir);
    void Unmount();
}

public interface IMovableUnit
{
    float GroupSpacing { get; }

    void Register_CancleMove(Action onCancle);
    void Register_OnArrival(Action onArrival);
    
    void GoTo(Vector3[] destinations, Vector3 direction, bool charge = false);

    Vector3 LastMoveDirection { get; }
    Vector3 WorldPosition { get; }
}
public interface IChargableUnit
{
    void Charge(Vector3 destination);
}

//public events to inform managers about some state or demand
public delegate void AttackOrMove(Unit attacker, ITargetable target);
public delegate ITargetable FindTarget(Vector3 position, float inRange, Team team);
public delegate void UnitDeath(Unit unitToDie);



[Serializable]
public class SoldierContext
{
    [Header("Helper")]
    public Unit UnitInstance; // the f
    public Soldier Soldier; //Just because co-routines

    [Header("Graphics")]
    public Renderer MainRenderer;
    public LineRenderer LineRenderer;
    public Projector Projector;
    public Animator Animator;
    public GameObject GFX;

    [Header("Materials")]
    public Material TeamABodyMaterial;
    public Material TeamBBodyMaterial;
    public Material DeadBodyMaterial;
    public Material TeamAProjectorMaterial;
    public Material TeamBProjectorMaterial;

    [Header("AI")]
    public NavMeshAgent NavMeshAgent;

    [Header("Physics")]
    public Collider MainCollider;

    [Header("Markers")]
    public Transform MainTransform;
    public Transform FireSpot;
    public Transform BodyMiddle;


    [Header("Projectiles")]
    public Rocket Rocket;
    public GameObject MortarRound;
    public float MortarRoundAirtime;
    public float MortarRoundExplodeRadius;

    public float Elevation { get { return MainTransform.transform.position.y; } }

    public Vector3 DirectionUponArrival { get; set; }
    public Vector3 Position { get { return MainTransform.position; } set { MainTransform.position = value; } }

    SoldierState s;
    public SoldierState State { get { return s; } set { s = value; OnStateChanged?.Invoke(); } }

    SquadMode m;
    public SquadMode Mode { get { return m; } set { m = value; OnModeChanged?.Invoke(); } }

    CoverState c;
    public CoverState CoverState { get { return c; } set { c = value; OnCoverChaged?.Invoke(); } }

    public ITargetable CurrentTarget { get; set; }

    SoldierWeapons currentWeapon;
    public SoldierWeapons CurrentWeapon
    {
        set  { currentWeapon = value; OnWeaponChanged?.Invoke(value); }
        get => currentWeapon;
    }
    
    public Func<Team> GetTeam;

    public Action OnArriaval;
    public Action OnMoveCancled;
    public Action OnStateChanged;

    public Action OnModeChanged;

    public Action OnCoverChaged;

    public Action<SoldierWeapons> OnWeaponChanged;
}

/// <summary>
/// Soldier specific implementation
/// </summary>
public class Soldier : Unit, IStateControl, IChargableUnit, IFireableUnit, IMountableUnit, IMovableUnit
{
    //set by Initialize(...)
    ExplodeAt explodeAt;
    FindTarget findTarget;
    AttackOrMove attackOrMove;

    [SerializeField] SoldierContext context;

    [SerializeField] SoldierGraphics graphics;
    [SerializeField] SoldierAttack attack;
    [SerializeField] SoldierMove move;

    [SerializeField] float waitAfterDeath;

    public override UnitType UnitType => UnitType.Soldier;

    public SquadMode Mode { get { return context.Mode; } set { context.Mode = value; } }
    
    void Update()
    {
        if (!IsAlive)
            return;

        graphics.DetermineSpeed();

        if(Input.GetKeyDown(KeyCode.Keypad1))
        {
            context.CurrentWeapon = SoldierWeapons.M16;
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            context.CurrentWeapon = SoldierWeapons.LAW;
        }
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            context.CurrentWeapon = SoldierWeapons.M60;
        }
        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            context.CurrentWeapon = SoldierWeapons.M79;
        }
    }
    
    void LateUpdate()
    {
        if (!IsAlive)
            return;

        //if (InState(UnitState.MovingToDestination, UnitState.Chasing))
        if(NotInState(SoldierState.none))
            move.Update();
    }
    
    /// <summary>
    /// Initialize this unit upon spawning with needed references
    /// </summary>
    public void Initialize(ExplodeAt explodeAt, AttackOrMove attackOrMove, FindTarget findTarget, UnitDeath unitDeath, RegionManager regionManager, HeatMap heatMap)
    {
        Initialize(unitDeath, regionManager, heatMap);

        this.explodeAt = explodeAt;
        this.findTarget = findTarget;
        this.attackOrMove = attackOrMove;

        graphics.Initialize(context);

        attack.Initialize(context, findTarget, OutOfAmmo);
        attack.AttackingUnit = AttackingUnit;
        attack.TargetFound = TargetFound;

        move.Initialize(context);
        move.NewDestination = NewDestination;

        IsReconable = true;

        context.GetTeam = () => Team;
        context.OnStateChanged += graphics.OnStateChanged;
        //context.OnStateChanged += () => Info = Enum.GetName(typeof(SoldierState), state);

        context.OnCoverChaged += () => graphics.Graphics_OnCoverStateChanged(CoverState);
        context.OnCoverChaged += () =>
        {
            switch (context.CoverState)
            {
                case CoverState.Standing:
                    Vulnerability = 1.0f;
                    break;
                case CoverState.Crouch:
                    Vulnerability = 0.7f;
                    break;
                case CoverState.Prone:
                    Vulnerability = 0.25f;
                    break;
            }
        };

        //context.OnWeaponChanged += (x) => Info = Enum.GetName(typeof(SoldierWeapons), x);
        context.OnWeaponChanged += (x) => graphics.Graphics_OnWeaponChanged(x);
        context.OnWeaponChanged += (x) => attack.SetWeapon(x);

    }

    bool isVisible;
    public override bool IsVisible => isVisible;
    public override void SetVisibility(bool visible)
    {
        //Temp - show off
        if (isMounted && visible)
            graphics.SetVisibility(false);

        isVisible = visible;
        graphics.SetVisibility(visible);
    }

    public void SetWeapon(SoldierWeapons weapon)
    {
        context.CurrentWeapon = weapon;
    }

    public override void OnSelect()
    {
        //.
    }

    #region IMoutableUnit
    bool isMounted;

    public bool IsMounted => isMounted;
    bool snapToParent;
    bool hide;

    Transform originalParent;

    public void Mount(IMountableTarget target,bool snapToParent, bool hide)
    {
        this.snapToParent = snapToParent;
        this.hide = hide;

        isMounted = true;
        context.NavMeshAgent.enabled = false;


        if (hide)
            graphics.SetVisibility(false, true);
        if(snapToParent)
        {
            originalParent = transform.parent;
            transform.SetParent(target.UnitParent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        CoverState = CoverState.Prone;
    }
    public void Unmount(Vector3 pos, Vector3 dir)
    {
        Unmount();
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    public void Unmount()
    {
        if (IsMounted)
        {
            if (hide)
            {
                graphics.SetVisibility(true, true);
                graphics.ResetForceVisibilityState();
            }
            if (snapToParent)
                transform.SetParent(originalParent);
             
            CoverState = CoverState.Standing;
        }

        isMounted = false;

        var agent = context.NavMeshAgent;
        agent.enabled = true;
        //agent.ResetPath();
        //if (!agent.isOnNavMesh) //Retoggle agent to avoid odd states
        //{
        //    agent.enabled = false;
        //    agent.enabled = true;
        //
        //    agent.ResetPath();
        //}
    }

    #endregion

    #region IFireableUnit
    public void CoverFire(Vector3 targetPos)
    {
        attack.Attack(targetPos);
    }
    public void Fire(ITargetable target)
    {
        move.Stop();
        attack.Attack(target);
    }
    #endregion

    #region IMovableUnit

    public float GroupSpacing => move.GroupSpacing;

    public Vector3 LastMoveDirection => context.DirectionUponArrival;
    public Vector3 WorldPosition => transform.position;
    
    public void Register_CancleMove(Action onCancle)
    {
        context.OnMoveCancled = null;
        context.OnMoveCancled += onCancle;
    }

    public void Register_OnArrival(Action onArrival)
    {
        context.OnArriaval = null;
        context.OnArriaval += onArrival;
    }

    public void GoTo(Vector3[] destinations, Vector3 direction, bool charge = false)
    {
        if (isMounted)
            return;

        state = SoldierState.GoTo;
        move.GoTo(destinations, direction);
    }
    #endregion

    #region IChargableUnit
    public void Charge(Vector3 destination)
    {
        state = SoldierState.GoTo;
        move.Charge(destination);
    }
    #endregion

    #region ITargetable 

    public override CoverState CoverState { get { return context.CoverState; } set { context.CoverState = value; } }

    public override float LineOfSight { get { return attack.LineOfSight; } set { } }
    public override float AttackDistance { get { return attack.ElevatedAttackRange; } set { } }

    public override Vector3 BodyMiddle => context.BodyMiddle.position;

    protected override void Die()
    {
        base.Die();
        StartCoroutine(DelayUnitDeat(waitAfterDeath));
        graphics.Die();
        move.Stop();

        context.MainCollider.enabled = false;
        attack.Deactivate();

        Squad.Alive--;
        Squad.Platoon.Alive--;
    }
    IEnumerator DelayUnitDeat(float delay)
    {
        yield return new WaitForSeconds(delay);
        unitDeath.Invoke(this);
    }

    public override void DealDamage(int amount, ITargetable from)
    {
        float elevationBonus = 1;
        elevationBonus = ((from.BodyMiddle.y - BodyMiddle.y) > 0 ? 1.5f : 1);

        base.DealDamage((int)(amount * Vulnerability * elevationBonus), from);

        if(InState(SoldierState.Idle) && NotInState(SoldierState.Attack) && IsAlive)
        {
            CoverState = CoverState.Prone;
        }
    }

    #endregion

    #region IPoolable

    /// <summary>
    /// Resets all the states.
    /// </summary>
    public override void ResetState()
    {
        Mode = SquadMode.Normal;
        state = SoldierState.none;
        CoverState = CoverState.Standing;
        context.OnArriaval = null;
        context.DirectionUponArrival = Vector3.zero;
        context.CurrentTarget = null;

        base.ResetState();
    }

    /// <summary>
    /// Active a unit. Turns on graphics and resets all the states
    /// </summary>
    public override void Activate()
    {
        base.Activate();
        
        move.Activate();
        graphics.Activate();
        attack.Activate();
        
        context.MainCollider.enabled = true;
        
        gameObject.name = $"Unit: {UnitID.ToString("000")}";
    }

    public override void Deactivate()
    {
        base.Deactivate();

        context.MainCollider.enabled = false;

        move.Deactivate();
        graphics.Deactivate();
        attack.Deactivate();

        Squad = null;
        CurrentRegion = null;

        StopAllCoroutines();
        CancelInvoke();

        gameObject.name = $"Dead {UnitID.ToString("000")}";
    }



    #endregion

    #region Events

    void TargetFound(ITargetable target)
    {
        attackOrMove(this, target);

        //var context = FindObjectOfType<ManagerContext>();
        //context.AttackManager.CheckVisibility(BodyMiddle, target.BodyMiddle);
        //
        //float distance = Vector3.Distance(Position, target.Position);
        //
        //if (attack.IsAttackDistance(distance))
        //    attack.Attack(target);
        //else
        //{
        //    var dir = (target.Position - Position).normalized;
        //    var destination = target.Position - dir * attack.ElevatedAttackRange;
        //
        //    state = SoldierState.Chase;
        //    move.GoTo(destination, dir);
        //}
    }

    void OutOfAmmo()
    {
        var size = attack.CurrentStats.MagazineSize;
        var type = attack.CurrentStats.MagazineType;
        
        if (Squad.Platoon.Magazines[type] > 0)
        {
            attack.Ammo = size;
            Squad.Platoon.Magazines[type]--;
        }
        else
        {
            print($"Out of ammo [{type}]");
        }
    }

    void AttackingUnit(SoldierWeaponData stats, ITargetable target)
    {
        attack.Ammo--;

        if(stats.DamageType == DamageType.Immediate)
        {
            target.DealDamage(stats.Damage, this);
            graphics.Graphics_ImmediateAttack(target);
        }
        else if(stats.DamageType == DamageType.Conditional)
        {
            if (stats.MagazineType == AmmunitionType.rocket)
                RocketAttack(target.BodyMiddle);

            if (stats.MagazineType == AmmunitionType.grenade)
                GrenadeAttack(target.BodyMiddle);
        }

        heatMap.AddRecord(Position, target.Position, 1, Team);
        move.Move_AttackingUnit(target);
    }
    void RocketAttack(Vector3 targetPos)
    {
        var dir = targetPos - context.MainTransform.position;

        var r = GameObject.Instantiate(context.Rocket, context.FireSpot.position, context.FireSpot.rotation);
        r.TargetPosition = targetPos;
        r.transform.rotation = Quaternion.LookRotation(dir);
    }
    void GrenadeAttack(Vector3 targetPos)
    {
        Vector3 CalculateTrajectoryVelocity(Vector3 origin, Vector3 target, float time)
        {
            float vx = (target.x - origin.x) / time;
            float vz = (target.z - origin.z) / time;
            float vy = ((target.y - origin.y) - 0.5f * Physics.gravity.y * time * time) / time;
            return new Vector3(vx, vy, vz);
        }

        var dir = CalculateTrajectoryVelocity(context.FireSpot.position, targetPos, context.MortarRoundAirtime);

        var g = GameObject.Instantiate(context.MortarRound, context.FireSpot.position, context.FireSpot.rotation);
        g.transform.rotation = Quaternion.LookRotation(dir);
        var rb = g.GetComponent<Rigidbody>();
        rb.angularDrag = 0;
        rb.drag = 0;
        rb.velocity = dir;

        StartCoroutine(ExplodeAtDelayed(context.MortarRoundAirtime, targetPos, context.MortarRoundExplodeRadius));
    }
    IEnumerator ExplodeAtDelayed(float delay, Vector3 pos, float radius)
    {
        yield return new WaitForSeconds(delay);
        explodeAt(pos, radius);
    }

    void NewDestination(Vector3 destination)
    {
        graphics.Graphics_NewDestination(destination);
    }
    
    #endregion

    SoldierState state { get { return context.State; } set { context.State = value; StateUpdated(); } }
    public SoldierState UnitState { get { return state; } }


    public void StateUpdated()
    {
        //?
    }

    /// <summary>
    /// Helper functions to define if the unit is in one of the state.
    /// </summary>
    /// <param name="states">Possible states that the unit can be in</param>
    /// <returns></returns>
    bool InState(params SoldierState[] states)
    {
        foreach (var x in states)
        {
            if (x == state)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Helper functions to define if the unit is not in one of the state.
    /// </summary>
    /// <param name="states">Impossible states that the unit can be in</param>
    /// <returns></returns>
    bool NotInState(params SoldierState[] states)
    {
        return !InState(states);
    }
}