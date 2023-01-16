using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiltAlongVelocity : MonoBehaviour
{
    [SerializeField] Rigidbody rigidbody;
    [SerializeField] Transform target;

    void Update()
    {
        if(rigidbody.velocity != Vector3.zero)
            target.rotation = Quaternion.LookRotation(rigidbody.velocity);
    }
}
