using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtilleryManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [SerializeField] GameObject grid;
    [SerializeField] GameObject marker;
    [SerializeField] float markerAltitude;

    [SerializeField] GameObject explosion;
    
    public bool MortarMode { get; private set; }

    private void Update()
    {
        if (MortarMode && context.PlayerActions.SelectedPlatoon == null)
        {
            MortarMode = false;

        }
    }

    public void SetGridState(bool state)
    {
        grid.SetActive(state);
    }
    public void SetMarkerState(bool state)
    {
        marker.SetActive(state);
    }
    public void UpdateMarker(Vector3 pos)
    {
        var groundPos = pos;

        pos.y = markerAltitude;
        marker.transform.position = pos;

        //var platoon = context.PlayerActions.SelectedPlatoon;
        //if (platoon != null)
        //{
        //    if(platoon.Squads.Type == UnitType.Mortar)
        //    {
        //        foreach(var unit in platoon.Units)
        //        {
        //            var m = unit as Mortar;
        //            m.UpdateTargetPosition(groundPos);
        //        }
        //    }
        //}
    }

    
    public void SetMortarMode(Mortar mortar, bool state)
    {
        if (state)
        {
            MortarMode = true;

            SetGridState(true);
            SetMarkerState(true);
        }
        else
        {
            MortarMode = false;

            SetGridState(false);
            SetMarkerState(false);
        }
    }

    public void SetAltilleryMode(bool state, Action<Vector3> onFire)
    {
        context.UIManager.SetArilleryView(state);
        context.ArtilleryUI.OnFire = onFire;
    }


    //TODO: remove fixed radius
    public void AttackAt(Vector3 groundPos)
    {
        /*
        var squad = context.UnitManager.CurrentSquad;
        if (squad != null)
        {
            if (squad.Type == UnitType.Mortar)
            {
                foreach (var unit in squad.Units)
                {
                    var m = unit as Mortar;
                    m.UpdateTargetPosition(groundPos);

                    m.Attack(groundPos);
                }
            }
            else
            {
                context.AttackManager.Explode(groundPos, 10);
            }
        }
        */
    }
}
