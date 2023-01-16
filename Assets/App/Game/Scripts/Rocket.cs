using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    [SerializeField] float blastRadius;
    [SerializeField] float speed;


    public Action OnImpact;
    public Vector3 TargetPosition;

    private void Start()
    {
        Destroy(gameObject, 7);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
        var dir = TargetPosition - transform.position;
        if (Vector3.Angle(transform.forward, dir) > 90)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        var context = ManagerContext.Instance;

        context.AttackManager.Explode(TargetPosition, blastRadius);
    }
}
