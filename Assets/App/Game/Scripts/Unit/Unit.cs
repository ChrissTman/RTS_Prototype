using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public interface ISelectable
{
    Vector3 WorldPosition { get; }
    string Info { get; }
    float Percentage { get; }

    void OnSelect();
}

public interface IPoolable
{
    void Activate();
    void Deactivate();
}

public interface ITargetable
{
    CoverState CoverState { get; set; }

    Vector3 Position { get; }
    Vector3 BodyMiddle { get; }
    //Vector3 GroundPoint { get; }

    //bool IsImitation { get; }
    
    int HP { get; }
    int MaxHP { get; set; }
    float LineOfSight { get; set; }
    void DealDamage(int amount, ITargetable from);
    bool IsAlive { get; }
}

public static class CoverUtility
{
    public static float CalculateEfficiency(CoverState state)
    {
        float devider = 1;

        if (state == CoverState.Standing)
            devider = 1;
        if(state == CoverState.Crouch)
            devider = 1.15f;
        if (state == CoverState.Prone)
            devider = 1.8f;

        return devider;
    }

    public static float CalculateHight(CoverState state)
    {
        float offset = 0;

        if (state == CoverState.Prone)
            offset = 0.15f;
        if (state == CoverState.Crouch)
            offset = 0.8f;
        if (state == CoverState.Standing)
            offset = 1.3f;

        return offset;
    }
}
public enum CoverState
{
    none = 0,
    Prone = 1,
    Crouch = 2,
    Standing = 3
}

public enum SquadMode
{
    none = 0,
    Normal = 1,
    Recon = 2,
}

/// <summary>
/// Unit logic. Composites of all the interfaces that defines a full unit.
/// </summary>
public abstract class Unit : MonoBehaviour, IPoolable, ITargetable, ISelectable
{
    //event
    protected UnitDeath unitDeath;

    protected RegionManager regionManager;
    protected HeatMap heatMap;
    public MapRegion CurrentRegion;

    //Squad the unit is in
    public Squad Squad { get; set; }


    public SquadMode SquadMode { get; set; }
    public bool IsReconable { get; set; }
    
    public virtual UnitType UnitType { get; }

    //Stats. 0 - 1
    public float Efficiency { get; protected set; }
    public float Visiblity { get; protected set; }
    public float Accuracy { get; protected set; }
    public float Mobility { get; protected set; }
    public float Vulnerability { get; protected set; }
    
    //Calculates incrementally unique ID
    static int lastUnitID;
    public int UnitID { get; private set; }

    public string Info { get; set; }

    public Team Team { get; set; }

    protected void Initialize(UnitDeath unitDeath, RegionManager regionManager, HeatMap heatMap)
    {
        this.unitDeath = unitDeath;
        this.regionManager = regionManager;
        this.heatMap = heatMap;
    }
    public abstract void SetVisibility(bool visible);
    public abstract bool IsVisible { get; }


    #region ISelectable

    public float Percentage => (float)HP / MaxHP;
    public Vector3 WorldPosition => transform.position;


    public abstract void OnSelect();

    #endregion


    /*
    #region IMove
    public abstract Action OnArriaval { get; set; }

    public virtual void GoTo(Vector3 destination, Vector3 direction, bool charge = false)
    {
        Debug.LogError("GoTo not implemented!");
    }
    public virtual void Stop()
    {
        Debug.LogError("Stop not implemented!");
    }
    #endregion
    */

    #region IPoolable

    /// <summary>
    /// Resets all the states.
    /// </summary>
    public virtual void ResetState()
    {

    }

    /// <summary>
    /// Active a unit. Turns on graphics and resets all the states
    /// </summary>
    public virtual void Activate()
    {
        ResetState();
        ResetHP();

        //Calculates incrementally unique ID
        UnitID = lastUnitID++;
        //Info = $"{UnitID.ToString("000")}";

        ////4 FPS
        InvokeRepeating("UpdateRegion", UnityEngine.Random.Range(0.1f, 0.5f), 1f / 4f);
    }

    /// <summary>
    /// Deactivates the unit. Turns off graphics and stop all asynchronous calls.
    /// </summary>
    public virtual void Deactivate()
    {
        ResetState();

        //just to make sure
        CancelInvoke();
        StopAllCoroutines();
    }

    #endregion

    #region ITargetable

    /// <summary>
    /// World position of the unit.
    /// </summary>
    public Vector3 Position { get { return transform.position; } set { transform.position = value; } }
    public virtual Vector3 BodyMiddle { get { return transform.position + Vector3.up; } }

    //public Vector3 GetBodyMiddle { get { return context.BodyMiddle.position; } }
    
    int hp;
    public int HP { get { return hp; } }
    public int MaxHP { get; set; } = 100;

    bool isAlive;
    public bool IsAlive { get { return isAlive; } }

    public virtual CoverState CoverState { get; set; }

    public virtual float LineOfSight { get; set; }
    public virtual float AttackDistance { get; set; }
    

    /// <summary>
    /// Deals damage to the local unit.
    /// </summary>
    /// <param name="amount">amount of damage to apply</param>
    /// <param name="from">from what unit the attack comes from</param>
    public virtual void DealDamage(int amount, ITargetable from)
    {
        hp -= amount;
        hp = Mathf.Clamp(HP, 0, MaxHP);

        if (HP == 0 && IsAlive)
        {
            Die();
        }
    }

    void ResetHP()
    {
        isAlive = true;
        hp = MaxHP;
    }

    protected virtual void Die()
    {
        if (CurrentRegion != null)
            CurrentRegion.Units.Remove(this);

        //redundant
        isAlive = false;
        hp = 0;
    }

    #endregion


    void UpdateRegion()
    {
        CurrentRegion = regionManager.UpdateUnit(this);
    }


}
