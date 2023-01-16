using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

delegate Platoon SpawnPlatoonDelegate(Vector3 position, Team team);

/// <summary>
/// Holds all units and squads.
/// Creates new units.
/// Calculates visible units on screen.
/// </summary>
public class UnitManager : MonoBehaviour
{
    //TODO: events - unit/squad created, unit selection updated
    
    [SerializeField] ManagerContext context;

    public HQ HQ { get; private set; }
    public Platoon HQPlatoon { get; private set; }

    public Platoon ArtilleryPlatoon { get; private set; }

    [Header("?")]
    [SerializeField] int squadBufferSize;

    public FixedList<Platoon> Platoons = new FixedList<Platoon>(100);

    Dictionary<PlatoonType, SpawnPlatoonDelegate> platoonCreators = new Dictionary<PlatoonType, SpawnPlatoonDelegate>();

    //public FixedList<Unit> SelectedUnits;
    //public FixedList<Squad> AllSquads;

    //Redundant
    //public Squad CurrentSquad { get { return context.PlayerActions.SelectedSquad; } }

    //int currentUnitAmount;
    //UnitType currentUnitType;

    void Start()
    {
        //AllSquads = new FixedList<Squad>(squadBufferSize);

        platoonCreators.Add(PlatoonType.Rifle, SpawnRiflePlatoon);
        platoonCreators.Add(PlatoonType.Explosive, SpawnExplosivePlatoon);
        platoonCreators.Add(PlatoonType.HQ, SpawnHQPlatoon);
        platoonCreators.Add(PlatoonType.Artillery, SpawnArtilleryPlatoon);
    }

    ////Temp
    //public void SetUnitType(int type)
    //{
    //    currentUnitType = (UnitType)type;
    //
    //    if ((UnitType)type == UnitType.Mortar)
    //        currentUnitAmount = 1;
    //    if ((UnitType)type == UnitType.Altillery)
    //        currentUnitAmount = 1;
    //    if ((UnitType)type == UnitType.Soldier)
    //        currentUnitAmount = 10;
    //    if ((UnitType)type == UnitType.Helicopter)
    //        currentUnitAmount = 1;
    //    if ((UnitType)type == UnitType.Mannequin)
    //        currentUnitAmount = 1;
    //
    //}

    /*
    /// <summary>
    /// This function will dissolve or create a recon sub-squad. Selectable mini squad for recon
    /// </summary>
    /// <param name="currentSquad"></param>
    public void ReconToggle(Squad squad)
    {
        if(squad.ReconSquadActive) //Disable recon
        {
            //set units to normal behavior
            foreach(var unit in squad.ReconUnits)
            {
                unit.SquadMode = SquadMode.Normal;
            }

            //clears all ReconUnit;
            squad.ReconUnits = new List<Unit>();
        }
        else //Enable recon
        {
            int portion = squad.Units.Count / Squad.ReconPortion;

            if(portion < 1)
            {
                Debug.LogError($"Can't create recon out of {portion} units. The squad is too small.");
                return;
            }
            
            //SelectedUnits.ClearAll();
            for (int i = 0; i < portion; i++)
            {
                var unit = squad.Units[i];
                unit.SquadMode = SquadMode.Recon;
                squad.ReconUnits.Add(unit);
            }

            context.PlayerActions.Select(squad, true);
        }

        squad.ReconSquadActive = !squad.ReconSquadActive;
    }
    */

    /// <summary>
    /// Spawn a unit and adds it to internal collections.
    /// </summary>
    /// <param name="pos">Position where to spawn the unit</param>
    /// <param name="team">Team of the new unit.</param>
    /// <returns>New units</returns>
    public Unit SpawnUnit(Vector3 pos, Team team, UnitType type)
    {
        var unit = context.UnitPool.GetUnit(type);
        
        unit.Position = pos;
        unit.Team = team;

        unit.Activate();
        
        return unit;
    }
    
    static int tempPlatoonNumber;
    public Platoon SpawnPlatoon(Vector3 pos, PlatoonType type, bool autoMove = true, bool autoSelect = true)
    {
        if(!platoonCreators.ContainsKey(type)) { Debug.LogError($"Can't spawn platoon of type {type}"); return null; }

        var team = context.GameManager.CurrentTeam;
        var platoon = platoonCreators[type](pos, team);

        platoon.Magazines.Add(AmmunitionType.bullet, 20);
        platoon.Magazines.Add(AmmunitionType.rocket, 4);
        platoon.Magazines.Add(AmmunitionType.grenade, 8);

        Platoons.Add(platoon);

        if(autoSelect)
            context.PlayerActions.Select(platoon);
        if(autoMove)
            context.MovementManager.MovePlatoon(platoon, pos, Vector3.forward, FormationType.Box,false);

        return platoon;
    }

