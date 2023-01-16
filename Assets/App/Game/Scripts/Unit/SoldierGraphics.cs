using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[Serializable]
public class SoldierGraphics 
{
    SoldierContext context;

    [SerializeField] List<SoldierWeaponTypeGraphicsPair> typePairs;

    Dictionary<SoldierWeapons, GameObject> graphics = new Dictionary<SoldierWeapons, GameObject>();

    Renderer[] renderers;

    bool initialized;
    public void Initialize(SoldierContext context)
    {
        this.context = context;

        graphics = typePairs.ToDictionary(x => x.Type, x => x.Gfx);

        var gunTrail = context.LineRenderer;
        gunTrail.positionCount = 2;

        renderers = context.GFX.GetComponentsInChildren<Renderer>();
    }

    //TODO: change to coroutine
    async void ShowBulletTrail(Vector3 target)
    {
        var animator = context.Animator;
        var gunTrail = context.LineRenderer;

        animator.SetTrigger("Shoot");
        
        gunTrail.SetPosition(0, context.FireSpot.position);
        gunTrail.SetPosition(1, target);

        //0.1s delay
        await Task.Delay(100);

        if (!GameManager.IsAlive)
            return;

        HideBulletTrail();
    }
    void HideBulletTrail()
    {
        var gunTrail = context.LineRenderer;

        gunTrail.SetPosition(0, Vector3.zero);
        gunTrail.SetPosition(1, Vector3.zero);
    }
    
    void SetTeamVisuals()
    {
        var mainRenderer = context.MainRenderer;
        var projector = context.Projector;
        var team = context.GetTeam();

        mainRenderer.material = (team == Team.TeamRed) ? context.TeamBBodyMaterial : context.TeamABodyMaterial;
        projector.material = (team == Team.TeamRed) ? context.TeamBProjectorMaterial : context.TeamAProjectorMaterial;
    }

    public void DetermineSpeed()
    {
        if(context.NavMeshAgent.velocity.magnitude > 0.05f)
            context.Animator.SetBool("IsMoving", true);
        else
            context.Animator.SetBool("IsMoving", false);
    }

    public void OnStateChanged()
    {
        //if (InState(SoldierState.Chase, SoldierState.GoTo))
        //    context.Animator.SetBool("IsMoving", true);
        //else
        //    context.Animator.SetBool("IsMoving", false);
    }

    public void Activate()
    {
        SetTeamVisuals();
        context.GFX.SetActive(true);
        context.Projector.gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        HideBulletTrail();
        context.GFX.SetActive(false);
        context.Projector.gameObject.SetActive(false);
    }

    public void Graphics_ImmediateAttack(ITargetable target)
    {
        var pos = target.Position + (Vector3.up * CoverUtility.CalculateHight(target.CoverState));
        ShowBulletTrail(pos);
    }

    public void Graphics_NewDestination(Vector3 destination)
    {
        context.Projector.transform.position = destination + Vector3.up * 100;
    }

    public void Graphics_OnCoverStateChanged(CoverState coverState)
    {
        context.Animator.speed = Random.Range(0.6f, 1.1f);
        if (coverState == CoverState.Standing)
            context.Animator.SetInteger("Standing_level", 2);
        if (coverState == CoverState.Crouch)
            context.Animator.SetInteger("Standing_level", 1);
        if (coverState == CoverState.Prone)
            context.Animator.SetInteger("Standing_level", 0);

        //var pos = context.GFX.transform.localPosition;
        //pos.y = y;
        //context.GFX.transform.localPosition = pos;
    }

    public void Graphics_OnWeaponChanged(SoldierWeapons type)
    {
        foreach(var x in graphics.Values)
        {
            x.SetActive(false);
        }

        graphics[type].SetActive(true);
    }

    public void Die()
    {
        context.Animator.SetTrigger("Die");
        var mainRenderer = context.MainRenderer;
        mainRenderer.material = context.DeadBodyMaterial;
    }

    bool currentVisiblity;
    bool isCurrentStateForced;
    //TODO: cache the results. Research again Gameobject on/off vs component on/off
    public void SetVisibility(bool visible, bool force = false)
    {
        if(!force && isCurrentStateForced)
            return;

        if (force)
            isCurrentStateForced = true;

        //Debug.Log("visible: " + visible);

        currentVisiblity = visible;
        foreach(var x in renderers)
        {
            x.enabled = visible;
        }
    }
    public void ResetForceVisibilityState()
    {
        isCurrentStateForced = false;
    }


    /// <summary>
    /// Helper functions to define if the unit is in one of the state.
    /// </summary>
    /// <param name="states">Possible states that the unit can be in</param>
    /// <returns></returns>
    bool InState(params SoldierState[] states)
    {
        foreach (var x in states)
        {
            if (x == context.State)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Helper functions to define if the unit is not in one of the state.
    /// </summary>
    /// <param name="states">Impossible states that the unit can be in</param>
    /// <returns></returns>
    bool NotInState(params SoldierState[] states)
    {
        return !InState(states);
    }

}

[Serializable]
public class SoldierWeaponTypeGraphicsPair
{
    public string Name;
    public SoldierWeapons Type;
    public GameObject Gfx;
}