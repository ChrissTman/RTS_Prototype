using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using dis = System.Diagnostics;

/// <summary>
/// Handles attacks.
/// </summary>
public class AttackManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;
    
    [SerializeField] float offsetForAttack;

    [SerializeField] Terrain terrain;

    [Header("temp")]
    [SerializeField] GameObject explosion;

    private void Awake()
    {
        
    }

    public Vector3 CheckVisibility(Vector3 from, float fromOffset, Vector3 to, float toOffset, float radius)
    {
        var start = from;
        var end = to;

        var dist = Vector3.Distance(start, end);

        float x2 = 1;
        float y2 = terrain.SampleHeight(end) + toOffset;

        float step = 1f / dist;

        int samples = (int)(1f / step) + 1;
        float[] heights = new float[samples];

        int index = 0;
        for (float d = 0; d < 1; d += step)
        {
            var pos = Vector3.Lerp(start, end, d);
            var height = terrain.SampleHeight(pos);
            heights[index++] = height;
        }

        for (int i = 0; i < heights.Length; i++)
        {
            float x1 = i * step;
            float y1 = heights[i] + fromOffset;

            bool clear = true;
            for (int h = i + 1; h < samples - 1; h++)
            {
                float x = h * step;
                float y = heights[h];

                float a = y1 - y2;
                float b = x2 - x1;
                float c = (x1 - x2) * y1 + (y2 - y1) * x1;

                float pointDinstace = (a * x + b * y + c) / Mathf.Sqrt(a * a + b * b);

                var pos = Vector3.Lerp(start, end, x1);
                Debug.DrawLine(pos, pos + Vector3.up * 4, Vector3.Distance(pos, to) <= radius ? Color.green : Color.red, 0.5f);


                if (pointDinstace > 0f)
                {
                    clear = false;
                    break;
                }
            }

            if (clear)
            {
                var pos = Vector3.Lerp(start, end, x1);
                pos.y = heights[i];
                
                Debug.DrawLine(pos, pos + Vector3.up * 4, Vector3.Distance(pos, to) <= radius ? Color.green : Color.red, 0.5f);
                if (Vector3.Distance(pos, to) <= radius)
                    return pos;
            }
        }

        throw new Exception("Can't find a viable place on the map to attack!");
    }


    //TODO: group units by distance, assign locations per group ; move to MovementManager
    //TODO: if unit is closer then the offsetForAttack
    //public void AttackUnit(FixedList<Unit> units, ITargetable target)
    //{
    //    //TEMP
    //    var squadPos = MovementManager.CalculateAvaragePosition(units);
    //    var dir = (target.Position - squadPos).normalized;
    //
    //    var distance = Vector3.Distance(squadPos, target.Position);
    //    var offset = (distance < offsetForAttack) ?
    //        distance :
    //        offsetForAttack;
    //
    //
    //    var destination = target.Position - dir * offset;
    //
    //    context.MovementManager.MoveUnits(units, destination, dir, FormationType.CurvedBox);
    //    
    //    foreach(Slot<Unit> slot in units)
    //    {
    //        if (slot.Taken)
    //        {
    //            var unit = slot.Element;
    //            var moveable = unit as IMovableUnit;
    //            moveable.Register_OnArrival(() => (unit as IFireableUnit)?.Fire(target));
    //        }
    //    }
    //
    //    Debug.DrawLine(destination, destination + Vector3.up * 5, Color.cyan, 10, false);
    //}

    public void AttackOrMove(Unit attacker, ITargetable target)
    {
        var fromOffset = CoverUtility.CalculateHight(attacker.CoverState);
        var toOffset = CoverUtility.CalculateHight(target.CoverState);
 
        var point = CheckVisibility(attacker.Position, fromOffset,target.Position, toOffset, attacker.AttackDistance);
        var dir = (target.BodyMiddle - point).normalized;

        var distance = Vector3.Distance(attacker.BodyMiddle, point);
        
        if (distance < 5)
            (attacker as IFireableUnit)?.Fire(target);
        else
        {
            var moveable = attacker as IMovableUnit;

            moveable.GoTo(new Vector3[] { point }, dir);
            moveable.Register_OnArrival(() => (attacker as IFireableUnit)?.Fire(target));
        }
    }

    //TODO: utilize region manager. Do not iterate through all of the units

    /// <summary>
    /// Add explosion to world position
    /// </summary>
    /// <param name="point"></param>
    /// <param name="radius"></param>
    public void Explode(Vector3 point, float radius)
    {
        var e = Instantiate(explosion, point, Quaternion.identity);
        Destroy(e, 5);

        var pool = context.UnitPool;
        foreach(var unitCategory in pool.Units.Values)
        {
            foreach (var slot in unitCategory.Buffer)
            {
                if (slot.Taken)
                {
                    var unit = slot.Element;

                    if (Vector3.Distance(point, unit.Position) < radius)
                    {
                        unit.DealDamage(9999, new EmptyTarget(Vector3.one * 9999));
                        //context.UnitManager.KillUnit(unit);
                    }
                }
            }
        }
    }

    public ITargetable FindTarget(Vector3 pos, float range, Team t)
    {
        //context.RegionManager

        //var units = context.UnitPool.Units.Values;
        //
        //ITargetable unit = null;
        //float lastSmallestDistance = float.MaxValue;
        //foreach (var category in units)
        //{
        //    foreach (var slot in category.Buffer)
        //    {
        //        if (slot.Taken && slot.Element.Team != t)
        //        {
        //            var dist = Vector3.Distance(slot.Element.Position, pos);
        //            if (dist < lastSmallestDistance)
        //            {
        //                unit = slot.Element as ITargetable;
        //                lastSmallestDistance = dist;
        //            }
        //        }
        //    }
        //}

        var team = t == Team.TeamGreen ? Team.TeamRed : Team.TeamGreen;

        Unit unit = context.UnitManager.GetClosestUnit(pos, range, team);
        if(unit != null && unit.Team == t)
            print("MRDKO!");

        if (unit == null)
            return null;
        

        float devider = CoverUtility.CalculateEfficiency(unit.CoverState);

        var distance = Vector3.Distance(pos, unit.Position);
        return distance <= range / devider ? unit : null;
    }
}
