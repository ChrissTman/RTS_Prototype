using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Arrow formation. More info in IFormation.
/// </summary>
public class ArrowFormationBehaviour : IFormationBehaviour
{
    public List<Row> GenerateFormation(int numberOfUnits, float unitSpacing,
                                       out Vector3 horizontalDirection, ref Vector3 formationDirection, ref Vector3 formationOrigin,
                                       out Vector3 formationMiddle, out Vector3 calculatePoint)
    {
        calculatePoint = formationOrigin;

        horizontalDirection = Vector3.Cross(formationDirection, Vector3.up);

        formationDirection = -formationDirection;
        formationMiddle = -Vector3.zero;

        var rows = new List<Row>();

        //FirstSlot - Apex of the formation.
        Slot firstSlot = new Slot() { WorldPosition = formationOrigin, WorldDirection = formationDirection };
        Row firstRow = new Row() { Slots = new List<Slot>() { firstSlot } };
        rows.Add(firstRow);

        int numberOfRows = (numberOfUnits - 1) / 2 + ((numberOfUnits - 1) % 2 == 0 ? 0 : 1);
        
        //Calculates the rest
        for (int y = 1; y <= numberOfRows; y++)
        {
            var currentRow = new Row();

            float delta = unitSpacing * y; //both for X and Y

            //results for both sides
            Vector3 destination1 = formationOrigin + (horizontalDirection *  delta) + (formationDirection * delta);
            Vector3 destination2 = formationOrigin + (horizontalDirection * -delta) + (formationDirection * delta);
            
            currentRow.Slots.Add(new Slot() { WorldPosition = destination1, WorldDirection = -formationDirection });
            if(y * 2 + 1 <= numberOfUnits) //if the amount is uneven, destination 2 is not included on the last row.
                currentRow.Slots.Add(new Slot() { WorldPosition = destination2, WorldDirection = formationDirection });

            rows.Add(currentRow);
        }

        return rows;
    }
}
