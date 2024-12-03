using System;
using System.Collections.Generic;
using Procedural_Mesh_Generation.Island_Generation;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Procedural_Mesh_Generation
{
    public class MeshGenerator
    {
        protected static void InitSeed(uint seed)
        {
            seed = seed == 0 ? (uint)Random.Range(1, 9999999) : seed;
            SRnd.SetSeed(seed);
        }

        #region Floors

        protected static void SetUpFloors(MeshData meshData)
        {
            int floorCount = SRnd.RangeInt(meshData.GenerationData.MinFloorCount,
                meshData.GenerationData.MaxFloorCount + 1);

            int necessaryBranching = SRnd.RangeInt(meshData.GenerationData.MinNumberOfBranches,
                meshData.GenerationData.MaxNumberOfBranches) - 1;

            GenerateFloors(meshData, floorCount, ref necessaryBranching);

            float directionalSwelling = SRnd.RangeFloat(meshData.GenerationData.MinDirectionSwelling,
                meshData.GenerationData.MaxDirectionSwelling);
            float tempRadius = SRnd.RangeFloat(meshData.GenerationData.MinFloorsRadius,
                meshData.GenerationData.MaxFloorsRadius);

            foreach (var floor in meshData.Floors)
            {
                SetFloorAnchor(meshData, floor);
                SetFloorRadius(meshData, floor, directionalSwelling, tempRadius);
            }
        }

        private static void GenerateFloors(MeshData meshData, int floorCountToGenerate, ref int necessaryBranching,
            int baseIndexOfNewBranch = 0)
        {
            for (int i = 0; i < floorCountToGenerate; i++)
            {
                Floor floor = new Floor
                {
                    Index = baseIndexOfNewBranch + i
                };

                float branchingProba =
                    meshData.GenerationData.BranchingProba.Evaluate((float)floor.Index / floorCountToGenerate);

                if (necessaryBranching > 0 && !meshData.IsFirstFloor(floor))
                {
                    if (SRnd.RangeFloat(0, 1) <= branchingProba)
                    {
                        floor.IsBranching = true;
                        necessaryBranching--;
                        meshData.Floors.Add(floor);
                        GenerateFloors(meshData, floorCountToGenerate - i, ref necessaryBranching, i++);
                        GenerateFloors(meshData, (floorCountToGenerate - i) / 2, ref necessaryBranching, i++);
                        return;
                    }
                }

                meshData.Floors.Add(floor);
            }
        }

        private static void SetFloorAnchor(MeshData meshData, Floor floor)
        {
            if (meshData.IsFirstFloor(floor))
            {
                floor.AnchorPos = meshData.CenterVertex;
            }
            else
            {
                Floor previousFloor;


                previousFloor = meshData.GetPreviousFloor(floor);


                Vector3 randomPos = GetFloorPos(meshData, floor);

                Vector3 previousFloorAnchorPos = previousFloor.AnchorPos;

                float y = meshData.GenerationData.SwellingDirection switch
                {
                    GenerationDirection.Upward => previousFloorAnchorPos.y + randomPos.y,
                    GenerationDirection.Downward => previousFloorAnchorPos.y - randomPos.y,
                    _ => throw new ArgumentOutOfRangeException()
                };

                floor.AnchorPos = new Vector3(randomPos.x, y, randomPos.z);
            }
        }

        private static void SetFloorRadius(MeshData meshData, Floor newFloor, float directionnalSwelling,
            float tempRadius)
        {
            float directionalSwellingValue = (meshData.Floors.Count - newFloor.Index) * directionnalSwelling;

            float randomSwelling = 0;

            if (SRnd.RangeInt(0, 100) <= meshData.GenerationData.RandomSwellingChance)
            {
                randomSwelling = SRnd.RangeFloat(meshData.GenerationData.MinRandomSwelling,
                    meshData.GenerationData.MaxRandomSwelling);
            }

            float radius = tempRadius * directionalSwellingValue + randomSwelling;
            newFloor.Radius = radius;
        }

        protected static Vector3 GetFloorPos(MeshData data, Floor currentFloor)
        {
            float height = SRnd.RangeFloat(data.GenerationData.MinFloorsHeight, data.GenerationData.MaxFloorsHeight);

            float offsetValue =
                data.GenerationData.FloorsMaxOffsetAlongObj.Evaluate((float)currentFloor.Index / data.Floors.Count);

            float xOffset = SRnd.RangeFloat(-offsetValue, offsetValue) +
                            data.GetPreviousFloor(currentFloor).AnchorPos.x;
            float zOffset = SRnd.RangeFloat(-offsetValue, offsetValue) +
                            data.GetPreviousFloor(currentFloor).AnchorPos.z;

            return new Vector3(xOffset, height, zOffset);
        }

        #endregion

        #region Verticies

        protected static void CreateVertices(MeshData meshData)
        {
            foreach (var floor in meshData.Floors)
            {
                for (int i = 0; i < meshData.GenerationData.MeshComplexity; i++)
                {
                    floor.Vertices.Add(GetVertexPos(meshData, floor, i));
                }
            }
        }

        private static Vector3 GetVertexPos(MeshData meshData, Floor floor, int index)
        {
            float angle = index * 2 * Mathf.PI / meshData.GenerationData.MeshComplexity;

            float x = floor.AnchorPos.x + floor.Radius * Mathf.Cos(angle) +
                      SRnd.RangeFloat(0, meshData.GenerationData.VerticesMaxOffset);

            float y = meshData.IsFirstFloor(floor)
                ? meshData.Floors[0].AnchorPos.y
                : floor.AnchorPos.y + SRnd.RangeFloat(0, meshData.GenerationData.VerticesMaxOffset / floor.Index);

            float z = floor.AnchorPos.z + floor.Radius * Mathf.Sin(angle) +
                      SRnd.RangeFloat(0, meshData.GenerationData.VerticesMaxOffset);

            return new Vector3(x, y, z);
        }

        #endregion

        protected static void AddTrianglesToMeshData(MeshData meshData, Floor floor)
        {
            int meshComplexity = meshData.GenerationData.MeshComplexity;

            for (int i = 0; i < meshComplexity; i++)
            {
                int currentVertex = i + floor.Index * meshComplexity;

                // If last vertex in floor (need to apply changes for it to loop)
                if (i == meshComplexity - 1)
                {
                    int firstVertexInFloor = floor.Index * meshComplexity;
                    
                    if (meshData.IsLastFloor(floor)) continue;

                    int firstVertexInNextFloor = meshData.Floors[floor.Index + 1].Index * meshComplexity;
                    int currentVertexInNextFloor = i + meshData.Floors[floor.Index + 1].Index * meshComplexity;

                    meshData.Triangles.Add(currentVertex);
                    meshData.Triangles.Add(firstVertexInFloor);
                    meshData.Triangles.Add(currentVertexInNextFloor);

                    meshData.Triangles.Add(firstVertexInFloor);
                    meshData.Triangles.Add(firstVertexInNextFloor);
                    meshData.Triangles.Add(currentVertexInNextFloor);
                }
                else
                {
                    int nextVertexInFloor = i + floor.Index * meshComplexity + 1;

                    if (meshData.IsLastFloor(floor)) continue;
                    
                    int currentVertexInNextFloor = i + meshData.Floors[floor.Index + 1].Index * meshComplexity;
                    int nextVertexInNextFloor = i + meshData.Floors[floor.Index + 1].Index * meshComplexity + 1;

                    meshData.Triangles.Add(currentVertex);
                    meshData.Triangles.Add(nextVertexInFloor);
                    meshData.Triangles.Add(currentVertexInNextFloor);

                    meshData.Triangles.Add(nextVertexInFloor);
                    meshData.Triangles.Add(nextVertexInNextFloor);
                    meshData.Triangles.Add(currentVertexInNextFloor);
                }
            }
        }


        protected static Mesh CreateMesh(MeshData data)
        {
            Mesh mesh = new Mesh();

            List<Vector3> allVertices = new List<Vector3>();

            foreach (var floor in data.Floors)
            {
                foreach (var vertex in floor.Vertices)
                {
                    allVertices.Add(vertex);
                }
            }

            allVertices.Add(data.CenterVertex);
            if (data is IslandMeshData islandMeshData) allVertices.Add(islandMeshData.TipVertex);

            mesh.vertices = allVertices.ToArray();
            mesh.triangles = data.Triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.name = data.GenerationData.MeshName;

            return mesh;
        }

        protected static T GenerateObj<T>(Mesh mesh, MeshData data) where T : GeneratedObj
        {
            T generatedObj = new GameObject().AddComponent<T>();

            generatedObj.name = data.GenerationData.ObjName;
            generatedObj.MeshData = data;

            MeshRenderer renderer = generatedObj.gameObject.AddComponent<MeshRenderer>();
            renderer.material = data.GenerationData.Material;

            MeshFilter islandMeshFilter = generatedObj.gameObject.AddComponent<MeshFilter>();
            islandMeshFilter.mesh = mesh;

            return generatedObj;
        }
    }
}