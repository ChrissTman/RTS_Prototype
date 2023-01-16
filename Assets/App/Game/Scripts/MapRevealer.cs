using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRevealer : MonoBehaviour
{
    public float Range;
    ManagerContext context;

    [SerializeField] bool autoRevealing;

    public bool IsRevealing { get; set; }

    public Vector3 Pos { get { return transform.position; } }

    void Start()
    {
        context = ManagerContext.Instance;
        if (autoRevealing)
            StartRevealing();
    }

    public void StartRevealing()
    {
        context.FogOfWar.AddMapRevealer(this);
    }
    public void StopRevealing()
    {
        context.FogOfWar.RemoveMapRevealer(this);
    }

    private void OnDestroy()
    {
        StopRevealing();
    }
}
