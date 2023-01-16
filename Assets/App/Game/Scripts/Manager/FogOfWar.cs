using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public ManagerContext context;
    public Material Material;

    Vector4[] data = new Vector4[1000];

    FixedList<MapRevealer> mapRevealers = new FixedList<MapRevealer>(25);

    struct FOW_Points
    {
        public Vector2 Position;
        public float Range;
    }
    
    void Update()
    {
        Material.SetInt("_DataSize", 0);
        UpdateVisualFOW();
        UpdateLogicalFOW();
    }


    int dataAdded = 0;
    void UpdateVisualFOW()
    {
        ResetData();

        dataAdded = 0;

        var team = context.GameManager.CurrentTeam;
        var platoons = context.UnitManager.Platoons;
        var selectedPlatoon = context.PlayerActions.SelectedPlatoon;
        //var mapSize = ManagerContext.MapManager.MapSize;
        //var mapSizeHalf = ManagerContext.MapManager.MapSize / 2f;

        var forwardingMode = context.ForwardingManager.ForwardingMode;

        if (forwardingMode && selectedPlatoon != null) //just selected platoon
        {
            AddPlatoon(selectedPlatoon);
        }
        else //normal
        {
            foreach (Slot<Platoon> slot in platoons)
            {
                if (!slot.Taken) continue;
                AddPlatoon(slot.Element);
            }

            for (int i = 0; i < mapRevealers.BufferSize; i++)
            {
                var buffer = mapRevealers.Buffer;
                var slot = buffer[i];
                if (slot.Taken)
                {
                    var trans = slot.Element;
                    var x = trans.Pos.x;
                    var y = trans.Pos.z;
                    var z = trans.Range;

                    data[dataAdded++] = new Vector4(x, y, z);
                }
            }
        }

        if (dataAdded == 0)
            return;

        Material.SetInt("_DataSize", dataAdded);
        Material.SetVectorArray("_Data", data);
    }

    void AddPlatoon(Platoon platoon)
    {
        var team = context.GameManager.CurrentTeam;


        foreach (var squad in platoon.Squads)
        {
            if (squad.Team == team)
            {
                foreach (var unit in squad.Units)
                {
                    if (!unit.IsAlive || ((unit is IMountableUnit) && (unit as IMountableUnit).IsMounted))
                        continue;

                    unit.SetVisibility(true);

                    bool isSelected = platoon == context.PlayerActions.SelectedPlatoon;

                    var x = unit.Position.x;
                    var y = unit.Position.z;
                    //var z = unit.AttackDistance + 2;
                    var z = unit.LineOfSight;
                    var w = isSelected ? unit.AttackDistance : 0;

                    //if (x == 0 || y == 0)
                    //    print(squad.ID);

                    data[dataAdded++] = new Vector4(x, y, z, w);
                }
            }
        }
    }

    public void AddMapRevealer(MapRevealer mr)
    {
        mapRevealers.Add(mr);
    }
    public void RemoveMapRevealer(MapRevealer mr)
    {
        mapRevealers.Remove(mr);
    }

    void ResetData()
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = new Vector4();
        }
    }
    void UpdateLogicalFOW()
    {
        var team = context.GameManager.EnemyTeam;
        var platoons = context.UnitManager.Platoons;
        foreach (Slot<Platoon> slot in platoons)
        {
            if (!slot.Taken) continue;

            var platoon = slot.Element;
            foreach (var squad in platoon.Squads)
            {
                if (squad.Team == team)
                {
                    foreach (var unit in squad.Units)
                    {
                        bool isIncluded = false;

                        Vector2 unitPos = new Vector2(unit.Position.x, unit.Position.z);
                        for (int i = 0; i < dataAdded; i++)
                        {
                            Vector2 point = new Vector2(data[i].x, data[i].y);
                            float dist = Vector2.Distance(point, unitPos);
                            //print($"{dist} <= {data[i].z}");
                            if (dist < data[i].z)
                            {
                                isIncluded = true;
                                break;
                            }
                        }

                        unit.SetVisibility(isIncluded);
                    }
                }
            }
        }
    }
}