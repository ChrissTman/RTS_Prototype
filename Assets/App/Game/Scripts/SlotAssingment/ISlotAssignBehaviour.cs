using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISlotAssignBehaviour
{
    List<Slot> AssignSlots(Vector3 formationOrigin, Vector3 formationDirection, Vector3 horizontalDirection,
                           Vector3 calculatePoint,
                           List<Row> rows, List<Unit> units, IFormationBehaviour formation);
}
