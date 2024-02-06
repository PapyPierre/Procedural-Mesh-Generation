using System;
using System.Collections;
using UnityEngine;

public class Wireframe : MonoBehaviour
{
    private void OnPreRender()
    {
        GL.wireframe = true;
    }

    private void OnRenderObject()
    {
     //   GL.wireframe = true;
    }
    
    private void OnPostRender()
    {
         GL.wireframe = false;
    }
}