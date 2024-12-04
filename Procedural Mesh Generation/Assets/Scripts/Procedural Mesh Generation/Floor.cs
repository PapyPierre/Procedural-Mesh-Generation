using System;
using System.Collections.Generic;
using UnityEngine;

namespace Procedural_Mesh_Generation
{
    [Serializable]
    public class Floor
    {
        public int Index;
        public int DepthIndex; // How many previous floor has this floor
        public Vector3 AnchorPos;
        public float Radius;
        
        /// <summary>
        /// -1 means uninitialized, -2 means null
        /// </summary>
        public int PreviousFloorIndex = -1;
        public List<int> NextFloorsIndex = new List<int>();
        
        public readonly List<Vector3> Vertices = new List<Vector3>();
    }
}
