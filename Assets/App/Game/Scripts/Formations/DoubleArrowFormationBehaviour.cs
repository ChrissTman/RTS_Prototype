using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Arrow formation. More info in IFormation.
/// </summary>
public class DoubleArrowFormationBehaviour : IFormationBehaviour
{
    public List<Row> GenerateFormation(int numberOfUnits, float unitSpacing,
                                       out Vector3 horizontalDirection, ref Vector3 formationDirection, ref Vector3 formationOrigin,
                                       out Vector3 formationMiddle, out Vector3 calculatePoint)
    {
        calculatePoint = formationOrigin;

        horizontalDirection = Vector3.Cross(formationDirection, Vector3.up).normalized;

        formationDirection = -formationDirection.normalized;
        formationMiddle = -Vector3.zero;

        
        var row = new Row();
        var slots = row.Slots;

        float flankSize = numberOfUnits / 4;

        float middleSize = numberOfUnits - flankSize - 3;

        float middleSideOffset = (middleSize % 2 == 0) ? middleSize / 2 : (middleSize + 0) / 2;

        Slot apexLeft = new Slot
        {
            WorldPosition = formationOrigin + horizontalDirection * middleSideOffset * unitSpacing,
            WorldDirection = formationDirection
        };
        Slot apexRight = new Slot
        {
            WorldPosition = formationOrigin - horizontalDirection * middleSideOffset * unitSpacing,
            WorldDirection = formationDirection
        };

        Debug.DrawLine(apexLeft.WorldPosition,  apexLeft.WorldPosition  + Vector3.up * 5, Color.red, 10, false);
        Debug.DrawLine(apexRight.WorldPosition, apexRight.WorldPosition + Vector3.up * 5, Color.red, 10, false);

        slots.Add(apexLeft);
        slots.Add(apexRight);
        Debug.Log($"Adding apexes: {slots.Count}");

        int middleHight = (int)(middleSize / 2) + ((middleSize % 2 == 0) ? 0 : 1);

        int middleSizeCreated = 0;
        for (int y = 1; y <= middleHight; y++)
        {
            Slot left = new Slot()
            {
                WorldPosition = apexLeft.WorldPosition + ((-horizontalDirection * y * unitSpacing) + 
                                                          (-formationDirection  * y * unitSpacing)),
                WorldDirection = formationDirection,
            };
            middleSizeCreated++;
            if (middleSizeCreated < middleSize)
                slots.Add(left);
            else
                break;

            Debug.Log($"Adding middle left{y}: {slots.Count}");

            Slot right = new Slot()
            {
                WorldPosition = apexRight.WorldPosition + ((horizontalDirection * y * unitSpacing) +
                                                          (-formationDirection  * y * unitSpacing)),
                WorldDirection = formationDirection,
            };
            middleSizeCreated++;
            if (middleSizeCreated < middleSize)
                slots.Add(right);
            else
                break;
            Debug.Log($"Adding middle right{y}: {slots.Count}");
        }

        //Left flank
        for (int i = 1; i <= flankSize; i++)
        {
            var s = new Slot();

            s.WorldPosition = apexLeft.WorldPosition + ((horizontalDirection * i * unitSpacing) + 
                                                       (-formationDirection  * i * unitSpacing));

            slots.Add(s);
        }
        Debug.Log($"Adding left flank: {slots.Count}");


        //Right flank
        for (int i = 1; i <= flankSize; i++)
        {
            var s = new Slot();

            s.WorldPosition = apexRight.WorldPosition + ((-horizontalDirection * i * unitSpacing) +
                                                         (-formationDirection  * i * unitSpacing));

            slots.Add(s);
        }

        Debug.Log($"Adding right flank: {slots.Count}");

        Debug.Log($"created: {row.Slots.Count}, needed: {numberOfUnits}");
        
        return new List<Row> { row };
        
        /*
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
            Vector3 destination1 = formationOrigin + (horizontalDirection * delta) + (formationDirection * delta);
            Vector3 destination2 = formationOrigin + (horizontalDirection * -delta) + (formationDirection * delta);

            currentRow.Slots.Add(new Slot() { WorldPosition = destination1, WorldDirection = -formationDirection });
            if (y * 2 + 1 <= numberOfUnits) //if the amount is uneven, destination 2 is not included on the last row.
                currentRow.Slots.Add(new Slot() { WorldPosition = destination2, WorldDirection = formationDirection });

            rows.Add(currentRow);
        }

        return rows;
        */
    }
}
