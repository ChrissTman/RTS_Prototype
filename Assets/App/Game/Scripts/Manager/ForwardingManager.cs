using UnityEngine;
using System.Collections;

public enum ForwardingType { none, Airstrike, Artillery, Mortar, Sniper }

public class ForwardingManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;
    [SerializeField] float snapDistance;
    [SerializeField] Transform marker;

    public bool ForwardingMode { get; private set; }

    ForwardingType currentType;
    ITargetable lastTarget;
    public void StartForwarding(ForwardingType type)
    {
        context.GameManager.TimeScale = TimeScale.Paused;

        currentType = type;
        marker.gameObject.SetActive(true);
        ForwardingMode = true;
    }

    public void UpdateMarker(Vector3 groundPosition)
    {
        var platoon = context.PlayerActions.SelectedPlatoon;
        var unit = platoon.Squads[0].Units[0];
        var pos = unit.WorldPosition;
        var los = unit.LineOfSight;

        var team = context.GameManager.EnemyTeam;
        lastTarget = context.UnitManager.GetClosestUnit(groundPosition, snapDistance, team);

        if (lastTarget != null)
        {
            if (Vector3.Distance(lastTarget.Position, pos) > los)
                lastTarget = null;
        }


        if (lastTarget != null)
        {
            marker.gameObject.SetActive(true);
            marker.position = lastTarget.Position + Vector3.up * 10; 
        }
        else
            marker.gameObject.SetActive(false);
    }

    public void Submit()
    {
        if (lastTarget == null)
        {
            StopForwarding();
            return;
        }

        if(currentType == ForwardingType.Airstrike)
            context.AirstrikeManager.ScheduleAirStrike(lastTarget.Position);

        if (currentType == ForwardingType.Artillery)
        {
            var artilleryPlatoon = context.UnitManager.ArtilleryPlatoon;
            foreach (var x in artilleryPlatoon.Squads)
            {
                if (x.Type == UnitType.Altillery)
                {
                    foreach (var y in x.Units)
                    {
                        var artillery = (y as Artillery);
                        artillery.Attack(lastTarget.Position);
                    }
                }
            }

        }

        if (currentType == ForwardingType.Mortar)
        {
            var hqPlatoon = context.UnitManager.HQPlatoon;
            foreach(var x in hqPlatoon.Squads)
            {
                if(x.Type == UnitType.Mortar)
                {
                    foreach(var y in x.Units)
                    {
                        var mortar = (y as Mortar);
                        mortar.Attack(lastTarget.Position);
                    }
                }
            }
        }

        if(currentType == ForwardingType.Sniper)
        {
            lastTarget.DealDamage(9999999, new EmptyTarget(Vector3.zero));
        }

        StopForwarding();
    }

    public void StopForwarding()
    {
        currentType = ForwardingType.none;
        marker.gameObject.SetActive(false);
        ForwardingMode = false;

        context.GameManager.TimeScale = TimeScale.Normal;
    }
}
