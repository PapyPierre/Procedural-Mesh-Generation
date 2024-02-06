using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralAnimation.Centipede
{
    public class Centipede : MonoBehaviour
    {
        [SerializeField] internal Transform head;
        [SerializeField] internal List<BodyPart> bodyParts;
        [SerializeField, Space] internal float minBodyPartSpacing;

        [SerializeField, Space] internal float moveSpeed;
        [SerializeField, Space] internal float angularSpeed;

        [SerializeField, Space] internal float maxDistTargetToLeg;

        private Camera camera;
        private Vector3 targetPos;

        private void Start()
        {
            camera = Camera.main;
        }

        private void Update()
        {
            SetTargetPos();
            MoveHead();
        }

        private void SetTargetPos()
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                
                if(Physics.Raycast(ray, out hitInfo, 1000))
                {
                    targetPos = hitInfo.point;
                    Debug.DrawRay(ray.origin, ray.direction * hitInfo.distance, Color.green, 3);
                }
            }
        }

        private void MoveHead()
        {
            if (targetPos == Vector3.zero) return;

            targetPos = new Vector3(targetPos.x, targetPos.y +1, targetPos.z);
            float step = Time.deltaTime * moveSpeed;
            head.transform.position = Vector3.MoveTowards(head.transform.position, targetPos, step);
            RotateHead();
        }

        private void RotateHead()
        {
            float rotStep = Time.deltaTime * angularSpeed;
            var rot = Quaternion.LookRotation(targetPos -  head.transform.position);

            head.transform.rotation = Quaternion.Lerp( head.transform.rotation, rot, rotStep);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPos, 0.5f);
        }
    }
}
