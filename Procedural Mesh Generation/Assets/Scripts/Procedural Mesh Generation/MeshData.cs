using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Procedural_Mesh_Generation
{
    [Serializable]
    public class MeshData
    {
        [Expandable] public MeshGenerationData GenData;
        
        public List<Floor> Floors = new List<Floor>();
        public List<int> Triangles = new List<int>();

        public int DeepestIndex = 0;

        [FormerlySerializedAs("CenterVertex")] public Vector3 GenerationPoint;
        public Vector3 PivotVertex;

        public Floor GetPreviousFloor(Floor currentFloor) => Floors[currentFloor.PreviousFloorIndex];

        public List<Floor> GetNextFloors(Floor currentFloor)
        {
            List<Floor> nextFloors = new List<Floor>();

            foreach (int index in currentFloor.NextFloorsIndex)
            {
                nextFloors.Add(Floors[index]);
            }

            return nextFloors;
        }
        
        public bool IsFirstFloor(Floor floor) => floor.Index == 0;
        public bool IsLastFloor(Floor floor) => floor.Index == Floors.Count - 1;
    }
}