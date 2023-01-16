using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActions : MonoBehaviour
{
    [SerializeField] int selectedMaxSize = 40;
    [SerializeField] ManagerContext context;

    public Platoon SelectedPlatoon { get; private set; }

    public FixedList<ISelectable>  Selected { get; private set; }
    public FixedList<IMovableUnit> Selected_IMovableUnit { get; private set; }
    //FixedList<> selected_IMovableUnit;

    void Awake()
    {
        Selected = new FixedList<ISelectable>(selectedMaxSize);
        Selected_IMovableUnit = new FixedList<IMovableUnit>(selectedMaxSize);
    }

    void Update()
    {
        context.UIManager.HighlightPlatoons(context.UnitManager.Platoons);
    }

    #region Select
    
    public void Select(Platoon platoon)
    {
        var team = context.GameManager.CurrentTeam;

        if (platoon.Team != team)
            return;

        ResetOldState();

        SelectedPlatoon = platoon;
        
        var actions = context.PlatoonActionManager;
        var infos = actions.GetIcons(platoon);
        context.UIManager.LoadActionsUIState(infos, actions.Execute);

        foreach(Squad squad in platoon.Squads)
        {
            foreach(Unit unit in squad.Units)
            {
                if(unit.IsAlive)
                    AddSelectable(unit);
            }
        }
        
        FinishSelection();
    }

    public void Select(ISelectable selectable)
    {
        ResetOldState();

        if (selectable != null)
        {
            AddSelectable(selectable);
        }

        FinishSelection();
    }

    void ResetOldState()
    {
        Selected.ClearAll();
        Selected_IMovableUnit.ClearAll();
        SelectedPlatoon = null;
        context.UIManager.ResetActionUI();
    }

    public void ClearSelection()
    {
        ResetOldState();
    }

    void FinishSelection()
    {
        //context.UIManager.HightlightSelected(Selected);
        context.AudioManager.PlaySFX(AudioType.OnSelect);
        context.UIManager.UpdateHudInfo($"Platoon {SelectedPlatoon.Company.Prefix}{SelectedPlatoon.Index}: {SelectedPlatoon.Alive} units");
    }

    void AddSelectable(ISelectable selectable)
    {
        Selected.Add(selectable);

        if (selectable is IMovableUnit)
            Selected_IMovableUnit.Add(selectable as IMovableUnit);

        selectable.OnSelect();
    }

    public void RemoveSelected(ISelectable selectable)
    {
        Selected.Remove(selectable);

        if (selectable is IMovableUnit)
            Selected_IMovableUnit.Remove(selectable as IMovableUnit);
    }

    #endregion

    public void Interact()
    {

    }
    
    public void Action(IInteractable target, Vector3 dragStart_Pos, Vector3 dragEnd_Pos)
    {
        //Perform action based on Drag
        if(target == null)
        {
            var dir = dragEnd_Pos - dragStart_Pos;
            var origin = dragEnd_Pos;

            context.MovementManager.MovePlatoon(SelectedPlatoon, origin, dir, FormationType.Box, false);
        }
        //Perform action on the target
        else
        {
            if (SelectedPlatoon != null)
                context.UnitManager.Interact(target, SelectedPlatoon);
        }
    }
}
