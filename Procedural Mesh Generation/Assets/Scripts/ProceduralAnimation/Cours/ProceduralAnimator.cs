using UnityEngine;

namespace ProcudralAnimation
{
    public class ProceduralAnimator : MonoBehaviour
    {
        [SerializeField] KeyCode upKey = KeyCode.UpArrow;
        [SerializeField] KeyCode downKey = KeyCode.DownArrow;
        [SerializeField] KeyCode rightKey = KeyCode.RightArrow;
        [SerializeField] KeyCode leftKey = KeyCode.LeftArrow;

        [SerializeField] float speed = 20f;
        [SerializeField] float distanceFormGround = 10f;
        [SerializeField] float maxDistanceFormGround = 100f;
        [SerializeField] LayerMask groundMask;
        [SerializeField] float positionLerpSpeed = 10f;
        [SerializeField] int feedCount = 7;
        [SerializeField] float feetMaxRadius = 15f;
        [SerializeField] GameObject footPrefab;
        [SerializeField] Transform feetGroup;
        [SerializeField] int legSegmentCount = 30;
        [SerializeField] float randomNewFootPositionRadius = 4f;
        [SerializeField] float footStepSpeed = 5f;
        [SerializeField] float curveCoreDecal = -3f;
        [SerializeField] float curveFootDecal = 10f;

        Foot[] feet;


        void Start()
        {
            transformWantedPosition.x = transform.position.x;
            transformWantedPosition.z = transform.position.z;

            InitFeet();
        }

        void Update()
        {
            Move();
            MoveTooFarFeet();
            UpdateFeet();
        }


        Vector3 horizontalDisplacement;
        Vector3 transformWantedPosition;

        void Move()
        {
            horizontalDisplacement = Vector3.zero;

            if (Input.GetKey(upKey))
            {
                horizontalDisplacement += Vector3.forward;
            }

            if (Input.GetKey(downKey))
            {
                horizontalDisplacement += Vector3.back;
            }

            if (Input.GetKey(rightKey))
            {
                horizontalDisplacement += Vector3.right;
            }

            if (Input.GetKey(leftKey))
            {
                horizontalDisplacement += Vector3.left;
            }

            horizontalDisplacement = horizontalDisplacement.normalized;

            if (Physics.Raycast(transformWantedPosition + horizontalDisplacement * speed * Time.deltaTime +
                                Vector3.up * maxDistanceFormGround * 0.5f, Vector3.down, out RaycastHit hit,
                    maxDistanceFormGround, groundMask))
            {
                Debug.DrawRay(hit.point, Vector3.up, Color.red);

                transformWantedPosition = hit.point + Vector3.up * distanceFormGround;
            }

            transform.position =
                Vector3.Lerp(transform.position, transformWantedPosition, positionLerpSpeed * Time.deltaTime);
        }


        void MoveTooFarFeet()
        {
            for (int i = 0; i < feedCount; i++)
            {
                if (feet[i] != null)
                {
                    if (feet[i].isMovingToCore == false)
                    {
                        Vector3 pos = feet[i].transform.position - transform.position;
                        pos.y = 0;

                        if (pos.magnitude > feetMaxRadius)
                        {
                            ReturnFootOrder(feet[i]);
                        }
                    }
                }
            }
        }

        void ReturnFootOrder(Foot foot)
        {
            Vector3 difVector = transform.position - foot.transform.position;
            difVector.y = 0;

            Vector3 randomPosition = Random.insideUnitCircle * randomNewFootPositionRadius;
            randomPosition.z = randomPosition.y;
            randomPosition.y = 0;

            Vector3 pos = transform.position + difVector.normalized * (feetMaxRadius - randomNewFootPositionRadius) +
                          randomPosition;

            if (Physics.Raycast(pos + Vector3.up * maxDistanceFormGround * 0.5f, Vector3.down, out RaycastHit hit,
                    maxDistanceFormGround, groundMask))
            {
                foot.haveTarget = true;
                foot.isMovingToCore = true;
                foot.isMovingToTarget = false;
                foot.step = 0;

                foot.previousPosition = foot.transform.position;
                foot.targetPosition = hit.point;

                Debug.DrawRay(hit.point, Vector3.up, Color.red, 1);
            }
            else
            {
                foot.haveTarget = false;
            }
        }

