using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Procedural_Mesh_Generation
{
    [Serializable]
    public class MeshData
    {
        [Expandable] public MeshGenerationData GenerationData;
        
        public List<Floor> Floors = new List<Floor>();
        public List<int> Triangles = new List<int>();

        public Vector3 CenterVertex;

        public Floor GetPreviousFloor(Floor currentFloor) => Floors[currentFloor.Index - 1];
        public bool IsFirstFloor(Floor floor) => floor.Index == 0;
        public bool IsLastFloor(Floor floor) => floor.Index == Floors.Count - 1;
    }
}