using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Circle formation. More info in IFormation.
/// </summary>
public class CircleFormationBehaviour : IFormationBehaviour
{
    public List<Row> GenerateFormation(
            int numberOfUnits, float unitSpacing,
            out Vector3 horizontalDirection, ref Vector3 formationDirection, ref Vector3 formationOrigin,
            out Vector3 formationMiddle, out Vector3 calculatePoint)
    {
        var slots = new Slot[numberOfUnits];
        
        formationMiddle = formationOrigin;

        //elastic radius based on the number of units;   
        var baseRadius = Mathf.Cos(Mathf.PI / 4) * unitSpacing / 4;
        var radius = numberOfUnits * baseRadius;
        
        //Fixed positions and directions.
        //Because circle is always the same.
        calculatePoint = (formationOrigin + -Vector3.right * radius);
        formationDirection  = Vector3.right;
        horizontalDirection = Vector3.forward;

        //Create circular slots
        for (int i = 0; i < numberOfUnits; i++)
        {
            //0.1f to rotate the circle so layering would work.
            //2 slots don't have the same altitude in the formation.

            // Whole circle / number of units = Angle between two units
            // index * angle between two units = Angle of the unit.
            float angle = i * Mathf.PI * 2 / numberOfUnits + 0.1f;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius; //position is relative

            var destination = pos + formationOrigin;
            slots[i] = new Slot() { WorldPosition = destination, WorldDirection = destination - formationOrigin};
        }
        
        //Order slots along X axis.
        List<Slot> orderedSlots = slots.OrderBy(x => x.WorldPosition.x).ToList();
        
        List<Row> rows = new List<Row>();
        Row currRow = null;

        //Layers 1D list into 2D => Rows>Slots
        int count = 0;
        for (int slot = 0; slot < orderedSlots.Count; slot++)
        {
            var s = orderedSlots[slot];

            if (currRow == null)
                currRow = new Row();
            else if(currRow.Slots.Count >= 2)
            {
                rows.Add(currRow);
                currRow = new Row();
            }

            //Debug.Log($"adding {slot} - {orderedSlots[slot].WorldPosition.x}");
            count++;
            currRow.Slots.Add(s);

            if(slot + 1 == orderedSlots.Count)
                rows.Add(currRow);
        }

        //Temp
        //for (int row = 0; row < rows.Count; row++)
        //{
        //    Debug.Log($"row: {row}");
        //    for (int slot = 0; slot < rows[row].Slots.Count; slot++)
        //    {
        //        Debug.Log($"slot: {slot} ; {rows[row].Slots[slot].WorldPosition} ; {rows[row].GetHashCode()}");
        //    }
        //}
        
        return rows;
    }
}
