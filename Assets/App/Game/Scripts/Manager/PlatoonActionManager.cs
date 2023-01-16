using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlatoonActionManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;
    [SerializeField] RiflePlatoonUIActions riflePlatoon;

    [SerializeField] HQPlatoonUIActions hqPlatoon;

    //[SerializeField] SoldierUIActions soldierActions;
    //[SerializeField] HelicopterUIActions helicopterActions;
    //[SerializeField] MortarUIActions mortarUIActions;
    //[SerializeField] ArtilleryUIActions artilleryUIActions;

    //public Squad CurrentSquad;

    Dictionary<PlatoonType, IUIUnitActions> implementations = new Dictionary<PlatoonType, IUIUnitActions>();

    private void Awake()
    {
        //soldierActions.CreateActions();
        //implementations.Add(PlatoonType.Soldier, soldierActions);
        //
        //helicopterActions.CreateActions();
        //implementations.Add(PlatoonType.Helicopter, helicopterActions);
        //
        //mortarUIActions.CreateActions(context);
        //implementations.Add(PlatoonType.Mortar, mortarUIActions);
        //
        //artilleryUIActions.CreateActions(context);
        //implementations.Add(PlatoonType.Altillery, artilleryUIActions);

        riflePlatoon.CreateActions(context);
        implementations.Add(PlatoonType.Rifle, riflePlatoon);

        hqPlatoon.CreateActions(context);
        implementations.Add(PlatoonType.HQ, hqPlatoon);
    }

    public List<PlatoonActionInfo> GetIcons(Platoon platoon)
    {
        if (implementations.ContainsKey(platoon.Type))
            return implementations[platoon.Type].GetIcons(platoon);
        //else
        //{
        //    throw new Exception($"Type: {type} doesn't have any icons associated");
        //}
        return new List<PlatoonActionInfo>();
    }

    public void Execute(int index)
    {
        var platoon = context.PlayerActions.SelectedPlatoon;
        var type = platoon.Type;

        var actions = implementations[type];
        actions.Execute(index, platoon);
    }
}

[Serializable]
public class PlatoonAction
{
    [SerializeField] string name;
    public UnitType SquadType;
    public Action<Platoon> Execution;
    public Sprite Sprite;
    public Func<Platoon, bool> IsEnabled; //object - context
}

[Serializable]
public class RiflePlatoonUIActions : IUIUnitActions
{

    [SerializeField] List<PlatoonAction> actions;

    //List<Action<Soldier>> actions;

    public List<PlatoonActionInfo> GetIcons(Platoon pl)
    {
        List<PlatoonActionInfo> info = new List<PlatoonActionInfo>();
        var size = actions.Count;
        for (int i = 0; i < size; i++)
        {
            var action = actions[i];

            if (!action.IsEnabled(pl))
                continue;

            var actionInfo = new PlatoonActionInfo();
            actionInfo.Sprite = action.Sprite;
            actionInfo.ActionIndex = i;

            info.Add(actionInfo);
        }
        return info;
    }

    public void CreateActions(ManagerContext context)
    {
        foreach (var x in actions)
            x.IsEnabled = (p) => true;

        var fwd = context.ForwardingManager;
        var supl = context.SupplyManager;
        
        SetExecution(actions[0], (unit) => unit.CoverState = CoverState.Standing);
        SetExecution(actions[1], (unit) => unit.CoverState = CoverState.Crouch);
        SetExecution(actions[2], (unit) => unit.CoverState = CoverState.Prone);

        actions[3].Execution = (p) => fwd.StartForwarding(ForwardingType.Airstrike);
        actions[3].IsEnabled = (p) => supl.GetUpgradeFlags(p).HasAirForwarder;

        actions[4].Execution = (p) => fwd.StartForwarding(ForwardingType.Mortar);
        actions[4].IsEnabled = (p) => {
            var hqHasMortars = context.UnitManager.HQPlatoon != null ?
                context.UnitManager.HQPlatoon.Squads.Any(x => x.Type == UnitType.Mortar) :
                false;
            return supl.GetUpgradeFlags(p).HasMortarForwarder && hqHasMortars;
        };

        actions[5].Execution = (p) => fwd.StartForwarding(ForwardingType.Artillery);
        actions[5].IsEnabled = (p) => {
            var anyArtilleryOnField = context.UnitManager.ArtilleryPlatoon != null;
            return supl.GetUpgradeFlags(p).HasArtilleryForwarder && anyArtilleryOnField;
        };

        actions[6].Execution = (p) => fwd.StartForwarding(ForwardingType.Sniper);
        actions[6].IsEnabled = (p) => supl.GetUpgradeFlags(p).HasSniperForwarder;
    }

    public void Execute(int index, Platoon platoon)
    {
        var action = actions[index];
        action.Execution(platoon);
    }
    
    void SetExecution(PlatoonAction action, Action<Unit> processUnit)
    {
        action.Execution = (Platoon platoon) =>
        {
            foreach (Squad squad in platoon.Squads)
            {
                //Apply to all or to the specific type
                if(action.SquadType == UnitType.none || action.SquadType == squad.Type)
                {
                    foreach(Unit unit in squad.Units)
                    {
                        processUnit(unit);
                    }
                }
            }
        };
    }
}


[Serializable]
public class HQPlatoonUIActions : IUIUnitActions
{
    [SerializeField] List<PlatoonAction> actions;

