using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [SerializeField] ScenarioData f5_data;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F5))
        {
            Spawn(f5_data);
        }
    }

    void Spawn(ScenarioData data)
    {
        foreach(var squadData in data.Squads)
        {
            //context.UnitManager.SpawnSquad(squadData);
        }
    }
}

[System.Serializable]
public class ScenarioData
{
    public List<PlatoonScnenarioPoint> Squads;
}

[System.Serializable]
public class PlatoonScnenarioPoint
{
    public Team Team;
    public UnitType Type;
    public FormationType Formation;
    public int Amount;
    [SerializeField] Transform point;
    public Vector3 Position { get { return point.position; } }
    public Vector3 Direction { get { return point.rotation * Vector3.forward; } }
}