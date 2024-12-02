using System;
using UnityEngine;

namespace Procedural_Mesh_Generation
{
    [Serializable]
    public class Floor
    {
        public int Index;
        public Vector3 AnchorPos;
        public float Radius;
        public bool IsBranching;
    }
}
