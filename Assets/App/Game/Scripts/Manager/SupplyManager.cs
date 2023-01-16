using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SupplyManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [SerializeField] TransportHelicopter transportHelicopter;

    [Header("HQ")]
    [SerializeField] Transform landingMarker;

    Dictionary<Platoon, PlatoonUpgradeFlags> upgradeFlags = new Dictionary<Platoon, PlatoonUpgradeFlags>();

    void Start()
    {
        Invoke("DefaultScenario", 1);
    }

    //temp
    void DefaultScenario()
    {
        CallTransport(PlatoonType.HQ, landingMarker);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            CallTransport(PlatoonType.Explosive);
        }

        if(Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var pl = context.PlayerActions.SelectedPlatoon;
            upgradeFlags.Add(pl, new PlatoonUpgradeFlags() { HasAirForwarder = true });
        }
    }

    public void UpgradeHQMortars()
    {
        var hq = context.UnitManager.HQ;
    }

    public void CallRiflePlatoon()
    {
        CallTransport(PlatoonType.Rifle);
    }
    public void CallExplosivePlatoon()
    {
        CallTransport(PlatoonType.Explosive);
    }
    public void CallArtilleryPlatoon()
    {
        CallTransport(PlatoonType.Artillery);
    }

    public void CallTransport(PlatoonType type, Transform manualMarker = null)
    {
        var lzPos = manualMarker == null ? context.UnitManager.HQ.LZ : manualMarker.position;

        var transPos = NoY(lzPos).normalized * 300;
        transPos.y = lzPos.y;

        var transPosEnd = NoY(lzPos).normalized * -300;
        transPosEnd.y = 50;

        var platoon = context.UnitManager.SpawnPlatoon(Vector3.zero, type, false, false);
        var transport = Instantiate(transportHelicopter, transPos, Quaternion.identity);

        transport.Initialize();

        transport.Move(lzPos, Vector3.forward, true);
        transport.OnArrival += () => transport.UnmountAll();
        transport.OnUnmountAll += () => transport.Move(transPosEnd, Vector3.forward, false);

        foreach (var x in platoon.Squads)
        {
            foreach (var u in x.Units)
            {
                var im = (u as IMountableUnit);
                transport.RegisterUnit(u);
                im.Mount(transport, true, true);
            }
        }
    }

    public PlatoonUpgradeFlags GetUpgradeFlags(Platoon p)
    {
        if (upgradeFlags.TryGetValue(p, out PlatoonUpgradeFlags flags))
            return flags;
        else
        {
            var newFlags = new PlatoonUpgradeFlags();
            upgradeFlags.Add(p, newFlags);
            return newFlags;
        }
    }

    Vector3 NoY(Vector3 v3)
    {
        return new Vector3(v3.x, 0, v3.z);
    }
}

public class PlatoonUpgradeFlags
{
    public bool HasAirForwarder;
    public bool HasMortarForwarder;
    public bool HasSniperForwarder;
    public bool HasArtilleryForwarder;
}