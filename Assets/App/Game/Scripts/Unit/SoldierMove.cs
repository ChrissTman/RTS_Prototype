using UnityEngine;
using System;
using System.Collections;
using UnityEngine.AI;

public delegate void NewDestination(Vector3 destination);

[Serializable]
public class SoldierMove
{
    [Tooltip("Smoothing of the rotation while moving")]
    [SerializeField] float rotationSmoothing;

    [Tooltip("Smoothness of rotation to the final rotation when the unit arrives")]
    [SerializeField] float rotationSmoothingArrival;

    [Tooltip("Speed of normal walking")]
    [SerializeField] float walkSpeed;

    [Tooltip("Speed of crouch walking")]
    [SerializeField] float crouchSpeed;

    [Tooltip("Speed of prone walking")]
    [SerializeField] float proneSpeed;

    [Tooltip("Speed while charging")]
    [SerializeField] float chargeSpeed;

    [Tooltip("Speed while the unit is in recon mode")]
    [SerializeField] float reconSpeed;

    [Tooltip("Spacing between units in a group")]
    public float GroupSpacing;


    NavMeshAgent agent { get { return context.NavMeshAgent; } }
    float currentSpeed { get { return agent.speed; } set { agent.speed = value; } }
    SoldierState state { get { return context.State; } set { context.State = value; } }
    CoverState cover { get { return context.CoverState; } set { context.CoverState = value; } }
    Vector3 position { get { return context.MainTransform.position; } }

    SoldierContext context;

    public NewDestination NewDestination;

    bool initialized;
    public void Initialize(SoldierContext context)
    {
        this.context = context;

        context.OnModeChanged += Move_OnModeChanged;
        context.OnCoverChaged += Move_OnCoverChanged;

        initialized = true;
    }

    public void Activate()
    {
        agent.enabled = true;
        
        //Debug.Log(agent.enabled);
        agent.updateRotation = false;

        //Force update
        Move_OnModeChanged();
        Move_OnCoverChanged();
    }

    public void Deactivate()
    {
        agent.enabled = false;
    }


    //float StandingSpeed;
    void Move_OnModeChanged()
    {
        //if(context.Mode == SquadMode.Normal)
        //{
        //    StandingSpeed = walkSpeed;
        //}
        //else if (context.Mode == SquadMode.Recon)
        //{
        //    StandingSpeed = reconSpeed;
        //}
    }

    void Move_OnCoverChanged()
    {
        if (context.CoverState == CoverState.Standing)
            currentSpeed = walkSpeed;
        else if (context.CoverState == CoverState.Crouch)
            currentSpeed = crouchSpeed;
        else if (context.CoverState == CoverState.Prone)
            currentSpeed = proneSpeed;
    }

    public void Move_AttackingUnit(ITargetable target)
    {
        LookAtLocation(target.BodyMiddle);
    }

    public static Transform markerRoot;

    int destinationIndex;
    Vector3[] destinations;
    /// <summary>
    /// Set the target destination.
    /// </summary>
    /// <param name="destinations">world space. Has to be on Navmesh.</param>
    public void GoTo(Vector3[] destinations, Vector3 direction, bool charge = false)
    {
        this.destinations = destinations;
        context.OnMoveCancled?.Invoke();

        if (!agent.isOnNavMesh) //Retoggle agent to avoid odd states
        {
            agent.enabled = false;
            agent.enabled = true;
        }
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Not on navmesh!");
            return;
        }

        destinationIndex = 0;

        var destination = destinations[destinationIndex];
        agent.SetDestination(destination);
        agent.isStopped = false;
        
        context.CurrentTarget = null;
        
        //Debug info
        var marker = context.Projector;
        if (marker.transform.IsChildOf(context.MainTransform))
        {
            if (markerRoot == null)
            {
                markerRoot = new GameObject(" ~*~ Markers root ~*~").transform;
        
                markerRoot.position = Vector3.zero;
                markerRoot.rotation = Quaternion.identity;
                markerRoot.localScale = Vector3.one;
                markerRoot.SetParent(null);
                markerRoot.gameObject.SetActive(false);
            }
        
            marker.transform.SetParent(markerRoot);
        }
        
        marker.transform.position = destinations[destinations.Length - 1] + Vector3.up * 100;

        cover = CoverState.Standing;

        context.DirectionUponArrival = direction;

        NewDestination?.Invoke(destination);
    }

    public void Stop()
    {
        agent.isStopped = true;
    }
    
    /// <summary>
    /// Moves the unit if it has a destination
    /// </summary>
    public void Update()
    {
        if (!context.NavMeshAgent.enabled)
            return;

        var transform = context.MainTransform;

        context.UnitInstance.Info = $"{agent.remainingDistance}";


        bool arrived = false;
        if(!agent.pathPending && agent.remainingDistance < 0.4f)
        {
            if (destinationIndex + 1 == destinations.Length)
                arrived = true;
            else
                agent.SetDestination(destinations[++destinationIndex]);
        }

        if (agent.velocity.sqrMagnitude > Mathf.Epsilon && !arrived && agent.velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(agent.velocity.normalized), Time.deltaTime * rotationSmoothing);
        }
        else if (!agent.isStopped && arrived)
        {
            agent.isStopped = true;
            state = SoldierState.Idle;
            cover = CoverState.Crouch;

            if (context.OnArriaval != null)
            {
                context.OnArriaval();
                context.OnArriaval = null;
            }
        }
        else if (context.DirectionUponArrival != Vector3.zero)
        {
            //TODO: stop setting rotation
            var a = transform.rotation;
            var b = Quaternion.LookRotation(context.DirectionUponArrival.normalized);
            var t = Time.deltaTime * rotationSmoothingArrival;
            transform.rotation = Quaternion.Lerp(a, b, t);
        }
    }

    public void LookAtLocation(Vector3 location)
    {
        var dir = location - position;
        context.DirectionUponArrival = dir;

        //Debug.Log("New feature! ");
    }

    public void Charge(Vector3 destination)
    {
        var dir = destination - context.Position;
        dir.Normalize();

        GoTo(new Vector3[] { destination }, dir, true);
    }
}