using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Camera))]
public class ViewportCamera : MonoBehaviour
{
    private Camera camera;
    private RenderTexture viewportTexture;

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (viewportTexture == null || viewportTexture.width != Screen.width || viewportTexture.height != Screen.height)
        {
            RecreateRenderTexture();
        }
    }

    private void RecreateRenderTexture()
    {
        if (viewportTexture != null)
        {
            viewportTexture.Release();
            Destroy(viewportTexture);
        }

        viewportTexture = new RenderTexture(Screen.width, Screen.height, 24, DefaultFormat.HDR);
        camera.targetTexture = viewportTexture;
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), viewportTexture, ScaleMode.StretchToFill, true);
    }
}