    #region Specific platoons

    int hqPlatNum;
    Platoon SpawnHQPlatoon(Vector3 pos, Team team)
    {
        var platoon = new Platoon { Type = PlatoonType.HQ, Team = team };
        platoon.Company.Prefix = "X";
        platoon.Index = ++hqPlatNum;

        var hqSquad = SpawnHQSquad(pos, platoon, team);
        SpawnMortarSquad(pos, platoon, team, 4);
        SpawnRifleSquad(pos, platoon, team, 15);

        HQPlatoon = platoon;
        HQ = (HQ)hqSquad.Units[0];

        return platoon;
    }

    int artiPlatNum;
    Platoon SpawnArtilleryPlatoon(Vector3 pos, Team team)
    {
        var platoon = new Platoon { Type = PlatoonType.Artillery, Team = team };
        platoon.Company.Prefix = "R";
        platoon.Index = ++artiPlatNum;

        SpawnArtillerySquad(pos, platoon, team, 2);

        ArtilleryPlatoon = platoon;
        return platoon;
    }

    int exploPlatNum;
    Platoon SpawnExplosivePlatoon(Vector3 pos, Team team)
    {
        var platoon = new Platoon { Type = PlatoonType.Explosive, Team = team };
        platoon.Company.Prefix = "B";
        platoon.Index = ++exploPlatNum;

        SpawnRifleSquad(pos, platoon, team, 3);
        SpawnNadeLauncherSquad(pos, platoon, team, 4);
        SpawnHeavyRifleSquad(pos, platoon, team, 5, 0);
        SpawnRifleSquad(pos, platoon, team, 3);

        return platoon;
    }

    int rifPlatNum;
    Platoon SpawnRiflePlatoon(Vector3 pos, Team team)
    {
        var platoon = new Platoon() { Type = PlatoonType.Rifle, Team = team };
        platoon.Company.Prefix = "A";
        platoon.Index = ++rifPlatNum;

        SpawnRifleSquad(pos, platoon, team, 15);
        SpawnNadeLauncherSquad(pos, platoon, team, 5);
        SpawnHeavyRifleSquad(pos, platoon, team, 2, 4);

        return platoon;
    }

    Squad SpawnArtillerySquad(Vector3 pos, Platoon platoon, Team team, int amout)
    {
        var artillerySquad = CreateSquad(team, UnitType.Altillery);
        artillerySquad.Platoon = platoon;

        Spawn(amout, UnitType.Altillery, pos, team, artillerySquad);

        platoon.Squads.Add(artillerySquad);

        return artillerySquad;
    }

    Squad SpawnRifleSquad(Vector3 pos, Platoon platoon, Team team, int amount)
    {
        //Main rifle squad
        var rifleSquad = CreateSquad(team, UnitType.Soldier);
        rifleSquad.Platoon = platoon;

        Spawn(amount, UnitType.Soldier, pos, team, rifleSquad,
            (x) => (x as Soldier).SetWeapon(SoldierWeapons.M16));

        platoon.Squads.Add(rifleSquad);

        return rifleSquad;
    }
    Squad SpawnNadeLauncherSquad(Vector3 pos, Platoon platoon, Team team, int amount)
    {
        //Main rifle squad
        var nadeSquad = CreateSquad(team, UnitType.Soldier);
        nadeSquad.Platoon = platoon;

        Spawn(amount, UnitType.Soldier, pos, team, nadeSquad,
            (x) => (x as Soldier).SetWeapon(SoldierWeapons.M79));

        platoon.Squads.Add(nadeSquad);

        return nadeSquad;
    }
    Squad SpawnHeavyRifleSquad(Vector3 pos, Platoon platoon, Team team, int lawAmount, int m60Amount)
    {
        //Heavy squad
        var heavySquad = CreateSquad(team, UnitType.Soldier);
        heavySquad.Platoon = platoon;

        Spawn(lawAmount, UnitType.Soldier, pos, team, heavySquad,
            (x) => (x as Soldier).SetWeapon(SoldierWeapons.LAW));
        Spawn(m60Amount, UnitType.Soldier, pos, team, heavySquad,
            (x) => (x as Soldier).SetWeapon(SoldierWeapons.M60));

        platoon.Squads.Add(heavySquad);

        return heavySquad;
    }
    Squad SpawnMortarSquad(Vector3 pos, Platoon platoon, Team team, int amount)
    {
        var mortarSquad = CreateSquad(team, UnitType.Mortar);
        mortarSquad.Platoon = platoon;

        Spawn(amount, UnitType.Mortar, pos, team, mortarSquad);
        platoon.Squads.Add(mortarSquad);
        return mortarSquad;
    }

