using System.Collections.Generic;
using Procedural_Mesh_Generation.Island_Generation;
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
            int floorCount = SRnd.RangeInt(meshData.GenData.MinFloorCount, meshData.GenData.MaxFloorCount + 1);

            GenerateFloors(meshData, floorCount);

            float baseRadius = SRnd.RangeFloat(meshData.GenData.MinFloorsRadius, meshData.GenData.MaxFloorsRadius);

            foreach (var floor in meshData.Floors)
            {
                SetFloorAnchor(meshData, floor);
                SetFloorRadius(meshData, floor, baseRadius);
            }
        }

        private static void GenerateFloors(MeshData meshData, int floorCountToGenerate)
        {
            List<Floor> originFloorForNextBranches = new List<Floor>();
            List<Floor> nextOriginFloors = new List<Floor>();

            for (int i = 0; i < floorCountToGenerate; i++)
            {
                int branchToGenerate = meshData.GenData.BranchMultiplicator;

                if (i == 0)
                {
                    int floorCountInBranch = SRnd.RangeInt(meshData.GenData.MinFloorCountInBranch,
                        meshData.GenData.MaxFloorCountInBranch + 1);

                    GenerateBranch(meshData, floorCountInBranch, 0, null,
                        out Floor lastFloorOfTrunk);

                    floorCountToGenerate -= floorCountInBranch;

                    nextOriginFloors.Add(lastFloorOfTrunk);
                }

                foreach (var originFloorForNewBranch in originFloorForNextBranches)
                {
                    for (int j = 0; j < branchToGenerate; j++)
                    {
                        int floorCountInBranch = SRnd.RangeInt(meshData.GenData.MinFloorCountInBranch,
                            meshData.GenData.MaxFloorCountInBranch + 1);

                        GenerateBranch(meshData, floorCountInBranch, i + j, originFloorForNewBranch,
                            out Floor lastFloorOfBranch);

                        floorCountToGenerate -= floorCountInBranch;

                        nextOriginFloors.Add(lastFloorOfBranch);
                    }
                }

                foreach (var floor in nextOriginFloors)
                {
                    originFloorForNextBranches.Add(floor);
                }

                nextOriginFloors.Clear();
            }

            /*
            for (int i = 0; i < floorCountToGenerate; i++)
            {
                Floor floor = new Floor
                {
                    Index = meshData.Floors.Count
                };

                SetFloorInfos(meshData, floor, i, baseIndexOfNewBranch);

                meshData.Floors.Add(floor);

                float branchProba = meshData.GenData.BranchProba.Evaluate((float)floor.DepthIndex / totalFloorCount);
                bool canBranch = meshData.GenData.minFloorCountBetweenBranching <= floor.Index;
                int floorLeftToGen = floorCountToGenerate - i;

                if (canBranch && floorLeftToGen > 3 && SRnd.RangeFloat(0f, 1f) <= branchProba)
                {
                    branchCount++;
                    floor.IsBranch = true;

                    GenerateFloors(meshData, floorLeftToGen / 3, ref branchCount, floor.Index, totalFloorCount);

                    if (branchCount < maxBranchCount)
                    {
                        GenerateFloors(meshData,  floorLeftToGen / 3 * 2, ref branchCount, floor.Index,
                            totalFloorCount);
                    }

                    return;
                }
            }
            */
        }

        private static void GenerateBranch(MeshData meshData, int floorCountToGenerateInBranch,
            int indexOfBranch, Floor originFloor, out Floor lastFloorInBranch)
        {
            lastFloorInBranch = null;

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
        }

        private static void SetFloorInfos(MeshData meshData, Floor floor, int indexInBranch, int branchIndex,
            Floor branchOriginFloor)
        {
            floor.IndexInBranch = indexInBranch;
            floor.BranchIndex = branchIndex;

            if (indexInBranch == 0)
            {
                if (branchIndex != 0) // First floor of new branch
                {
                    floor.PreviousFloorIndex = branchOriginFloor.Index;
                    branchOriginFloor.NextFloorsIndex.Add(floor.Index);

                    floor.DepthIndex = branchOriginFloor.Index + 1;
                }
                else // First floor
                {
                    floor.PreviousFloorIndex = -2;
                }
            }
            else // Other floors
            {
                floor.PreviousFloorIndex = meshData.Floors[floor.Index - 1].Index;
                meshData.Floors[floor.Index - 1].NextFloorsIndex.Add(floor.Index);

                if (branchIndex != 0)
                {
                    floor.DepthIndex = branchOriginFloor.Index + indexInBranch + 1;
                }
                else
                {
                    floor.DepthIndex = indexInBranch + 1;
                }
            }
        }

        private static void SetFloorAnchor(MeshData meshData, Floor floor)
        {
            Floor previousFloor = meshData.IsFirstFloor(floor) ? floor : meshData.GetPreviousFloor(floor);

            Vector3 randomPos = ComputeFloorPos(meshData, floor, previousFloor);

            float y = meshData.CenterVertex.y;

            if (!meshData.IsFirstFloor(floor) || meshData.GenData.HeightBetweenCenterAndFirstFloor)
            {
                y = previousFloor.AnchorPos.y + randomPos.y;
            }

            floor.AnchorPos = new Vector3(randomPos.x, y, randomPos.z);
        }

        private static void SetFloorRadius(MeshData meshData, Floor floor, float tempRadius)
        {
            float t = (float)floor.DepthIndex / meshData.Floors.Count;
            float swellingValue = meshData.GenData.SwellingAlongVerticalAxis.Evaluate(t);
            swellingValue *= meshData.GenData.SwellingImpact;

            float randomSwelling = 0;

            if (SRnd.RangeInt(0, 100) <= meshData.GenData.RandomSwellingChance)
            {
                randomSwelling = SRnd.RangeFloat(meshData.GenData.MinRandomSwelling,
                    meshData.GenData.MaxRandomSwelling);
            }

            float radius = tempRadius * swellingValue + randomSwelling;
            floor.Radius = radius;
        }

        private static Vector3 ComputeFloorPos(MeshData meshData, Floor currentFloor, Floor previousFloor)
        {
            float height = SRnd.RangeFloat(meshData.GenData.MinFloorsHeight, meshData.GenData.MaxFloorsHeight);

            float t = previousFloor.AnchorPos.y / meshData.Floors.Count * meshData.GenData.MaxFloorsHeight;
            float randomOffsetValue = meshData.GenData.FloorsOffsetAlongVerticalAxis.Evaluate(t);

            float xOffset = 0;
            float zOffset = 0;

            if (currentFloor.IndexInBranch == 0)
            {
                randomOffsetValue *= meshData.GenData.FloorsOffesetImpact;

                xOffset = previousFloor.AnchorPos.x;
                zOffset = previousFloor.AnchorPos.z;
            }
            else
            {
                if (currentFloor.BranchIndex != 0)
                {
                    Vector3 branchDir =
                        (previousFloor.AnchorPos - meshData.Floors[previousFloor.PreviousFloorIndex].AnchorPos)
                        .normalized;

                    xOffset = previousFloor.AnchorPos.x + branchDir.x * meshData.GenData.BranchDirMultiplicator;
                    zOffset = previousFloor.AnchorPos.z + branchDir.z * meshData.GenData.BranchDirMultiplicator;
                }
            }

            if (currentFloor.IndexInBranch == 0)
            {
                xOffset += SRnd.RangeFloat(-randomOffsetValue, randomOffsetValue);
                zOffset += SRnd.RangeFloat(-randomOffsetValue, randomOffsetValue);
            }

            return new Vector3(xOffset, height, zOffset);
        }

        #endregion

        #region Verticies

        protected static void CreateVertices(MeshData meshData)
        {
            foreach (var floor in meshData.Floors)
            {
                for (int i = 0; i < meshData.GenData.MeshComplexity; i++)
                {
                    floor.Vertices.Add(GetVertexPos(meshData, floor, i));
                }
            }
        }

        private static Vector3 GetVertexPos(MeshData meshData, Floor floor, int index)
        {
            float angle = index * 2 * Mathf.PI / meshData.GenData.MeshComplexity;

            float x = floor.AnchorPos.x + floor.Radius * Mathf.Cos(angle) +
                      SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset);

            float y = meshData.IsFirstFloor(floor)
                ? meshData.Floors[0].AnchorPos.y
                : floor.AnchorPos.y + SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset / floor.Index);

            float z = floor.AnchorPos.z + floor.Radius * Mathf.Sin(angle) +
                      SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset);

            return new Vector3(x, y, z);
        }

        #endregion

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

            allVertices.Add(data.CenterVertex);
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