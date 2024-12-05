using System.Collections.Generic;
using Procedural_Mesh_Generation.Island_Generation;
using UnityEngine;

namespace Procedural_Mesh_Generation
{
    public class MeshGenerator
    {
        #region Floors

        protected static void SetUpFloors(MeshData meshData)
        {
            GenerateFloors(meshData);

            float baseRadius = SRnd.RangeFloat(meshData.GenData.MinFloorsRadius, meshData.GenData.MaxFloorsRadius);

            foreach (var floor in meshData.Floors)
            {
                SetFloorPosition(meshData, floor);
                SetFloorRadius(meshData, floor, baseRadius);
            }
        }

        private static void GenerateFloors(MeshData meshData)
        {
            int floorCountToGenerate =
                SRnd.RangeInt(meshData.GenData.MinFloorCount, meshData.GenData.MaxFloorCount + 1);

            List<Floor> originFloorsForNextBranches = new List<Floor>();
            List<Floor> nextOriginFloors = new List<Floor>();

            for (int i = 0; i < floorCountToGenerate; i++)
            {
                int branchToGenerate = meshData.GenData.BranchMultiplicator;

                if (i == 0)
                {
                    GenerateBranch(meshData, ref floorCountToGenerate, 0, null,
                        out Floor lastFloorOfTrunk);

                    nextOriginFloors.Add(lastFloorOfTrunk);
                }

                foreach (var originFloorForNewBranch in originFloorsForNextBranches)
                {
                    for (int j = 0; j < branchToGenerate; j++)
                    {
                        GenerateBranch(meshData, ref floorCountToGenerate, i + j, originFloorForNewBranch,
                            out Floor lastFloorOfBranch);

                        nextOriginFloors.Add(lastFloorOfBranch);
                    }
                }

                foreach (var floor in nextOriginFloors)
                {
                    originFloorsForNextBranches.Add(floor);
                }

                nextOriginFloors.Clear();
            }
        }

        private static void GenerateBranch(MeshData meshData, ref int totalFloorCountToGenerate,
            int indexOfBranch, Floor originFloor, out Floor lastFloorInBranch)
        {
            lastFloorInBranch = null;

            int floorCountToGenerateInBranch = SRnd.RangeInt(meshData.GenData.MinFloorCountInBranch,
                meshData.GenData.MaxFloorCountInBranch + 1);

            for (int i = 0; i < floorCountToGenerateInBranch; i++)
            {
                Floor floor = new Floor
                {
                    Index = meshData.Floors.Count
                };

                SetFloorInfos(meshData, floor, i, indexOfBranch, originFloor);

                meshData.Floors.Add(floor);

                lastFloorInBranch = floor;
            }

            totalFloorCountToGenerate -= floorCountToGenerateInBranch;
        }

        private static void SetFloorInfos(MeshData meshData, Floor floor, int indexInBranch, int branchIndex,
            Floor branchOriginFloor)
        {
            floor.IndexInBranch = indexInBranch;
            floor.BranchIndex = branchIndex;

            if (floor.IsFirstFloorInBranch())
            {
                if (meshData.IsFirstFloor(floor))
                {
                    floor.PreviousFloorIndex = -2;
                }
                else
                {
                    floor.PreviousFloorIndex = branchOriginFloor.Index;
                    branchOriginFloor.NextFloorsIndex.Add(floor.Index);
                    floor.DepthIndex = branchOriginFloor.Index + 1;
                }
            }
            else
            {
                floor.PreviousFloorIndex = meshData.Floors[floor.Index - 1].Index;
                meshData.Floors[floor.Index - 1].NextFloorsIndex.Add(floor.Index);
                floor.DepthIndex = branchIndex != 0 ? branchOriginFloor.Index + indexInBranch + 1 : indexInBranch + 1;
            }

            if (floor.DepthIndex > meshData.DeepestIndex)
            {
                meshData.DeepestIndex = floor.DepthIndex;
            }
        }

        private static void SetFloorPosition(MeshData meshData, Floor floor)
        {
            Vector3 position = meshData.GenerationPoint;

            if (!meshData.IsFirstFloor(floor))
            {
                position = ComputeFloorPos(meshData, floor, meshData.GetPreviousFloor(floor));
            }
            else if (meshData.GenData.HeightBetweenGenPointAndFirstFloor)
            {
                position.y += meshData.GenData.MaxFloorsHeight;
            }

            floor.Pos = position;
        }

        private static void SetFloorNormal(MeshData meshData, Floor floor)
        {
            if (meshData.GetNextFloors(floor).Count == 0)
            {
                floor.Normal = (floor.Pos - meshData.GetPreviousFloor(floor).Pos).normalized;
            }
            else
            {
                Vector3 targetPos = Vector3.zero;
                
                foreach (var nextFloor in meshData.GetNextFloors(floor))
                {
                    targetPos += nextFloor.Pos;
                }

                targetPos /= meshData.GetNextFloors(floor).Count;
                
                floor.Normal = (targetPos - floor.Pos).normalized;
            }
        }

