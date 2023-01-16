using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

using Random = UnityEngine.Random;


public enum DamageType
{
    none,
    Immediate,
    Conditional,
}

public enum AmmunitionType
{
    none,
    bullet,
    rocket,
    grenade,
}


public enum SoldierWeapons
{
    none,
    M16,
    LAW,
    M79,
    M60
}

public interface IAttack
{
    void Attack(ITargetable target);
    void Attack(Vector3 position);
}

public delegate void SoldierOutOfAmmo();

public delegate void TargetFound(ITargetable target);
public delegate void AttackingUnit(SoldierWeaponData stats, ITargetable target);

[Serializable]
public class SoldierAttack : IAttack
{
    [Tooltip("Base attack distance (modified by every weapon)")]
    public float AttackBaseRange;

    [Tooltip("Base detect distance (modified by every weapon)")]
    public float DetectBaseRange;

    //[Tooltip("Damage that the unit deals")]
    //public int Damage;

    //[Tooltip("Delay after each shot in whole seconds i.e. 5.25s or 0.75s")]
    //public float FireRate;

    public bool Initialized { get { return initialized; } }
    public TargetFound TargetFound;
    public AttackingUnit AttackingUnit;

    [SerializeField] List<SoldierWeaponDataStatsPair> weaponStatsPairs;
    Dictionary<SoldierWeapons, SoldierWeaponData> weaponStats = new Dictionary<SoldierWeapons, SoldierWeaponData>();

    [NonSerialized]
    public SoldierWeaponData CurrentStats;
    SoldierWeapons currentWeapon = SoldierWeapons.M16;

    public int Ammo { get => CurrentStats.Ammo; set => CurrentStats.Ammo = value; }

    SoldierOutOfAmmo outOfAmmo;
    FindTarget findTarget;
    SoldierContext context;

    SoldierState state { get { return context.State; } set { context.State = value; } }
    Vector3 position { get { return context.MainTransform.position; } }

    float lastFiredShot;
    bool firingLoop;

    public float LineOfSight
    {
        get
        {
            var elevated = DetectBaseRange * CurrentStats?.LineOfSight + context.Elevation * 0.25f;
            var devider = 1;//CoverUtility.CalculateEfficiency(context.CoverState);
            return elevated.HasValue ? (elevated.Value / devider) : 1;
        }
    }
    public float ElevatedAttackRange
    {
        get
        {
            var elevated = AttackBaseRange * CurrentStats?.AttackDistance + context.Elevation * 0.25f;
            var devider = 1;//CoverUtility.CalculateEfficiency(context.CoverState);
            return elevated.HasValue ? (elevated.Value / devider) : 1;
        }
    }

    bool initialized;
    public void Initialize(SoldierContext context, FindTarget findTarget, SoldierOutOfAmmo outOfAmmo)
    {
        this.outOfAmmo = outOfAmmo;
        this.context = context;
        this.findTarget = findTarget;

        weaponStats = weaponStatsPairs.ToDictionary(x => x.Type, y => y.Stats);

        initialized = true;
    }

    bool enabled;
    public void Activate()
    {
        enabled = true;

        context.Soldier.StartCoroutine(AutodetectState());
        
        //if (token != null)
        //    token.Enabled = false;
        //
        //token = new AsyncToken() { Enabled = true };

        //AutoDetectState(token);
    }
    public void Deactivate()
    {
        //if (token != null)
        //    token.Enabled = false;

        if(attackLoop != null)
            context.Soldier.StopCoroutine(attackLoop);

        enabled = false;
    }

    public bool IsAttackDistance(float distance) => distance <= ElevatedAttackRange;
    public bool IsDetectDistance(float distance) => distance <= LineOfSight;

    /// <summary>
    /// Is the target alive and in range to attack.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    bool IsTargetViableForAttack(ITargetable target)
    {
        if (target == null || !target.IsAlive)
            return false;
        
        if(target is IMountableUnit)
        {
            var m = target as IMountableUnit;
            if (m.IsMounted)
                return false;
        }
        
        var dist = Vector3.Distance(position, target.Position);
        var canAttack = dist - 3 <= ElevatedAttackRange;

        return canAttack;
    }


    Coroutine attackLoop;
    /// <summary>
    /// Starts attacking or chasing a unit. It still checks if the unit in reach of all the distance thresholds. 
    /// </summary>
    /// <param name="target">Target which can be damaged.</param>
    public void Attack(ITargetable target)
    {
        context.CoverState = CoverState.Prone;
        attackLoop = context.Soldier.StartCoroutine(AttackLoop(target));
    }

    public void Attack(Vector3 position)
    {
        attackLoop = context.Soldier.StartCoroutine(AttackLoop(new EmptyTarget(position)));
    }

    bool isAttacking;
    IEnumerator AttackLoop(ITargetable target)
    {
        context.CurrentTarget = target;

        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        while (IsTargetViableForAttack(context.CurrentTarget) && enabled && Ammo > 0)
        {
            state = SoldierState.Attack;
            var currTarget = context.CurrentTarget;
            
            lastFiredShot = Time.time; //debug
            
            //Notify other systems to attack
            AttackingUnit?.Invoke(CurrentStats, target);

            firingLoop = true;

            var portion = CurrentStats.FireRate * 0.5f;
            yield return new WaitForSeconds(CurrentStats.FireRate + Random.Range(-portion, portion));
        }

        if (enabled && Ammo <= 0)
            outOfAmmo?.Invoke();

        firingLoop = false;

        context.CurrentTarget = null;
        if (state == SoldierState.Attack)
            state = SoldierState.Idle;
    }

    void GetTarget()
    {
        ITargetable target = findTarget(position, LineOfSight, context.GetTeam());
        
        if(target == null || context.CurrentTarget == target)
            return;
        else if (!target.IsAlive || InState(SoldierState.Attack))
        {
            Debug.LogWarning("Target is not alive or the Unit is already attacking");
            return;
        }
        TargetFound?.Invoke(target);
    }

    IEnumerator AutodetectState()
    {
        var wait = new WaitForSeconds(0.75f);
        while(enabled)
        {
            if (NotInState(SoldierState.Attack))
                GetTarget();

            yield return wait;
        }
    }
    
    public void SetWeapon(SoldierWeapons weapon)
    {
        currentWeapon = weapon;
        CurrentStats = weaponStats[weapon];
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

[Serializable]
public class SoldierWeaponData
{
    public float LineOfSight;
    public float AttackDistance;
    public float FireRate;
    public int Damage;
    public AmmunitionType MagazineType;
    [HideInInspector] public int Ammo;
    public int MagazineSize;
    public DamageType DamageType;

}

[Serializable]
public class SoldierWeaponDataStatsPair
{
    public string Name; //because of inspector
    public SoldierWeapons Type;
    public SoldierWeaponData Stats;
}