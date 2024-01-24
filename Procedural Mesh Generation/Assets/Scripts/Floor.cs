using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Floor
{
    public int index;
    public Vector3 anchorPos;
    public float radius;
    public Color color;
    public int verticesCount;
    public List<Vertex> vertices = new List<Vertex>();
}
