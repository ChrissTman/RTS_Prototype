using UnityEngine;
using System.Collections;

public class ManagerContext : MonoBehaviour
{
    public static ManagerContext Instance;

    [Header("Game Managers")]
    public GameManager GameManager;
    public UnitManager UnitManager;
    public MovementManager MovementManager;
    public AttackManager AttackManager;
    public CameraController CameraController;
    public UnitPool UnitPool;
    public AirstrikeManager AirstrikeManager;
    public ForwardingManager ForwardingManager;
    public SupplyManager SupplyManager;

    [Header("Map")]
    public RegionManager RegionManager;
    public MapManager MapManager;
    public MinimapManager MinimapManager;
    public HeatMap HeatMap;
    public ArtilleryManager ArtilleryManager;
    public FogOfWar FogOfWar;
    public ScenarioManager ScenarioManager;

    [Header("UI")]
    public PlatoonActionManager PlatoonActionManager;
    public UIManager UIManager;
    public ArtilleryUI ArtilleryUI;

    [Header("Player Input")]
    public PlayerActions PlayerActions;
    public PCInput PCInput;

    [Header("Audio")]
    public AudioManager AudioManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
}
