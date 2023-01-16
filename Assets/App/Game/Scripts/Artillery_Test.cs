using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artillery_Test : MonoBehaviour
{
    [SerializeField] float airTime;
    [SerializeField] Transform joint;
    [SerializeField] Transform origin;
    [SerializeField] Transform target;

    // Update is called once per frame
    void Update()
    {
        var velocityVec = CalculateTrajectoryVelocity(joint.position, target.position, airTime);
        Debug.DrawLine(origin.position, origin.position + velocityVec);

        joint.rotation = Quaternion.LookRotation(velocityVec);

        if (Input.GetKeyDown(KeyCode.K))
        {

            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.transform.position = origin.position;
            s.transform.localScale = Vector3.one / 4;

            var rb = s.AddComponent<Rigidbody>();
            rb.angularDrag = 0;
            rb.drag = 0;

            rb.velocity = velocityVec;
            Destroy(s, airTime);
        }
    }

    Vector3 CalculateTrajectoryVelocity(Vector3 origin, Vector3 target, float t)
    {
        float vx = (target.x - origin.x) / t;
        float vz = (target.z - origin.z) / t;
        float vy = ((target.y - origin.y) - 0.5f * Physics.gravity.y * t * t) / t;
        return new Vector3(vx, vy, vz);
    }
}