        private static Vector3 ComputeFloorPos(MeshData meshData, Floor currentFloor, Floor previousFloor)
        {
            float height = SRnd.RangeFloat(meshData.GenData.MinFloorsHeight, meshData.GenData.MaxFloorsHeight);
            height += previousFloor.Pos.y;

            float t = previousFloor.Pos.y / meshData.Floors.Count * meshData.GenData.MaxFloorsHeight;
            float offsetValue = meshData.GenData.HeightBasedFloorsOffset.Evaluate(t) *
                                meshData.GenData.FloorsOffesetImpact;

            float posX = previousFloor.Pos.x;
            float posZ = previousFloor.Pos.z;

            if (currentFloor.IsFirstFloorInBranch())
            {
                posX = previousFloor.Pos.x + SRnd.RangeFloat(-offsetValue, offsetValue);
                posZ = previousFloor.Pos.z + SRnd.RangeFloat(-offsetValue, offsetValue);
            }
            else if (!currentFloor.IsTrunk())
            {
                Vector3 branchDir =
                    (previousFloor.Pos - meshData.Floors[previousFloor.PreviousFloorIndex].Pos).normalized;

                posX = previousFloor.Pos.x + branchDir.x * meshData.GenData.BranchDirectionWeight;
                posZ = previousFloor.Pos.z + branchDir.z * meshData.GenData.BranchDirectionWeight;
            }

            return new Vector3(posX, height, posZ);
        }

        private static void SetFloorRadius(MeshData meshData, Floor floor, float baseRadius)
        {
            float t = (float)floor.DepthIndex / meshData.DeepestIndex;
            float swellingValue = meshData.GenData.Swelling.Evaluate(t) * meshData.GenData.SwellingImpact;

            float randomSwelling = 0;

            if (SRnd.RangeInt(0, 100) <= meshData.GenData.RandomSwellingChance)
            {
                randomSwelling = SRnd.RangeFloat(meshData.GenData.MinRandomSwelling,
                    meshData.GenData.MaxRandomSwelling);
            }

            floor.Radius = baseRadius * swellingValue + randomSwelling;
        }

        #endregion

        protected static void CreateVertices(MeshData meshData)
        {
            foreach (var floor in meshData.Floors)
            {
                SetFloorNormal(meshData, floor);

                for (int i = 0; i < meshData.GenData.MeshComplexity; i++)
                {
                    floor.Vertices.Add(ComputeVertexPos(meshData, floor, i));
                }
            }
        }

        private static Vector3 ComputeVertexPos(MeshData meshData, Floor floor, int index)
        {
            float angle = index * 2 * Mathf.PI / meshData.GenData.MeshComplexity;

            float x = floor.Pos.x + floor.Radius * Mathf.Cos(angle) +
                      SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset);

            float y = meshData.IsFirstFloor(floor)
                ? meshData.Floors[0].Pos.y
                : floor.Pos.y + SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset / floor.Index);

            float z = floor.Pos.z + floor.Radius * Mathf.Sin(angle) +
                      SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset);

            var pos = new Vector3(x, y, z);

            return Quaternion.LookRotation(floor.Normal) * Quaternion.Euler(90, 0, 0) * (pos - floor.Pos) + floor.Pos;
        }

        protected static void AddTrianglesToMeshData(MeshData meshData, Floor floor)
        {
            int meshComplexity = meshData.GenData.MeshComplexity;

            for (int i = 0; i < meshComplexity; i++)
            {
                int currentVertex = i + floor.Index * meshComplexity;

                // If last vertex in floor (need to apply changes for it to loop)
                if (i == meshComplexity - 1)
                {
                    int firstVertexInFloor = floor.Index * meshComplexity;

                    if (meshData.IsLastFloor(floor)) continue;

                    foreach (var nextFloor in meshData.GetNextFloors(floor))
                    {
                        int firstVertexInNextFloor = nextFloor.Index * meshComplexity;
                        int currentVertexInNextFloor = i + nextFloor.Index * meshComplexity;

                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(firstVertexInFloor);

                        meshData.Triangles.Add(firstVertexInFloor);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(firstVertexInNextFloor);
                    }
                }
                else
                {
                    int nextVertexInFloor = i + floor.Index * meshComplexity + 1;

                    if (meshData.IsLastFloor(floor)) continue;

                    foreach (var nextFloor in meshData.GetNextFloors(floor))
                    {
                        int currentVertexInNextFloor = i + nextFloor.Index * meshComplexity;
                        int nextVertexInNextFloor = i + nextFloor.Index * meshComplexity + 1;

                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(nextVertexInFloor);

                        meshData.Triangles.Add(nextVertexInFloor);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(nextVertexInNextFloor);
                    }
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

            allVertices.Add(data.GenerationPoint);
            if (data is IslandMeshData islandMeshData) allVertices.Add(islandMeshData.PivotVertex);

            mesh.vertices = allVertices.ToArray();
            mesh.triangles = data.Triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.name = data.GenData.MeshName;

            return mesh;
        }

        protected static T GenerateObj<T>(Mesh mesh, MeshData data) where T : GeneratedObj
        {
            T generatedObj = new GameObject().AddComponent<T>();

            generatedObj.name = data.GenData.ObjName;
            generatedObj.MeshData = data;

            MeshRenderer renderer = generatedObj.gameObject.AddComponent<MeshRenderer>();
            renderer.material = data.GenData.Material;

            MeshFilter islandMeshFilter = generatedObj.gameObject.AddComponent<MeshFilter>();
            islandMeshFilter.mesh = mesh;

            return generatedObj;
        }
    }
}