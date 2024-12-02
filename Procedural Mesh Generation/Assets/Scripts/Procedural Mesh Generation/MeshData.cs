using System.Collections.Generic;
using UnityEngine;

namespace Procedural_Mesh_Generation
{
    public class MeshData
    {
        public MeshGenerationData GenerationData;
        
        public readonly List<Floor> Floors = new List<Floor>();

        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
        
        public Vector3 CenterVertex;
    }
}