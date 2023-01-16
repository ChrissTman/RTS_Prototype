using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalisticObject : MonoBehaviour
{
    public float Mass => Rigidbody.mass;
    public Vector3 Velocity { get { return Rigidbody.velocity; } set { Rigidbody.velocity = value; } }

    public Rigidbody Rigidbody;
    [SerializeField] Transform gfx;

    private void Awake()
    {
        Rigidbody.drag = 0;
        Rigidbody.angularDrag = 0;
    }

    void Update()
    {
        gfx.rotation = Quaternion.LookRotation(Rigidbody.velocity);
    }
}
