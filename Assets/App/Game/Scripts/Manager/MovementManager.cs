using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

[Serializable]
public enum FormationType
{
    none = 0,
    Box = 1,
    Line = 2,
    CurvedBox = 3,
    Row = 4,
    Circle = 5,
    Arrow = 6,
    DoubleArrow = 7,
}

/// <summary>
/// Slot assignment in formations.
/// </summary>
/// //TODO: make unit spacing individual to every formation
public class MovementManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [Header("Grouping")]
    //[SerializeField] float unitSpacing;
    [SerializeField] float invertAngle;
    [SerializeField] float squadVerticalOffset;
    
    //FixedList<Unit> selectedUnits { get { return context.UnitManager.SelectedUnits; } }

    //all formation's behaviors
    Dictionary<FormationType, Formation> formations = new Dictionary<FormationType, Formation>();

    //current formation type;
    FormationType formationType;
    //current IFormation
    Formation currentFormation { get { return formations[formationType]; } }

    Dictionary<IMovableUnit, Vector3> destinations = new Dictionary<IMovableUnit, Vector3>();

    void Awake()
    {
        //creates different formations and its behaviors
        var layerAssingmentBehaviour = new LayerAssingmentBehaviour() { InvertAngle = invertAngle };
        var axisAssingmentBehaviour = new AxisAssingmentBehaviour();

        formations.Add(FormationType.Box, new Formation()
        {
            FormationBehaviour = new BoxFormationBehaviour(7, false),
            SlotAssignBehaviour = layerAssingmentBehaviour,
        });

        formations.Add(FormationType.Row, new Formation()
        {
            FormationBehaviour = new BoxFormationBehaviour(2, false),
            SlotAssignBehaviour = layerAssingmentBehaviour,
        });

        formations.Add(FormationType.Line, new Formation()
        {
            FormationBehaviour = new BoxFormationBehaviour(int.MaxValue, false),
            SlotAssignBehaviour = layerAssingmentBehaviour,
        });
        formations.Add(FormationType.CurvedBox, new Formation()
        {
            FormationBehaviour = new BoxFormationBehaviour(7, true),
            SlotAssignBehaviour = layerAssingmentBehaviour,
        });

        formations.Add(FormationType.Arrow, new Formation()
        {
            FormationBehaviour = new ArrowFormationBehaviour(),
            SlotAssignBehaviour = axisAssingmentBehaviour,
        });

        formations.Add(FormationType.DoubleArrow, new Formation()
        {
            FormationBehaviour = new DoubleArrowFormationBehaviour(),
            SlotAssignBehaviour = axisAssingmentBehaviour,
        });

        formations.Add(FormationType.Circle, new Formation()
        {
            FormationBehaviour = new CircleFormationBehaviour(),
            SlotAssignBehaviour = axisAssingmentBehaviour,
        });

        //default
        formationType = FormationType.Box;
    }

    //selects specific formation type. Affects formation property.
    public void SetFormationType(FormationType type)
    {
        formationType = type;
    }

    //temp
    bool executeMove = true;
    void Update()
    {
        //temp
        if (Input.GetKeyDown(KeyCode.K))
        {
            executeMove = !executeMove;
        }
    }

    /*
    /// <summary>
    /// Gets current formation.
    /// Orders units and assigns slots.
    /// Sets destinations to the units.
    /// </summary>
    /// <param name="formationOrigin">Origin of the new formation.</param>
    /// <param name="formationDirection">Direction for the new formation.</param>
    /// <param name="forcedFormationType">Setting formation for this move.</param>
    public void MoveUnits(FixedList<IMovableUnit> units, Vector3 formationOrigin, Vector3 formationDirection, FormationType forcedFormationType = FormationType.none)
    {
        var numberOfUnits = units.GetTakenAmount();

        if (numberOfUnits == 0)
            return;

        //Get Group Spacing  from first unit
        float spacing = 0;
        for (int i = 0; i < units.BufferSize; i++)
        {
            if (units.Buffer[i].Taken)
                spacing = units.Buffer[i].Element.GroupSpacing;
        }

        //If the drag is below the threshold - TEMP
        if (formationDirection.magnitude < 0.01f)
        {
            formationDirection = CalculateAwarageDirection(units);
        }

        formationDirection.Normalize();

        
        //points which get calculated by GenerateFormation(...)
        Vector3 formationMidle = Vector3.zero;
        Vector3 horizontalDirection = Vector3.zero; //perpendicular direction to the formation facing direction
        Vector3 calculatePoint = Vector3.zero; //point for the vertical sorting calculation

        var formation = (forcedFormationType == FormationType.none) ?
                        (currentFormation) : (formations[forcedFormationType]);

        //Creates rows of slots
        List<Row> rows = formation.FormationBehaviour.GenerateFormation(
                                               numberOfUnits, spacing,
                                               out horizontalDirection, ref formationDirection, ref formationOrigin,
                                               out formationMidle, out calculatePoint);

        ISlotAssignBehaviour slotAssignBehaviour = formation.SlotAssignBehaviour;

        List<Slot> slots = slotAssignBehaviour.AssignSlots(
            formationOrigin, formationDirection, horizontalDirection, calculatePoint,
            rows, units, formation.FormationBehaviour);

        //Temp
        if (!executeMove)
            return;

        foreach(var slot in slots)
        {
            if(destinations.ContainsKey(slot.Unit))
            {
                destinations.Remove(slot.Unit);
            }

            destinations.Add(slot.Unit, slot.WorldPosition);
            var movable = slot.Unit as IMovableUnit;
            movable.GoTo(slot.WorldPosition, slot.WorldDirection);
        }
        
    }
    */

    public Vector3[] GetPoints(int numberOfUnits, float spacing, Vector3 formationOrigin, Vector3 formationDirection, FormationType type)
    {
        var formation = formations[type];

        //points which get calculated by GenerateFormation(...)
        Vector3 formationMidle = Vector3.zero;
        Vector3 horizontalDirection = Vector3.zero; //perpendicular direction to the formation facing direction
        Vector3 calculatePoint = Vector3.zero; //point for the vertical sorting calculation

        //Creates rows of slots
        List<Row> rows = formation.FormationBehaviour.GenerateFormation(
                                               numberOfUnits, spacing,
                                               out horizontalDirection, ref formationDirection, ref formationOrigin,
                                               out formationMidle, out calculatePoint);

        return rows.SelectMany(x => x.Slots).Select(x => x.WorldPosition).ToArray();
    }

    public void MovePlatoon(Platoon p, Vector3 formationOrigin, Vector3 formationDirection, FormationType type, bool forceNoOffset)
    {
        foreach (Squad squad in p.Squads)
        {
            int numberOfUnits = squad.Alive;

            if (numberOfUnits <= 0)
                continue;

            //Get Group Spacing  from first unit
            var randomUnit = squad.Units[0] as IMovableUnit;
            float spacing = randomUnit.GroupSpacing;
            
            formationDirection.Normalize();

            //points which get calculated by GenerateFormation(...)
            Vector3 formationMidle = Vector3.zero;
            Vector3 horizontalDirection = Vector3.zero; //perpendicular direction to the formation facing direction
            Vector3 calculatePoint = Vector3.zero; //point for the vertical sorting calculation

            var formation = formations[type];

            //Creates rows of slots
            List<Row> rows = formation.FormationBehaviour.GenerateFormation(
                                                   numberOfUnits, spacing,
                                                   out horizontalDirection, ref formationDirection, ref formationOrigin,
                                                   out formationMidle, out calculatePoint);

            ISlotAssignBehaviour slotAssignBehaviour = formation.SlotAssignBehaviour;

            List<Slot> slots = slotAssignBehaviour.AssignSlots(
                formationOrigin, formationDirection, horizontalDirection, calculatePoint,
                rows, squad.Units, formation.FormationBehaviour);

            foreach (var slot in slots)
            {
                if (destinations.ContainsKey(slot.Unit))
                {
                    destinations.Remove(slot.Unit);
                }

                destinations.Add(slot.Unit, slot.WorldPosition);
                var movable = slot.Unit as IMovableUnit;
                movable.GoTo(new Vector3[] { slot.WorldPosition }, slot.WorldDirection);
            }

            //Defines if the origin should be offseted for the next squad
            formationOrigin += forceNoOffset ? Vector3.zero : (-formationDirection * (rows.Count * spacing + squadVerticalOffset));
        }
    }

    public static Vector3 CalculateAwaragePosition(FixedList<IMovableUnit> units)
    {
        float x, y, z;
        x = y = z = 0;

        foreach (Slot<IMovableUnit> slot in units)
        {
            if (slot.Taken)
            {
                var moveableUnit = slot.Element;
                x += moveableUnit.WorldPosition.x;
                y += moveableUnit.WorldPosition.y;
                z += moveableUnit.WorldPosition.z;
            }
        }

        return new Vector3(x, y, z) / units.GetTakenAmount();
    }

    public static Vector3 CalculateAvaragePosition(FixedList<Unit> units)
    {
        float x, y, z;
        x = y = z = 0;

        foreach (Slot<Unit> slot in units)
        {
            if (!slot.Taken)
                continue;

            var unit = slot.Element;
            
            x += unit.Position.x;
            y += unit.Position.y;
            z += unit.Position.z;
        }

        return new Vector3(x, y, z) / units.GetTakenAmount();
    }

    public static Vector3 CalculateAwarageDirection(FixedList<IMovableUnit> units)
    {
        float x, y, z;
        x = y = z = 0;
        

        foreach (Slot<IMovableUnit> slot in units.Buffer)
        {
            if (!slot.Taken)
                continue;

            var unit = slot.Element;
            
            x += unit.LastMoveDirection.x;
            y += unit.LastMoveDirection.y;
            z += unit.LastMoveDirection.z;
        }

        return new Vector3(x, y, z) / units.GetTakenAmount();
    }

    public static Vector3 CalculateAwaragePosition(List<Vector3> positions)
    {
        float x, y, z;
        x = y = z = 0;

        foreach (var pos in positions)
        {
            x += pos.x;
            y += pos.y;
            z += pos.z;
        }

        return new Vector3(x, y, z) / positions.Count;
    }
}

/// <summary>
/// Slot holds it's position and orientation.
/// State if it's taken. Not needed.
/// Unit which it might got assigned.
/// </summary>
public class Slot
{
    public IMovableUnit Unit;
    public Vector3 WorldPosition;
    public Vector3 WorldDirection;
    public bool Taken;
}

/// <summary>
/// Row of slots.
/// </summary>
public class Row
{
    public List<Slot> Slots = new List<Slot>();
}

