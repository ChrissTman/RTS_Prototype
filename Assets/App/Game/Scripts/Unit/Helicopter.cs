using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

[Serializable]
public class HelicopterContext
{
    public GameObject Gfx;

    public Transform MainTransform;
    //public Transform moveTarget;

    public Transform BodyMiddle;

    public Vector3 GroundPosition { get; set; }
    public Action OnCancleMove { get; set; }
    public Action OnArrival { get; set; }

    public string Info;
}

public class Helicopter : Unit, IMountableTarget, IMovableUnit, IInteractable, ISelectable
{
    [SerializeField] float unmountGoDistance;
    [SerializeField] float unmountDelay;


    [SerializeField] HelicopterContext context;
    [SerializeField] HelicopterMove move;


    public override float AttackDistance => 15;
    public override float LineOfSight => 30;

    public float GroupSpacing => 25;

    public override Vector3 BodyMiddle => context.BodyMiddle.position;
    public override CoverState CoverState => CoverState.Standing;

    public override UnitType UnitType => UnitType.Helicopter;

    public void Initialize(UnitDeath unitDeath, RegionManager regionManager, HeatMap heatMap)
    {
        move.Initialize(context);
        base.Initialize(unitDeath, regionManager, heatMap);

    }

    void Update()
    {
        if (!IsAlive)
            return;

        Info = $"{Size}/{UsedSlots}";//context.Info;
        move.UpdateMove();
    }

    public override void OnSelect()
    {
        //throw new System.NotImplementedException();
    }

    bool isVisible;
    public override bool IsVisible => isVisible;
    public override void SetVisibility(bool visible)
    {
        isVisible = visible;
        context.Gfx.SetActive(visible);
    }

    #region IMountableTarget
    public int Size => 4 * 10;
    public int UsedSlots => registeredUnits.Count;
    public bool IsFull { get { return UsedSlots >= Size; } }

    //TODO: Unit slots
    public Transform UnitParent => transform;

    public Vector3 MountablePosition => context.GroundPosition;

    List<Unit> registeredUnits = new List<Unit>();

    public bool SnapsToParent => true;
    public bool Hides => true;

    //public void MountUnit(Unit unit) {
    //    var mountable = (unit as IMountableUnit);
    //    if (mountable != null && !mountable.IsMounted) {
    //        mountable.Mount(this, true, true);
    //        garisonedUnits.Add(unit);
    //    }
    //}

    public void UnmountUnit(Unit unit)
    {
        var mountable = (unit as IMountableUnit);
        if (mountable != null && mountable.IsMounted)
        {
            mountable.Unmount(context.GroundPosition, Vector3.forward);
            UnregisterUnit(unit);
        }
    }

    public void UnmountAll()
    {
        if(registeredUnits.Count > 0)
            StartCoroutine(UnmoutAllSequencial());
    }

    bool unmoutSideToggle;
    IEnumerator UnmoutAllSequencial()
    {
        foreach (var x in registeredUnits.ToArray())
        {
            UnmountUnit(x);
            if (x is IMovableUnit)
            {
                var mu = x as IMovableUnit;

                var trans = context.Gfx.transform;
                var dir = trans.right * unmountGoDistance * (unmoutSideToggle ? 1 : -1);
                var dest = context.GroundPosition + dir;
                unmoutSideToggle = !unmoutSideToggle;

                mu.GoTo(new Vector3[] { dest }, dir);

                yield return new WaitForSeconds(unmountDelay);
            }
        }

        LiftOff();
    }


    public Vector3 RegisterUnit(Unit unit)
    {
        if (!(unit is IMountableUnit))
            throw new System.Exception("Unit is not IMountableUnit");

        if (registeredUnits.Contains(unit))
            throw new Exception("Unit is already registered. Temp err");

        Land();
        registeredUnits.Add(unit);

        if (!IsInvoking("EvaluateWait"))
            InvokeRepeating("EvaluateWait", 0, 1);

        return context.GroundPosition;
    }

    public void UnregisterUnit(Unit unit)
    {
        registeredUnits.Remove(unit);
        if (registeredUnits.Count == 0)
            CancelInvoke("EvaluateWait");
    }

    void EvaluateWait()
    {
        bool areAllGerisoned = true;

        foreach(var x in registeredUnits)
        {
            if (x is IMountableUnit)
            {
                if(!(x as IMountableUnit).IsMounted)
                {
                    areAllGerisoned = false;
                    break;
                }
            }
        }
        
        if(areAllGerisoned && move.IsLanding)
        {
            LiftOff();
        }
        else if(areAllGerisoned && !move.IsLanding)
        {
            CancelInvoke("EvaluateWait");
        }
    }

    #endregion   

