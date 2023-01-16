using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreefallTest : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] Vector3 velo;
    private void Start()
    {
        rb.velocity = velo;

        var t = Mathf.Sqrt(2 * transform.position.y / (Physics.gravity.magnitude * rb.mass));

        Invoke("Kill", t);
    }

    void Kill()
    {
        Debug.Log("killing at " + transform.position.y);
        Destroy(gameObject);
    }
}
