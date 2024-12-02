using UnityEngine;

namespace Procedural_Mesh_Generation
{
    public class MeshGenerationData : ScriptableObject
    {
        [field: SerializeField] 
        public string ObjName { get; private set; }
        
        [field: SerializeField] 
        public string MeshName { get; private set; }
        
        [field: SerializeField] 
        public Material Material { get; private set; }
        
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

        [field: Header("Vertices"), SerializeField, Tooltip("maximal vertices offset on all axis")]
        internal float VerticesMaxOffset { get; private set; }

        [field: Header("Swelling"), SerializeField, Tooltip("Impact floor radius depending on swelling direction.")]
        internal float MinDirectionSwelling { get; private set; }

        [field: SerializeField, Tooltip("Impact floor radius depending on swelling direction.")]
        internal float MaxDirectionSwelling { get; private set; }

        [field: SerializeField, Tooltip("If the mesh should generation himself upward or downward")]
        internal GenerationDirection SwellingDirection { get; private set; }
        
        [field: SerializeField, Range(0,100), Tooltip("% of chance to apply random Swelling on each floor")]
        internal float RandomSwellingChance { get; private set; }

        [field: SerializeField, Tooltip("Random Swelling that can be added at any floor.")]
        internal float MinRandomSwelling { get; private set; }
        
        [field: SerializeField, Tooltip("Random Swelling that can be added at any floor.")]
        internal float MaxRandomSwelling { get; private set; }
        
        [field: SerializeField, Tooltip("Number of vertices per floor")]
        internal int MeshComplexity { get; private set; }

        [field: SerializeField]
        internal int MaxNumberOfBranches { get; private set; }
    }

    public enum GenerationDirection
    {
        Upward,
        Downward
    }
}