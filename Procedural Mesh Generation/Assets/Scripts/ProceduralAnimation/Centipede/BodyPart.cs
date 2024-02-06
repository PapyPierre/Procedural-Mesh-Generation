using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProceduralAnimation.Centipede
{
    public class BodyPart : MonoBehaviour
    {
        private Centipede centipede;
        private int indexInBody;
        private Transform previousBodyPart;

        [SerializeField] private Leg[] legs;
        
        private void Awake()
        {
            centipede = GetComponentInParent<Centipede>();
            indexInBody = centipede.bodyParts.IndexOf(this);

            previousBodyPart = indexInBody == 0 ? centipede.head : centipede.bodyParts[indexInBody - 1].transform;
        }

        private void Update()
        {
            MoveBodyPart(previousBodyPart.position);
            UpdateLegsPositions();
        }
        
        private void MoveBodyPart(Vector3 targetPos)
        {
            if (Vector3.Distance(transform.position, targetPos) < centipede.minBodyPartSpacing) return;

            float step = Time.deltaTime * centipede.moveSpeed;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
            
            RotateBodypart(targetPos);
        }
        
        private void UpdateLegsPositions()
        {
            foreach (var leg in legs)
            {
                Vector3 feetPos = leg.Part3.position;
                Vector3 touchPosOnGround = new Vector3(feetPos.x, 0, feetPos.z);

                if (Vector3.Distance(touchPosOnGround, leg.target.position) > centipede.maxDistTargetToLeg)
                {
                    ResetLegTarget(leg);
                }
            }
        }

        private void ResetLegTarget(Leg leg)
        {
            leg.target.position =  Random.insideUnitCircle;
        }

        private void RotateBodypart(Vector3 target)
        {
            float rotStep = Time.deltaTime * centipede.angularSpeed;
            var rot = Quaternion.LookRotation(target - transform.position);

            transform.rotation = Quaternion.Lerp(transform.rotation, rot, rotStep);
        }
    }
}