using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveTest : MonoBehaviour
{
    [SerializeField] AnimationCurve curve;

    [SerializeField] float scaleX;
    [SerializeField] float scaleY;

    [SerializeField] float visualQuality;


    void Update()
    {
        List<Vector3> points = new List<Vector3>();
        var step = 1 / visualQuality;
        for (float x = 0; x < 1; x += step)
        {
            var y = curve.Evaluate(x);

            points.Add(new Vector3(x * scaleX, y * scaleY, 0));
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            var a = points[i];
            var b = points[i + 1];

            Debug.DrawLine(a, b, Color.red);
        }
    }
}
