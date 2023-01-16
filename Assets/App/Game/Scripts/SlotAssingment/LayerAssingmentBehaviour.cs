using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LayerAssingmentBehaviour : ISlotAssignBehaviour
{
    public float InvertAngle;
    public List<Slot> AssignSlots(Vector3 formationOrigin, Vector3 formationDirection, Vector3 horizontalDirection,
                                  Vector3 calculatePoint, 
                                  List<Row> rows, List<Unit> units, IFormationBehaviour formation)
    {
        int numberOfRows = rows.Count;

        Debug.DrawLine(formationOrigin, formationOrigin + (formationDirection * 0.5f), Color.cyan, 10, false);
        Debug.DrawLine(formationOrigin, formationOrigin + (horizontalDirection * 0.25f), Color.green, 10, false);
        Debug.DrawLine(calculatePoint, calculatePoint + (Vector3.up * 1.0f), Color.yellow, 10, false);

        //Line of the formation
        Vector3 dirStart = formationOrigin;
        Vector3 dirEnd = formationOrigin + formationDirection;

        float unitAngleSum = 0;

        var formationInvert = formation as IInvertable;

        //Creates intersections for every unit/slot
        List<Intersection> intersections = new List<Intersection>();
        foreach (Unit u in units)
        {
            if (!u.IsAlive)
                continue;

            var unit = u as IMovableUnit;

            Vector3 unitPosition = unit.WorldPosition;
            Line perpendicularLine = new Line(unitPosition, unitPosition + horizontalDirection);

            Vector3 unitToFormationDirection = formationOrigin - unit.WorldPosition;
            float angle = Vector3.Angle(formationDirection, unitToFormationDirection);
            unitAngleSum += angle;
            //unit.Info = $"{angle.ToString("0.0")}";

            //if formation direction and perpendicular unit direction intersect
            if (LineIntersection(
                    new Vector2(dirStart.x, dirStart.z),
                    new Vector2(dirEnd.x, dirEnd.z),
                    perpendicularLine.A,
                    perpendicularLine.B,
                    out Vector2 intersection))
            {
                //final Y axis is 0 - works in 2D
                intersections.Add(new Intersection() { Point = new Vector3(intersection.x, 0, intersection.y), Unit = unit });

                Debug.DrawLine(new Vector3(intersection.x, 0, intersection.y), unit.WorldPosition, Color.magenta, 10, false);
            }
        }

        //avarage angle of every unit. The angle is used for mirroring formations. Every formation implements it's own rules
        float averageUnitAngle = unitAngleSum / units.Count();
        Vector2 calculatePoint2D = new Vector2(calculatePoint.x, calculatePoint.z);

        //Order intersections vertically.
        //How far is it along the formation direction.
        List<Intersection> verticalyOrdered = intersections.OrderBy(intersction => Vector2.Distance(new Vector2(intersction.Point.x, intersction.Point.z), calculatePoint2D))
                                            .ToList();

        if (formationInvert != null && formationInvert.CanInvertY(averageUnitAngle, InvertAngle))
            verticalyOrdered.Reverse();

        //Group vertical intersections into layers
        //2D list of intersections
        //can be simplified into 2 foreaches. Indexes might be needed.
        List<List<Intersection>> layers = new List<List<Intersection>>();
        int indexOffset = 0;
        for (int i = 0; i < numberOfRows; i++)
        {
            var currentRow = new List<Intersection>();
            for (int x = 0; x < rows[i].Slots.Count; x++)
            {
                int index = indexOffset + x;

                if (index >= units.Count())
                    break;

                if (i >= verticalyOrdered.Count)
                {
                    Debug.LogWarning($"Can't create slot assignment - index out of range({index}/{verticalyOrdered.Count}. Move won't be siply exected!");
                    return new List<Slot>();
                }

                Debug.DrawLine(rows[i].Slots[x].WorldPosition, rows[i].Slots[x].WorldPosition + Vector3.up, Color.blue, 10, false);

                Vector3 point = verticalyOrdered[index].Point;
                //Temp
                //verticalyOrdered[index].Unit.Info = $"{Vector2.Distance(new Vector2(point.x, point.z), origin2D).ToString("00.00")}-";

                currentRow.Add(verticalyOrdered[index]);
            }

            //if (true)
            //    currentRow.Reverse();

            indexOffset += rows[i].Slots.Count;
            layers.Add(currentRow);
        }

        //Slot assignment in one layer.
        //1D units, 1D slots.
        for (int y = 0; y < numberOfRows; y++)
        {
            var row = rows[y];
            var layer = layers[y];

            //How much is the position on the right
            var orderedUnits = layer.OrderByDescending(inter =>
            {
                Vector3 diff = (inter.Point - inter.Unit.WorldPosition);
                float angle = Vector3.SignedAngle(diff, formationDirection, Vector3.up); //-90 <=> 90 
                bool isNeg = angle < 0;
                float dist = Vector3.Distance(inter.Point, inter.Unit.WorldPosition) * (isNeg ? -1 : 1); // -99 <=> 99

                //Temp
                //inter.Unit.Info += $"{dist.ToString("00.00")}-";

                return dist;
            })
            .ToList();

            if (formationInvert != null && formationInvert.CanInvertX(averageUnitAngle, InvertAngle))
                orderedUnits.Reverse();

            for (int x = 0; x < row.Slots.Count; x++)
            {
                IMovableUnit unit = orderedUnits[x].Unit;
                Slot slot = row.Slots[x];

                //Redundant - old
                slot.Taken = true;

                //Temp
                //unit.Info += $"~{y}|{x}";

                //Slot assignment
                slot.Unit = unit;
            }
        }

        return rows.SelectMany(x => x.Slots).ToList();
    }

    /// <summary>
    /// Calculates an intersection between two 2D lines.
    /// <b>I don't understand this</b>
    /// </summary>
    /// <param name="p1">Ax</param>
    /// <param name="p2">Ay</param>
    /// <param name="p3">Bx</param>
    /// <param name="p4">By</param>
    /// <param name="intersection">result</param>
    /// <returns>if there is any valid results.</returns>
    public bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
            return false;

        var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        //Uncomment this if it should check if the intersection lays on the lines segments or just lines.
        //CZE ; line segment = usecka ; line = primka
        //if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        //{
        //    return false;
        //}

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }
}


/// <summary>
/// Intersection.
/// Reference to the unit the intersection is tied to.
/// </summary>
public class Intersection
{
    public Vector3 Point;
    public IMovableUnit Unit;
}

/// <summary>
/// 2D line segment.
/// </summary>
public class Line
{
    public Vector2 A, B;
    public Line(Vector3 a, Vector3 b)
    {
        A = new Vector2(a.x, a.z);
        B = new Vector2(b.x, b.z);
    }
}