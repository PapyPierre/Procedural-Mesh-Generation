using System.Collections;
using Island_Generation;
using UnityEngine;

// Example Class
public class IslandGeneratorCaller : MonoBehaviour
{
    [SerializeField] private IslandGenerationData generationData;
    [SerializeField] private Vector3 generateAt;
    
    private IEnumerator Start()
    {
        for (int i = 0; i < 100; i++)
        {
          GameObject lastIsland = IslandMeshGenerator.GenerateIsland(generateAt, generationData);
          yield return new WaitForSeconds(5);
          Destroy(lastIsland);
        }
    }
}