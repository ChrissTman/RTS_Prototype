using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlareProjectile : MonoBehaviour
{
    [SerializeField] MapRevealer mapRevealer;
    [SerializeField] Rigidbody rigidbody;
    [SerializeField] ParticleSystem particleSystem;

    [SerializeField] float durability;

    public void Ignite()
    {
        rigidbody.isKinematic = true;
        particleSystem.Play();
        mapRevealer.StartRevealing();

        Destroy(gameObject, durability);
    }

    public void Kick(Vector3 velocity)
    {
        rigidbody.velocity = velocity;
    }

    public void IgniteIn(float time)
    {
        Invoke("Ignite", time);
    }

    void Destroy()
    {
        GameObject.Destroy(gameObject);
    }
}