    Squad SpawnHQSquad(Vector3 pos, Platoon platoon, Team team)
    {
        var hqSquad = CreateSquad(team, UnitType.HQ);
        hqSquad.Platoon = platoon;

        Spawn(1, UnitType.HQ, pos, team, hqSquad);

        //Spawn(10, UnitType.Soldier, pos, team, hqSquad,
        //    (x) => (x as Soldier).SetWeapon(SoldierWeapons.M16));
        //Spawn(5, UnitType.Mortar, pos, team, hqSquad, null);

        platoon.Squads.Add(hqSquad);

        return hqSquad;
    }
    
    #endregion

    Squad CreateSquad(Team team, UnitType type)
    {
        var s = new Squad(team, type);

        //s.Ammo.Add(AmmunitionType.bullet, 1);
        //s.Ammo.Add(AmmunitionType.grenade, 1);
        //s.Ammo.Add(AmmunitionType.rocket, 1);

        return s;
    }

    //public Squad SpawnSquad(Vector3 pos)
    //{
    //    /*
    //    currentUnitAmount
    //    context.GameManager.CurrentTeam
    //    currentUnitType
    //    */
    //    return SpawnSquad(currentUnitAmount, context.GameManager.CurrentTeam, pos, Vector3.forward, currentUnitType, FormationType.none);
    //}

    //public Squad SpawnSquad(SquadScnenarioPoint data)
    //{
    //    return SpawnSquad(data.Amount, data.Team, data.Position, data.Direction, data.Type, data.Formation);
    //}

    public Squad SpawnSquad(int amount, Team team, Vector3 position, Vector3 direction, UnitType type, FormationType formation)
    {
        if (amount <= 0)
            return null;

        var newSquad = new Squad(team, type);

        for (int i = 0; i < amount; i++)
        {
            var newUnit = SpawnUnit(position, newSquad.Team, type);
            newUnit.Squad = newSquad;
            newSquad.Units.Add(newUnit);
            //newUnit.Info = $"{newSquad.ID}";
        }

        //AllSquads.Add(newSquad);

        //Callback on click
        //TODO: move it elsewhere
        //Action callback = () =>
        //{
        //    context.PlayerActions.Select(newSquad, false);
        //};
        //context.UIManager.CreateSquadButton(newSquad.Type, newSquad.Info, callback, newSquad.ID);

        //Selects the new squad after creation
        //var playerActions = context.PlayerActions;

        //playerActions.Select(newSquad, false);
        //context.MovementManager.MoveUnits(playerActions.Selected_IMovableUnit, position, direction, formation);

        return newSquad;
    }

    public void Spawn(int amount, UnitType type, Vector3 position, Team team, Squad squad, Action<Unit> initialize = null)
    {
        for (int i = 0; i < amount; i++)
        {
            var u = SpawnUnit(position, team, type);
            u.Squad = squad;
            squad.Units.Add(u);
            squad.Alive++;
            squad.Platoon.Alive++;

            initialize?.Invoke(u);
        }
    }

    /// <summary>
    /// Selects whole squad. In other cases just the recon squad or the normal squad while the recon mode is active.
    /// Deselects current selected units.
    /// </summary>
    /// <param name="squad">Squad to select</param>
    //public void SelectSquad(Squad squad, bool recon = false)
    //{
    //    if (context.GameManager.CurrentTeam != squad.Team)
    //        return;
    //
    //    var units = recon ? squad.ReconUnits : squad.Units;
    //
    //    SelectedUnits.ClearAll();
    //    for (int i = 0; i < units.Count; i++)
    //    {
    //        if (units[i].SquadMode != SquadMode.Recon)
    //        {
    //            SelectedUnits.Add(units[i]);
    //            units[i].OnSelect();
    //        }
    //    }?
    //
    //    CurrentSquad = squad;
    //    context.UIManager.ClearUnitHighlight();
    //    //uiManager.HightLightUnits(squad.Units);
    //}

