using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirstrikeManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [SerializeField] float airstrikeDelay;
    [SerializeField] float offset = 10;
    [SerializeField] float height;

    [SerializeField] Aeroplane aeroplane;

    float airstrikeTime;

    private void Awake()
    {
        InvokeRepeating("UpdateUI", 0, 1f / 10f);
    }

    void UpdateUI()
    {
        var transition = (airstrikeTime - Time.time) / airstrikeDelay;
        transition = Mathf.Clamp01(transition);
        context.UIManager.UpdateAirstrikeOverlay(transition);
    }

    public void ScheduleAirStrike(Vector3 position)
    {
        airstrikeTime = Time.time + airstrikeDelay;

        var fixedDirection = Vector3.forward;

        var dropPos = position;
        dropPos.y = height;

        var pos = position - (fixedDirection * offset);
        pos.y = height;
        var dir = Quaternion.LookRotation(fixedDirection);

        var aero = Instantiate(aeroplane, pos, dir);
        aero.Context = context;

        var target = position + (fixedDirection * offset);
        target.y = height;
        
        aero.TargetPos = target;
        aero.DropBombAt(dropPos);

        context.AudioManager.PlaySFX(AudioType.OnAirstrike);
    }

    public void TurnOnAirStrikeUI()
    {
        if (Time.time < airstrikeTime)
            return;

        context.ArtilleryManager.SetAltilleryMode(true, ScheduleAirStrike);
    }
}
