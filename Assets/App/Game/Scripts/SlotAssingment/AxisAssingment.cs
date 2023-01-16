using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class AxisAssingmentBehaviour : ISlotAssignBehaviour
{
    public List<Slot> AssignSlots(Vector3 formationOrigin, Vector3 formationDirection,
                                  Vector3 horizontalDirection, Vector3 calculatePoint,
                                  List<Row> rows, List<Unit> units,
                                  IFormationBehaviour formation)
    {
        var slots = rows.SelectMany(x => x.Slots)
                        .OrderBy(x => x.WorldPosition.z)
                        .ToList();
        
        List<IMovableUnit> orderedUnits = units
            .Where(x => x.IsAlive)
            .Cast<IMovableUnit>()
            .OrderBy(x => x.WorldPosition.z)
            .ToList();

        for (int i = 0; i < slots.Count(); i++)
        {
            var unit = orderedUnits[i];
            var slot = slots[i];

            slot.Unit = unit;
        }

        return slots;
    }
}