    public List<PlatoonActionInfo> GetIcons(Platoon pl)
    {
        List<PlatoonActionInfo> info = new List<PlatoonActionInfo>();
        var size = actions.Count;
        for (int i = 0; i < size; i++)
        {
            var action = actions[i];

            if (!action.IsEnabled(pl))
                continue;

            var actionInfo = new PlatoonActionInfo();
            actionInfo.Sprite = action.Sprite;
            actionInfo.ActionIndex = i;

            info.Add(actionInfo);
        }
        return info;
    }

    public void CreateActions(ManagerContext context)
    {
        actions[0].Execution += (plat) =>
        {
            var hq = context.UnitManager.HQ;
            hq.Unpack();
            context.MovementManager.MovePlatoon(plat, hq.Position, Vector3.forward, FormationType.Circle, true);
        };
        actions[0].IsEnabled = (pl) => !(pl.Squads[0].Units[0] as HQ).IsUnpacked;
        //SetExecution(actions[0], (unit) => unit.CoverState = CoverState.Standing);
        //SetExecution(actions[1], (unit) => unit.CoverState = CoverState.Crouch);
        //SetExecution(actions[2], (unit) => unit.CoverState = CoverState.Prone);
        //actions[3].Execution = (p) => context.ForwardingManager.StartForwarding(ForwardingType.Airstrike);
    }

    public void Execute(int index, Platoon platoon)
    {
        var action = actions[index];
        action.Execution(platoon);
    }

    void SetExecution(PlatoonAction action, Action<Unit> processUnit)
    {
        action.Execution = (Platoon platoon) =>
        {
            foreach (Squad squad in platoon.Squads)
            {
                //Apply to all or to the specific type
                if (action.SquadType == UnitType.none || action.SquadType == squad.Type)
                {
                    foreach (Unit unit in squad.Units)
                    {
                        processUnit(unit);
                    }
                }
            }
        };
    }
}

[Serializable]
public class PlatoonActionInfo
{
    public Sprite Sprite;
    public int ActionIndex;
}

/*
[Serializable]
public class SoldierUIActions : IUIUnitActions
{
    List<Action<Soldier>> actions;
    [SerializeField] List<Sprite> sprites;

    public List<Sprite> Icons => sprites;
    
    public void CreateActions()
    {
        actions = new List<Action<Soldier>>();

        actions.Add((Soldier s) => s.CoverState = CoverState.Standing);
        actions.Add((Soldier s) => s.CoverState = CoverState.Crouch);
        actions.Add((Soldier s) => s.CoverState = CoverState.Prone);
    }

    public void Execute(int index, Platoon platoon)
    {
        var action = actions[index];
        foreach (var squad in platoon.Squads)
        {
            foreach (var unit in squad.Units)
            {
                action((Soldier)unit);
            }
        }
    }
}

[Serializable]
public class HelicopterUIActions : IUIUnitActions
{
    List<Action<Helicopter>> actions;
    [SerializeField] List<Sprite> sprites;

    public List<Sprite> Icons => sprites;

    public void CreateActions()
    {
        actions = new List<Action<Helicopter>>();

        actions.Add((Helicopter s) =>
        {
            if (s.UsedSlots > 0)
            {
                Debug.Log("Landing!");
                s.Land();
                s.Register_OnArrival(() => { s.UnmountAll(); Debug.Log("Touch down, go go go!"); });
            }
        });
    }

    public void Execute(int index, Platoon platoon)
    {
        var action = actions[index];
        foreach (var squad in platoon.Squads)
        {
            foreach (var unit in squad.Units)
            {
                action((Helicopter)unit);
            }
        }
    }
}

[Serializable]
public class MortarUIActions : IUIUnitActions
{
    List<Action<Mortar>> actions;
    [SerializeField] List<Sprite> sprites;

    public List<Sprite> Icons => sprites;

    public void CreateActions(ManagerContext context)
    {
        actions = new List<Action<Mortar>>();

        actions.Add((Mortar m) => 
        {
            m.IsInMortarMode = !m.IsInMortarMode;
            context.ArtilleryManager.SetMortarMode(m, m.IsInMortarMode);
        });
        actions.Add((Mortar m) =>
        {
            m.FireFlare();
        });
    }

    public void Execute(int index, Platoon platoon)
    {
        var action = actions[index];
        foreach (var squad in platoon.Squads)
        {
            foreach (var unit in squad.Units)
            {
                action((Mortar)unit);
            }
        }
    }
}

[Serializable]
public class ArtilleryUIActions : IUIUnitActions
{
    List<Action<Artillery>> actions;
    [SerializeField] List<Sprite> sprites;

    public List<Sprite> Icons => sprites;

    public void CreateActions(ManagerContext context)
    {
        actions = new List<Action<Artillery>>();

        actions.Add((Artillery a) => {
            a.IsInAltilleryMode = !a.IsInAltilleryMode;
            context.ArtilleryManager.SetAltilleryMode(a.IsInAltilleryMode, a.Attack);
        });
    }

    public void Execute(int index, Platoon platoon)
    {
        var action = actions[index];
        foreach (var squad in platoon.Squads)
        {
            foreach (var unit in squad.Units)
            {
                action((Artillery)unit);
            }
        }
    }
}
*/

public interface IUIUnitActions
{
    List<PlatoonActionInfo> GetIcons(Platoon platoon);
    void Execute(int index, Platoon platoon);
}