        Foot tempFoot;

        void UpdateFeet()
        {
            for (int i = 0; i < feedCount; i++)
            {
                if (feet[i] != null)
                {
                    tempFoot = feet[i];

                    if (tempFoot.isMovingToCore)
                    {
                        tempFoot.step += footStepSpeed * Time.deltaTime;

                        tempFoot.transform.position =
                            Vector3.Lerp(tempFoot.previousPosition, transform.position, tempFoot.step);

                        if (tempFoot.step >= 1)
                        {
                            tempFoot.isMovingToCore = false;
                            tempFoot.step = 0;
                            tempFoot.previousPosition = tempFoot.transform.position;

                            if (tempFoot.haveTarget)
                            {
                                tempFoot.isMovingToTarget = true;
                            }
                        }
                    }
                    else if (tempFoot.isMovingToTarget)
                    {
                        tempFoot.step += footStepSpeed * Time.deltaTime;

                        tempFoot.transform.position =
                            Vector3.Lerp(tempFoot.previousPosition, tempFoot.targetPosition, tempFoot.step);

                        if (tempFoot.step >= 1)
                        {
                            tempFoot.previousPosition = tempFoot.transform.position;
                            tempFoot.step = 0;
                            tempFoot.isMovingToTarget = false;
                        }
                    }

                    RefreshCurve(tempFoot);
                }
            }
        }

        void InitFeet()
        {
            Vector3 pos;

            feet = new Foot[feedCount];

            for (int i = 0; i < feedCount; i++)
            {
                pos = Random.insideUnitCircle * feetMaxRadius;
                pos.z = pos.y;
                pos.y = 0;

                pos += transform.position;

                TryCreateNewFoot(i, pos);
            }
        }

        void TryCreateNewFoot(int index, Vector3 pos)
        {
            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, maxDistanceFormGround, groundMask))
            {
                Debug.DrawRay(hit.point, Vector3.up * 0.2f, Color.cyan, 2f);

                CreateFoot(index, hit.point);
            }
        }


        GameObject footInstance;

        void CreateFoot(int index, Vector3 footPosition)
        {
            footInstance = Instantiate(footPrefab, footPosition, Quaternion.identity, feetGroup);

            footInstance.GetComponent<LineRenderer>().positionCount = legSegmentCount;

            feet[index] = new Foot
            {
                transform = footInstance.transform,
                lineRenderer = footInstance.GetComponent<LineRenderer>(),
                targetPosition = footPosition,
                previousPosition = footPosition,
                step = 1,
            };
        }

        Vector3 p0;
        Vector3 p1;
        Vector3 p2;
        Vector3 p3;

        BezierCurve curve;

        void RefreshCurve(Foot foot)
        {
            p0 = transform.position;
            p1 = transform.position + Vector3.up * curveCoreDecal;
            p2 = foot.transform.position + Vector3.up * curveFootDecal;
            p3 = foot.transform.position;

            curve = new BezierCurve(p0, p1, p2, p3);

            for (int i = 0; i < legSegmentCount; i++)
            {
                Vector3 pos = curve.Evaluate((float)i / (legSegmentCount - 1));

                foot.lineRenderer.SetPosition(i, pos);
            }
        }
    }

    public class Foot
    {
        public Transform transform;
        public LineRenderer lineRenderer;

        public Vector3 targetPosition;
        public Vector3 previousPosition;
        public float step;

        public bool isMovingToCore;
        public bool isMovingToTarget;

        public bool haveTarget;
    }

    class BezierCurve
    {
        Vector3[] controlPoints;

        public BezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            controlPoints = new[] { p0, p1, p2, p3 };
        }

        float t2;
        float t3;
        float u;
        float u2;
        float u3;
        Vector3 position;

        public Vector3 Evaluate(float t)
        {
            t2 = t * t;
            t3 = t2 * t;

            u = 1 - t;
            u2 = u * u;
            u3 = u2 * u;

            position = u3 * controlPoints[0] +
                       3 * u2 * t * controlPoints[1] +
                       3 * u * t2 * controlPoints[2] +
                       t3 * controlPoints[3];

            return position;
        }
    }
}