using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthBuffToWorldPosDemo : MonoBehaviour
{
    public Transform Marker;

    public Material material;
    private new Camera camera;
    private new Transform transform;

    private void Start()
    {
        camera = GetComponent<Camera>();
        transform = GetComponent<Transform>();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // NOTE: code was ported from: https://gamedev.stackexchange.com/questions/131978/shader-reconstructing-position-from-depth-in-vr-through-projection-matrix

        var p = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        p[2, 3] = p[3, 2] = 0.0f;
        p[3, 3] = 1.0f;
        var clipToWorld = Matrix4x4.Inverse(p * camera.worldToCameraMatrix) * Matrix4x4.TRS(new Vector3(0, 0, -p[2, 2]), Quaternion.identity, Vector3.one);
        material.SetMatrix("clipToWorld", clipToWorld);
        material.SetTexture("_MainTex", source);

        Vector4 v4 = Marker.position;
        material.SetVector("_pos", v4);

        Graphics.Blit(source, destination, material);

    }

}