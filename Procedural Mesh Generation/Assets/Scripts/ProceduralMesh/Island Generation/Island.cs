using System.Collections.Generic;
using UnityEngine;

namespace Island_Generation
{
    public class Island
    {
        public IslandGenerationData Data;

        public readonly List<Floor> Floors = new List<Floor>();
    
        public Vector3 CenterVertex;
        public Vector3 TipVertex;
    
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
    }
}