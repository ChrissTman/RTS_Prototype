using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public enum UnitType
{
    none = 0,
    Soldier = 1,
    Mortar = 2,
    Helicopter = 3,
    Altillery = 4,
    Mannequin = 5,
    HQ = 6,
}

public class UnitPool : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [Header("Factories")]
    [SerializeField] SoldierFactory soldierFactory;
    [SerializeField] ArtilleryFactory artilleryFactory;
    [SerializeField] MortarFactory mortarFactory;
    [SerializeField] HelicopterFactory helicopterFactory;
    [SerializeField] MannequinFactory mannequinFactory;
    [SerializeField] HQFactory hqFactory;

    List<IUnitFactory> factories = new List<IUnitFactory>();
    public Dictionary<UnitType, FixedPool<Unit>> Units { get; private set; } = new Dictionary<UnitType, FixedPool<Unit>>();
    
    Transform unitPoolRoot;

    void Start()
    {
        factories.Add(soldierFactory);
        factories.Add(artilleryFactory);
        factories.Add(mortarFactory);
        factories.Add(helicopterFactory);
        factories.Add(mannequinFactory);
        factories.Add(hqFactory);

        InitializeFactories();
        CreatePool();
    }

    void InitializeFactories()
    {
        foreach(var f in factories)
        {
            f.Initialize(context);
        }
    }

    void CreatePool()
    {
        unitPoolRoot = new GameObject(" ~*~ Unit pool ~*~").transform;
        
        foreach (IUnitFactory factory in factories)
        {
            var pool = new FixedPool<Unit>(factory.Amount);
            for (int i = 0; i < factory.Amount; i++)
            {
                var unit = factory.Create();

                unit.transform.SetParent(unitPoolRoot);
                unit.Deactivate();

                pool.SetAtIndex(i, unit);
            }
            Units.Add(factory.Type, pool);
        }
    }

    public Unit GetUnit(UnitType type)
    {
        FixedPool<Unit> pool = null;

        if(!Units.TryGetValue(type, out pool))
            throw new Exception($"{type} is not in the pool");
            
        if (pool.GetEmptyAmount()<= 0)
            throw new Exception($"{type}'s pool is all used");

        return pool.GetAvailable();
    }

    public void ReturnUnit(Unit unit)
    {
        FixedPool<Unit> pool = null;
        if (!Units.TryGetValue(unit.UnitType, out pool))
            throw new Exception($"Can't return unit of type {unit.UnitType} and name {unit.gameObject.name}");

        pool.ReturnElement(unit);
    }
}

public interface IUnitFactory
{
    void Initialize(ManagerContext context);
    Unit Create();
    int Amount { get; }
    UnitType Type { get; }
}

[Serializable]
public class SoldierFactory : IUnitFactory
{
    [SerializeField] Soldier soldier;
    [SerializeField] int amount;
    
    public int Amount => amount;
    public UnitType Type => UnitType.Soldier;

    ManagerContext context;
    public void Initialize(ManagerContext context)
    {
        this.context = context;
    }

    public Unit Create()
    {
        var s = GameObject.Instantiate(soldier);

        var gameManager = context.GameManager;
        var attackManager = context.AttackManager;
        var unitManager = context.UnitManager;
        var regionManager = context.RegionManager;
        var heatMap = context.HeatMap;

        ExplodeAt explodeAt = new ExplodeAt(attackManager.Explode);
        FindTarget findTarget = new FindTarget(attackManager.FindTarget);
        AttackOrMove attackOrMove = new AttackOrMove(attackManager.AttackOrMove);
        UnitDeath unitDeath = new UnitDeath(unitManager.KillUnit);
        
        s.Initialize(explodeAt, attackOrMove, findTarget, unitDeath, regionManager, heatMap);

        //s.OnSelect += context.UIManager.ApplyProfile(profile);

        return s;
    }
}

[Serializable]
public class ArtilleryFactory : IUnitFactory
{
    [SerializeField] Artillery artillery;
    [SerializeField] int amount;

    public int Amount => amount;
    public UnitType Type => UnitType.Altillery;
    
    public Unit Create()
    {
        var a = GameObject.Instantiate(artillery);

        //var gameManager = context.GameManager;
        var unitManager = context.UnitManager;
        var attackManager = context.AttackManager;
        var regionManager = context.RegionManager;
        var heatMap = context.HeatMap;

        ExplodeAt explodeAt = new ExplodeAt(attackManager.Explode);
        UnitDeath unitDeath = new UnitDeath(unitManager.KillUnit);

        a.Initialize(explodeAt, unitDeath, regionManager, heatMap);

        return a;
    }

    ManagerContext context;
    public void Initialize(ManagerContext context)
    {
        this.context = context;
    }
}

[Serializable]
public class MortarFactory : IUnitFactory
{
    [SerializeField] Mortar artillery;
    [SerializeField] int amount;

    public int Amount => amount;
    public UnitType Type => UnitType.Mortar;

    public Unit Create()
    {
        var a = GameObject.Instantiate(artillery);

        //var gameManager = context.GameManager;
        var unitManager = context.UnitManager;
        //var attackManager = context.AttackManager;
        var regionManager = context.RegionManager;
        var heatMap = context.HeatMap;

        ExplodeAt explodeAt = context.AttackManager.Explode;

        UnitDeath unitDeath = new UnitDeath(unitManager.KillUnit);

        a.Initialize(explodeAt, unitDeath, regionManager, heatMap);

        return a;
    }

    ManagerContext context;
    public void Initialize(ManagerContext context)
    {
        this.context = context;
    }
}

[Serializable]
public class HelicopterFactory : IUnitFactory
{
    [SerializeField] Helicopter helicopter;
    [SerializeField] int amount;

    public int Amount => amount;

    public UnitType Type => UnitType.Helicopter;

    public Unit Create()
    {
        var h = GameObject.Instantiate(helicopter);
        
        UnitDeath unitDeath = new UnitDeath(context.UnitManager.KillUnit);

        h.Initialize(unitDeath, context.RegionManager, context.HeatMap);

        return h;
    }

    ManagerContext context;
    public void Initialize(ManagerContext context)
    {
        this.context = context;
    }
}

[Serializable]
public class MannequinFactory : IUnitFactory
{
    [SerializeField] Mannequin mannequin;
    [SerializeField] int amount;

    public int Amount => amount;

    public UnitType Type => UnitType.Mannequin;

    public Unit Create()
    {
        var man = GameObject.Instantiate(mannequin);

        UnitDeath unitDeath = new UnitDeath(context.UnitManager.KillUnit);

        man.Initialize(unitDeath, context.RegionManager, context.HeatMap);

        return man;
    }

    ManagerContext context;
    public void Initialize(ManagerContext context)
    {
        this.context = context;
    }
}


[Serializable]
public class HQFactory : IUnitFactory
{
    [SerializeField] HQ hq;
    [SerializeField] int amount;

    public int Amount => amount;

    public UnitType Type => UnitType.HQ;

    public Unit Create()
    {
        var h = GameObject.Instantiate(hq);

        TurnOnHQUI turnOnHQUI = new TurnOnHQUI(() => context.UIManager.SetSupplyUI(true));
        UnitDeath unitDeath = new UnitDeath(context.UnitManager.KillUnit);

        h.Initialize(turnOnHQUI, unitDeath, context.RegionManager, context.HeatMap);

        return h;
    }

    ManagerContext context;
    public void Initialize(ManagerContext context)
    {
        this.context = context;
    }
}