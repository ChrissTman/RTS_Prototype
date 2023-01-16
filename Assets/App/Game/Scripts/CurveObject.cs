using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class CurveObject : MonoBehaviour
{
    [SerializeField] Transform gfx;

    AnimationCurve curve;
    float tStep;
    float xScale;
    float yScale;
    Vector3 origin;
    Vector3 dir;
    bool invertX;

    public Action OnImpact;

    Vector3 posLastFrame;

    float t;
    public void Initialize(AnimationCurve curve, float tStep, float xScale, float yScale, Vector3 origin, Vector3 dir, bool invertX)
    {
        this.curve = curve;
        this.tStep = tStep;
        this.xScale = xScale;
        this.yScale = yScale;
        this.origin = origin;
        this.dir = dir.normalized;
        this.invertX = invertX;

        t = invertX ? 1 : 0;

        OnImpact += () => print("IMPACT");

        posLastFrame = transform.position + Vector3.one;
    }
    
    private void Update()
    {
        t += tStep * xScale * Time.deltaTime * (invertX ? -1f : 1f);

        if ((invertX && t > 0) || (!invertX && t < 1))
        {
            var x = t * xScale;
            var y = curve.Evaluate(t) * yScale;

            var finalPos = origin + dir * x * (invertX ? -1f : 1f); 
            finalPos.y = finalPos.y + y;

            Move(finalPos);
        }
        else
        {
            OnImpact?.Invoke();
            OnImpact = null;
            Destroy(gameObject);
        }

        //Visualization
        List<Vector3> points = new List<Vector3>();
        var step = 1f / 100f;
        for (float x = 0; x < 1; x += step)
        {
            var y = curve.Evaluate(x);

            var pos = origin + dir * x * xScale * (invertX ? -1f : 1f);
            pos.y += y * yScale;
            points.Add(pos);
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            var a = points[i];
            var b = points[i + 1];

            Debug.DrawLine(a, b, Color.red);
        }


        //Tilt
        var bombDir = transform.position - posLastFrame;
        posLastFrame = transform.position;
        
        if(bombDir != Vector3.zero)
            gfx.rotation = Quaternion.LookRotation(bombDir.normalized);
    }

    void Move(Vector3 pos)
    {
        transform.position = pos;
    }
}
