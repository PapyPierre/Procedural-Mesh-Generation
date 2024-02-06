using System;
using UnityEditor;
using UnityEngine;

namespace ProceduralAnimation.Centipede
{
    public class Leg : MonoBehaviour
    {
        [SerializeField] private Transform Part1;
        [SerializeField] private Transform Part2;
        [SerializeField] internal Transform Part3;

        internal Vector3 feetPos;

        [SerializeField, Space] internal Transform target;

        [SerializeField, Space] private Centipede centipede;

        private void Awake()
        {
            //TODO feetPos =
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(feetPos, Vector3.up, centipede.maxDistTargetToLeg, 2);
        }
#endif
    }
}