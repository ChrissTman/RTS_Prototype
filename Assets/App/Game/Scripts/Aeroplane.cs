using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aeroplane : MonoBehaviour
{
    public ManagerContext Context { get; set; }

    [SerializeField] Transform gfx;
    public Vector3 TargetPos { get; set; }

    [SerializeField] float speed = 50;


    [Header("Pitch - up/down")]
    [SerializeField] float tiltXModifier = 0.1f;
    [SerializeField] float tiltXMax = 50;
    [SerializeField] float tiltXSmooth = 2;

    [Header("Yaw - left/right")]
    [SerializeField] float tiltYModifier = 0.1f;
    [SerializeField] float tiltYMax = 50;
    [SerializeField] float tiltYSmooth = 50;
    [SerializeField] float turningSpeed = 2;

    [Header("Roll - spin left/right")]
    [SerializeField] float tiltZModifier = 0.1f;
    [SerializeField] float tiltZMax = 50;
    [SerializeField] float tiltZSmooth = 50;

    [Header("Bomber")]
    [SerializeField] LayerMask mask;
    [SerializeField] CurveObject bomb;
    [Tooltip("X is based on Y * height")]
    [SerializeField] float bombDropingRation;
    [SerializeField] float bombHorizontalSpeed;

    [SerializeField] float bombRadius;
    [SerializeField] AnimationCurve bombCurve;

    Vector3 dir;
    //Vector3 dirLastFrame;
    Quaternion targetYRot;
    float yawDeltaAngle;
    Quaternion currentYRot;
    float newYAngle;
    Quaternion finalYRot;
    float pitchAngle;
    float newZAngle;
    Quaternion finalZRot;
    float rollAngle;
    float newXAngle;
    Quaternion finalXRot;
    Quaternion currRot;

    private void Start()
    {
        previousRotation = transform.rotation;


        //fake initial speed
        //positionLastFrame = transform.position + (-transform.forward * speed) * Time.deltaTime;
    }

    void Update()
    {
        if (Context.GameManager.TimeScale == TimeScale.Paused || Time.deltaTime == 0)
            return;

        CalculateAngularSpeed();
        //CalculateSpeed();
        Move();
        
        if(isDropping && Vector3.Distance(transform.position, dropPos) < 0.5f)
        {
            Drop();
        }
    }

    void Move()
    {
        dir = TargetPos - transform.position;
        dir.y = 0;

        var forwardDir = transform.forward;
        forwardDir.y = 0;

        targetYRot = Quaternion.LookRotation(dir);
        currentYRot = Quaternion.LookRotation(forwardDir); //frame behind

        rollAngle = angularVelocity.y;
        yawDeltaAngle = Vector3.Angle(dir, forwardDir);

        pitchAngle = TargetPos.y - transform.position.y;

        var rawXAngle = Mathf.Clamp(-pitchAngle * tiltXModifier, -tiltXMax, tiltXMax);
        newXAngle = Mathf.Lerp(newXAngle, rawXAngle, tiltXSmooth * Time.deltaTime);
        finalXRot = Quaternion.AngleAxis(newXAngle, Vector3.right);

        //var rawYAngle = yawDeltaAngle;
        //newYAngle = Mathf.Lerp(newYAngle, rawYAngle, tiltYSmooth * Time.deltaTime);
        var smoothenedDir = Vector3.Lerp(forwardDir, dir, tiltYSmooth * Time.deltaTime);
        finalYRot = Quaternion.LookRotation(smoothenedDir);//Quaternion.AngleAxis(yawDeltaAngle, Vector3.up);


        var rawZAngle = Mathf.Clamp(-rollAngle * tiltZModifier, -tiltZMax, tiltZMax);
        newZAngle = Mathf.Lerp(newZAngle, rawZAngle, tiltZSmooth * Time.deltaTime);
        finalZRot = Quaternion.AngleAxis(newZAngle, Vector3.forward);


        //transform.rotation = Quaternion.RotateTowards(currRot, targetXRot * targetYRot * targetZRot, turningSpeed * Time.deltaTime);

        //transform.rotation = ;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.LookRotation(smoothenedDir), turningSpeed * Time.deltaTime);
        //transform.localRotation = Quaternion.RotateTowards(transform.localRotation, finalYRot, turningSpeed * Time.deltaTime);
        transform.localRotation *= finalXRot;
        gfx.localRotation = finalZRot;

        //move forward
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    Quaternion previousRotation;
    Vector3 angularVelocity;
    float angularVelocityMagnitude;
    void CalculateAngularSpeed()
    {
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousRotation);

        previousRotation = transform.rotation;

        float angle = 0.0f;
        Vector3 axis = Vector3.zero;

        deltaRotation.ToAngleAxis(out angle, out axis);

        angle *= Mathf.Deg2Rad;

        angularVelocity = axis * angle * (1.0f / Time.deltaTime);
        angularVelocityMagnitude = angularVelocity.magnitude;
    }

    //Vector3 positionLastFrame;
    //Vector3 velocity;
    //void CalculateSpeed()
    //{
    //    velocity = (transform.position - positionLastFrame) / Time.deltaTime;
    //    positionLastFrame = transform.position;
    //}

    float bombX;
    float bombY;
    Vector3 impactPos;
    Vector3 dropPos;
    bool isDropping;
    public void DropBombAt(Vector3 pos)
    {
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 999, mask))
        {
            impactPos = hit.point;

            var p = hit.point;
            var h = pos.y - p.y;
            
            bombX = h * bombDropingRation;
            bombY = h;

            dropPos = pos - transform.forward * bombX;
            isDropping = true;
        }
    }

    void Drop()
    {
        var b = Instantiate(bomb, transform.position, transform.rotation);
        b.Initialize(bombCurve, bombHorizontalSpeed, bombX, bombY, impactPos, transform.forward, true);
        b.OnImpact = Impact;

        isDropping = false;
    }

    void Impact()
    {
        Context.AttackManager.Explode(impactPos, bombRadius);
    }
}
