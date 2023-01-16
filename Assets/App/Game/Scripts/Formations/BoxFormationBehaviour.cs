using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Box formation. More info in IFormation
/// </summary>
public class BoxFormationBehaviour : IFormationBehaviour, IInvertable
{
    int maxRowCount;
    bool curve;
    
    public BoxFormationBehaviour(int maxRowCount, bool curve)
    {
        this.maxRowCount = maxRowCount;
        this.curve = curve;
    }

    public bool CanInvertX(float angle, float treshold) { return false; }
    public bool CanInvertY(float angle, float treshold) { return angle > treshold; }

    public List<Row> GenerateFormation(int numberOfUnits, float unitSpacing,
        out Vector3 horizontalDirection, ref Vector3 formationDirection, ref Vector3 formationOrigin,
        out Vector3 formationMiddle, out Vector3 calculatePoint)
    {
        calculatePoint = formationOrigin;

        //Perpendicular direction to the formation direction.
        horizontalDirection = Vector3.Cross(formationDirection, Vector3.up);

        List<Row> rows = new List<Row>();

        //Includes rows which are not full.
        var numberOfRows = (numberOfUnits / maxRowCount) +
                           ((numberOfUnits % maxRowCount != 0) ? 1 : 0);

        formationMiddle = formationOrigin + (-formationDirection * (numberOfRows / 2) * unitSpacing);

        for (int y = 0; y < numberOfRows; y++)
        {
            int rowSize = (y + 1 >= numberOfRows) ? numberOfUnits - y * maxRowCount : maxRowCount;


            var currentRow = new Row();
            for (int x = 0; x < rowSize; x++)
            {
                var spacing = unitSpacing * Random.Range(0.5f, 1f);

                float oddOffset = rowSize % 2 == 0 ? rowSize / 2 - 0.5f : rowSize / 2;
                Vector3 xOffset = (horizontalDirection * x * spacing) -
                                  (horizontalDirection * oddOffset * spacing);

                Vector3 yOffset = (-formationDirection * y * spacing) - (!curve ? Vector3.zero :
                                  (-formationDirection * Mathf.Abs(x - oddOffset) * spacing / 2));

                var xNoise = Random.Range(0.8f, 1f);
                var yNoise = Random.Range(0.8f, 1f);

                Vector3 destination = formationOrigin + xOffset * xNoise + yOffset * yNoise;

                if (y * maxRowCount + x >= numberOfUnits)
                    break;

                Debug.DrawLine(destination, destination + Vector3.up / 1, Color.blue, 5, false);

                currentRow.Slots.Add(new Slot() { WorldPosition = destination, WorldDirection = formationDirection });
            }
            rows.Add(currentRow);
        }

        return rows;
    }
}