    #region IPoolable
    public override void Activate()
    {
        base.Activate();
        context.Gfx.SetActive(true);
    }
    public override void Deactivate()
    {
        base.Deactivate();
        context.Gfx.SetActive(false);

    }
    protected override void Die()
    {
        base.Die();
    }
    #endregion

    #region IMovable

    public Vector3 LastMoveDirection { get; set; }

    public void Land()
    {
        if (!move.IsLanding)
        {
            move.MoveTo(context.GroundPosition, context.Gfx.transform.forward, true);
        }
    }
    public void LiftOff()
    {
        if (move.IsLanding)
        {
            move.MoveTo(context.GroundPosition, transform.forward, false);
        }
    }

    public void GoTo(Vector3[] destinations, Vector3 direction, bool charge = false)
    {
        move.MoveTo(destinations[destinations.Length - 1], direction, false);

        LastMoveDirection = direction;
    }
    public void Stop()
    {
        //?
    }

    public void Register_CancleMove(Action onCancle)
    {
        context.OnCancleMove = null;
        context.OnCancleMove += onCancle;
    }

    public void Register_OnArrival(Action onArrival)
    {
        context.OnArrival = null;
        context.OnArrival += onArrival;
    }

    #endregion

    #region IInteractable

    public void Interact(Squad squad)
    {
        //Debug.Log($"{squad.ID} => {UnitID}");
    }

    #endregion
}

[System.Serializable]
public class HelicopterMove
{

    HelicopterContext context;


    [SerializeField] float touchdownBias;

    [Header("Velocity")]
    [SerializeField] float maxSpeed = 1;
    [SerializeField] float turningSpeed = 0.1f;
    [SerializeField] float climbingSpeed = 0.1f;
    private float climbMomentum = 0;
    private Vector3 momentum;


    [Header("Altitude positioning")]
    [SerializeField] float altitude;
    [SerializeField] float hoverAltitude = 5;
    [SerializeField] float landingAltitude = 5;
    [SerializeField] LayerMask altitudeMask;
    private float desiredAltitude = 5;



    [Header("Navigation")]
    [SerializeField] Vector3 destination;
    [SerializeField] float positionError;
    [SerializeField] float landingDistance;
    public bool IsLanding;
    private Quaternion desiredRotation;


    [Header("Misc")]
    [SerializeField] float rotationDamp = 4f;
    private int lessRays = 0;
    private int forwardCollisionFlag = 0; // smoothed checking of backwards tilt if in danger of forward collision with a hill


    bool newMove; //prevents OnArrived triggering over and over after arriving


    public void Initialize(HelicopterContext context)
    {
        this.context = context;
    }

