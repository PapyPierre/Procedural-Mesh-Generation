using UnityEngine;

namespace Island_Generation
{
    [CreateAssetMenu(fileName = "IslandGenerationData", menuName = "ScriptableObjects/IslandGenerationData", order = 1)]
    public class IslandGenerationData : ScriptableObject
    {
        [field: SerializeField] public Material Material { get; private set; }

        [field: Header("Floors parameters"), SerializeField, Tooltip("minimal number of floors")]
        internal int MinFloorCount { get; private set; }

        [field: SerializeField, Tooltip("minimal number of floors")]
        internal int MaxFloorCount { get; private set; }

        [field: SerializeField, Space, Tooltip("minimal space between floors")]
        internal float MinFloorsHeight { get; private set; }

        [field: SerializeField, Tooltip("maximal space between floors")]
        internal float MaxFloorsHeight { get; private set; }

        [field: SerializeField, Space] internal float MinFloorsRadius { get; private set; }
        [field: SerializeField] internal float MaxFloorsRadius { get; private set; }

        [field: SerializeField, Space, Tooltip("maximal floors offset on x and z axis")]
        internal float FloorsMaxOffset { get; private set; }

        [field: SerializeField, Space, Tooltip("maximal vertices offset on all axis")]
        internal float VerticesMaxOffset { get; private set; }

        [field: SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
        internal float MinUpwardSwelling { get; private set; }

        [field: SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
        internal float MaxUpwardSwelling { get; private set; }

        [field: SerializeField, Tooltip("Number of vertices per floor")]
        internal int MeshComplexity { get; private set; }
    }
}