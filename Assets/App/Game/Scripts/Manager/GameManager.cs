using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Team { none, TeamGreen, TeamRed }
public enum GameMode { none, Strategic, Tactical }
public enum TimeScale { none, Paused, Normal, Fast };

public class GameManager : MonoBehaviour
{
    public static bool IsAlive = true;

    public Team CurrentTeam { get; set; } = Team.TeamGreen;
    public Team EnemyTeam { get { return CurrentTeam == Team.TeamGreen ? Team.TeamRed : Team.TeamGreen; } }

    public GameMode GameMode { get; set; } = GameMode.Strategic;

    TimeScale ts;
    public TimeScale TimeScale { get => ts; set { ts = value; UpdateTimescale(); } }

    [SerializeField] ManagerContext context;

    bool toggleHUD;
    
    
    void Awake()
    {
        IsAlive = true;
    }

    void OnDestroy()
    {
        IsAlive = false;
    }

    void Update()
    {
        //toggle teams - temp
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleTeams();
        }

        //change game speed - temp
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    ToggleSpeed();
        //}
        
        //select all units of your team
        //if(Input.GetKeyDown(KeyCode.A))
        //{
        //    context.UnitManager.SelectedUnits.ClearAll();
        //    for (int i = 0; i < context.UnitManager.UnitPool.BufferSize; i++)
        //    {
        //        var poolSlot = context.UnitManager.UnitPool.Buffer[i];
        //        if (poolSlot.Taken && poolSlot.Element.Team == CurrentTeam)
        //            context.UnitManager.SelectedUnits.Add(poolSlot.Element);
        //    }
        //
        //    context.UIManager.UpdateHudInfo();
        //}

        //Kill selected units
        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    //TODO: test
        //    for (int i = 0; i < context.UnitManager.SelectedUnits.BufferSize; i++)
        //    {
        //        var slot = context.UnitManager.SelectedUnits.Buffer[i];
        //        if (slot.Taken)
        //        {
        //            context.UnitManager.KillUnit(slot.Element);
        //        }
        //    }
        //}

        //Make developer screen shot - temp
        if (Input.GetKeyDown(KeyCode.F10))
        {
            var now = DateTime.Now;
            string date = $"{now.ToShortDateString().Replace('/', '_')}x{now.ToShortDateString().Replace('/', '_')}";
            ScreenCapture.CaptureScreenshot("Screenshots/RTS_concept_" + date + ".png");
        }

        //Toggle HUD
        //if(Input.GetKeyDown(KeyCode.H))
        //{
        //    toggleHUD = !toggleHUD;
        //    uiManager.SetUI(!toggleHUD);
        //}
        //
        //if (!toggleHUD)
        //    UpdateUISelection();
        //else
        //    uiManager.HightLightUnits(new List<Unit>());
    }

    //void UpdateUISelection()
    //{
    //    uiManager.HightLightUnits(unitManager.SelectedUnits);
    //}

    public void ToggleTeams()
    {
        //Flips the team
        CurrentTeam = EnemyTeam;

        context.PlayerActions.ClearSelection();
        context.UIManager.UpdateHudInfo();
        context.UIManager.SetSquadTeam(CurrentTeam);
    }

    public void ToggleSpeed()
    {
        switch (TimeScale)
        {
            case TimeScale.Paused:
                TimeScale = TimeScale.Normal;
                break;
            case TimeScale.Normal:
                TimeScale = TimeScale.Fast;
                break;
            case TimeScale.Fast:
                TimeScale = TimeScale.Paused;
                break;
        }
        context.UIManager.UpdateHudInfo();
    }

    void UpdateTimescale()
    {
        switch (TimeScale)
        {
            case TimeScale.Paused:
                Time.timeScale = 0;
                break;
            case TimeScale.Normal:
                Time.timeScale = 1;
                break;
            case TimeScale.Fast:
                Time.timeScale = 5;
                break;
        }
    }

    public void ToggleSelectionMode()
    {
        GameMode = GameMode == GameMode.Strategic ? GameMode.Tactical : GameMode.Strategic;
        
        context.UIManager.UpdateHudInfo();
        context.CameraController.RefreshSelectionMode();
    }
}