    /// <summary>
    ///  // TODO:: Change the landing Input to a command.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="direction"></param>
    public void MoveTo(Vector3 destination, Vector3 direction, bool Land)
    {
        //Debug.Log(this.destination);
        newMove = true;

        context.OnCancleMove?.Invoke();
        context.OnCancleMove = null;

        if (Land)
        {
            RaycastHit rh;
            if (Physics.Raycast(destination + Vector3.up * 100, Vector3.down, out rh, 500, altitudeMask))
            {
                if (Vector3.Dot(rh.normal, Vector3.up) < 0.88f)
                {
                    Debug.LogWarning("This heli was asked to land on a too steep angled hill.", context.MainTransform.gameObject);
                    IsLanding = false;
                }
                else
                    IsLanding = true;
            }
        }
        else
            IsLanding = false;

        this.destination = destination;
        // this.direction = direction; //Do we need this for a heli?
        //Debug.Log(this.destination);
    }
    /// <summary>
    /// Remove the height component from a vector
    /// </summary>
    /// <param name="v"></param>
    /// <returns> input vector without the Y component.</returns>
    Vector3 NoY(Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    /// <summary>
    /// Sample the height below the heli, if clipping below terain, signal to go UP
    /// </summary>
    /// <returns>Altitude below the heli</returns>
    float GetAltitude()
    {

        RaycastHit rh;
        if (Physics.Raycast(context.Gfx.transform.position + momentum, Vector3.down, out rh, 1000f, altitudeMask))
        {
            Debug.DrawRay(context.Gfx.transform.position + momentum, rh.point - (context.Gfx.transform.position + momentum), Color.blue);
            context.GroundPosition = rh.point;
            return Vector3.Distance(rh.point, context.Gfx.transform.position + momentum) + 0.5f;

        }
        else return -100f;

    }

    /// <summary>
    /// Destination direction without the Y component
    /// </summary>
    /// <returns>Destination direction without the Y component</returns>
    Vector3 GetPureDirection()
    {
        return NoY(destination - context.MainTransform.position).normalized;
    }

    public void UpdateMove()
    {

        //smoothens the forward/back/left/right tilt of the heli based on the distance to the destination
        float inclinationDump = Mathf.Clamp01(Vector3.Distance(context.MainTransform.position, destination) / landingDistance);

        //Destination direction relative to the heli for tilt purposes
        Vector3 relativeDirection = context.Gfx.transform.InverseTransformDirection(GetPureDirection());

        //pure 2D distance
        float dist = Vector3.Distance(NoY(context.MainTransform.position), NoY(destination));

        //look towards the destination
        var dir = GetPureDirection();
        desiredRotation = Quaternion.LookRotation(dir != Vector3.zero ? dir : context.Gfx.transform.forward);


        //flip the forward/backwards tilt when landing
        if (dist < landingDistance)
            desiredRotation *= new Quaternion((-relativeDirection.z / rotationDamp) * inclinationDump, 0, (-relativeDirection.x / rotationDamp) * inclinationDump, 1f);
        else if (forwardCollisionFlag > 0)
            desiredRotation *= new Quaternion((-relativeDirection.z / (rotationDamp * 3)) * inclinationDump, 0, (-relativeDirection.x / rotationDamp) * inclinationDump, 1f);
        else
            desiredRotation *= new Quaternion((relativeDirection.z / rotationDamp) * inclinationDump, 0, (-relativeDirection.x / rotationDamp) * inclinationDump, 1f);



        //only sample the heights every 6th frame
        if (lessRays++ % 6 == 0)
        {
            altitude = GetAltitude();

            if (!IsLanding && altitude < hoverAltitude)
            {  // check forward collision if can't climb fast enough
                RaycastHit rh;
                if (Physics.Raycast(context.Gfx.transform.position, momentum, out rh, momentum.magnitude * 2, altitudeMask))
                {
                    momentum *= 0.9f;
                    climbMomentum += Mathf.Clamp(desiredAltitude - altitude, -climbingSpeed, climbingSpeed) * Time.deltaTime;
                    forwardCollisionFlag = 20;
                }
                else
                    forwardCollisionFlag -= 1;
            }
        }


        //regular climbing
        climbMomentum += Mathf.Clamp(desiredAltitude - altitude, -climbingSpeed, climbingSpeed) * Time.deltaTime;



        float angleDamping = (Vector3.Dot(NoY(context.Gfx.transform.forward), GetPureDirection()) + 1.01f) / 2f;
        float angleDampingSqrt = Mathf.Sqrt(angleDamping);

        //add movement momentum if not very close to destination
        if (dist > positionError)
            momentum += GetPureDirection() * angleDampingSqrt * Time.deltaTime * maxSpeed;

        //states
        if (dist < positionError)
        { // arrived
            desiredRotation = Quaternion.LookRotation(NoY(context.Gfx.transform.forward));

            if (IsLanding)
                desiredAltitude = landingAltitude;
            else
                desiredAltitude = hoverAltitude;

            if (!IsLanding && newMove)
            {
                newMove = false;
                context.OnArrival?.Invoke();
                context.OnArrival = null;
            }

            // touchdown
            if (IsLanding && (altitude - touchdownBias) < landingAltitude && newMove)
            {
                //Debug.Log("Touchdown");
                newMove = false;
                context.OnArrival?.Invoke();
                context.OnArrival = null;
            }

        }
        else if (dist < landingDistance)
        { // landing
            float f = Mathf.Clamp01(dist / landingDistance); // 0-1 % value of how close to destination. 0 = at destination.
            float mod = 1 - ((1 / f) / 100f);
            momentum *= mod; // slow down when approaching target;

            if (IsLanding)
                desiredAltitude = Mathf.Lerp(landingAltitude, hoverAltitude, f); //descend to ground
            else
                desiredAltitude = hoverAltitude;
        }
        else
        { // on the way to the target but still far away
            desiredAltitude = hoverAltitude;
        }

        context.MainTransform.Translate(momentum / 100f); // apply 2D movement momentum
        momentum -= momentum / 100f;

        context.MainTransform.Translate(new Vector3(0, climbMomentum / 10f, 0));  // apply vertical momentum
        climbMomentum -= climbMomentum / 10f;

        //smoother angle transition when landing
        var currRot = context.Gfx.transform.rotation;
        if (dist < landingDistance || forwardCollisionFlag > 0)
            context.Gfx.transform.rotation = Quaternion.Lerp(currRot, desiredRotation, Time.deltaTime * turningSpeed * Mathf.Clamp01(angleDamping / 4f + 0.1f));
        else
            context.Gfx.transform.rotation = Quaternion.Lerp(currRot, desiredRotation, Time.deltaTime * turningSpeed * Mathf.Clamp01(angleDamping + 0.1f));


        //3D momentum ray
        Debug.DrawRay(context.Gfx.transform.position, new Vector3(momentum.x, climbMomentum, momentum.y), Color.red);
    }
}