    public void KillUnit(Unit unit)
    {
        context.PlayerActions.RemoveSelected(unit);


        var squad = unit.Squad;
        squad.Units.Remove(unit);

        if (squad.Units.Count == 0) //Remove Squad
        {
            //print("destroying squad");
            //AllSquads.Remove(squad);
            //context.UIManager.RemoveSquadButton(squad.ID);
            context.UIManager.ResetActionUI();

            var platoon = squad.Platoon;
            platoon.Squads.Remove(squad);

            if (platoon.Squads.Count == 0)
            {
                //print("destroying platoon");
                squad.Platoon.Magazines.Clear();
                if (!Platoons.Remove(squad.Platoon))
                {
                    Debug.LogError($"can't return {platoon.Company.Prefix}{platoon.Index} with {platoon.Alive} units");
                }
            }
        }

        //TODO: implement dying delay for animation
        //unit.Die();
        unit.Deactivate();

        context.UnitPool.ReturnUnit(unit);
        //Destroy(unit.gameObject);
    }

    public void KillCurrentPlatoon()
    {
        var s = context.PlayerActions.SelectedPlatoon;
        if (s == null)
            return;

        foreach(var x in s.Squads.ToArray())
        {
            foreach(var y in x.Units.ToArray())
            {
                y.DealDamage(9999999, new EmptyTarget(Vector3.one * 999));
            }
        }
    }

    //public void MountSelectedUnits(IMountableTarget mountable)
    //{
    //    var selectedUnits = context.UnitManager.SelectedUnits;
    //
    //    for (int i = 0; i < selectedUnits.BufferSize; i++)
    //    {
    //        var slot = selectedUnits.Buffer[i];
    //        if (slot.Taken && !mountable.IsFull)
    //        {
    //            var unit = slot.Element;
    //            Debug.Log(slot.Element.GetType());
    //            var movable = slot.Element as IMovableUnit;
    //            movable.OnArrival += () => mountable.MountUnit(unit); 
    //            movable.GoTo(mountable.Position, Vector3.forward);
    //        }
    //    }
    //}

    public void Interact(IInteractable interactable, Platoon platoon)
    {
        if (interactable is IMountableTarget)
        {
            var mountableTarget = (interactable as IMountableTarget);
            foreach (Squad squad in platoon.Squads)
            {
                foreach (Unit unit in squad.Units)
                {
                    if (unit is IMountableUnit)
                    {
                        var mountableUnit = unit as IMountableUnit;
                        var moveableUnit = unit as IMovableUnit;

                        var hide = mountableTarget.Hides;
                        var snap = mountableTarget.SnapsToParent;

                        var pos = mountableTarget.RegisterUnit(unit);

                        moveableUnit.Register_OnArrival(() => mountableUnit.Mount(mountableTarget, mountableTarget.SnapsToParent, mountableTarget.Hides));

                        moveableUnit.GoTo(new Vector3[] { pos }, Vector3.forward);

                        moveableUnit.Register_CancleMove(() => mountableTarget.UnregisterUnit(unit));
                    }
                }
            }
        }
    }

    public Unit GetClosestUnit(Vector3 position, float range, Team team)
    {
        float smallestDistance = float.MaxValue;
        Unit closestUnit = null;
        
        for (int pI = 0; pI < Platoons.BufferSize; pI++)
        {
            var pSlot = Platoons.Buffer[pI];
            if(pSlot.Taken)
            {
                var platoon = pSlot.Element;
                var squads = platoon.Squads;
                var squadsCount = squads.Count;
                for (int sI = 0; sI < squadsCount; sI++)
                {
                    var squad = squads[sI];
                    var units = squad.Units;
                    var unitCount = units.Count;
                    for (int uI = 0; uI < unitCount; uI++)
                    {
                        var unit = units[uI];
                        if (!unit.IsAlive || unit.Team != team)
                            continue;

                        var distance = Vector3.Distance(position, unit.Position);
                        if (smallestDistance > distance)
                        {
                            smallestDistance = distance;
                            closestUnit = unit;
                        }
                    }
                }
            }
        }

        //foreach(Slot<Platoon> platSlot in Platoons)
        //{
        //    if (!platSlot.Taken)
        //        continue;
        //
        //    var platoon = platSlot.Element;
        //    foreach(Squad squad in platoon.Squads)
        //    {
        //        foreach(Unit unit in squad.Units)
        //        {
        //            if (!unit.IsAlive || unit.Team != team)
        //                continue;
        //
        //            var distance = Vector3.Distance(position, unit.Position);
        //            if(smallestDistance > distance)
        //            {
        //                smallestDistance = distance;
        //                closestUnit = unit;
        //            }
        //        }
        //    }
        //}

        return closestUnit;
    }

    Vector3 NoY(Vector3 v3)
    {
        return new Vector3(v3.x, 0, v3.z);
    }
}