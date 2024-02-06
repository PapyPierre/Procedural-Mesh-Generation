using System;
using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    public float rotationSpeed = 2f;
    public float distanceFromCenter = 10f;

    private void Update()
    {
        Vector3 currentPosition = transform.position;

        float rotationAngle = rotationSpeed * Time.deltaTime;

        Quaternion rotation = Quaternion.AngleAxis(rotationAngle, Vector3.up);

        Vector3 newPosition = rotation * currentPosition;

        newPosition = newPosition.normalized * distanceFromCenter;

        transform.position = newPosition;

        transform.LookAt(Vector3.zero);
    }
}