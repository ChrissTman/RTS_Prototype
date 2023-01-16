using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFormationBehaviour
{
    /// <summary>
    /// Generates a formation. Slots in rows. It may override formationDirection.
    /// </summary>
    /// <param name="unitSpacing">Spacing between individual slots</param>
    /// <param name="numberOfUnits">Total number of units in formation</param>
    /// <param name="horizontalDirection">Perpendicular vector to the formation direction</param>
    /// <param name="formationDirection">Direction of the formation</param>
    /// <param name="formationOrigin">Origin of the formation</param>
    /// <param name="formationMiddle">Point which gets calculated</param>
    /// <returns></returns>
    List<Row> GenerateFormation(int numberOfUnits, float unitSpacing,
        out Vector3 horizontalDirection, ref Vector3 formationDirection, ref Vector3 formationOrigin,
        out Vector3 formationMiddle, out Vector3 calculatePoint);
}
