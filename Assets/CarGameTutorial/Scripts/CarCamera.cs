using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarCamera : MonoBehaviour
{
    public Transform lookAt;
    public Transform camPosition;
    public float CameraStiffness;

    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, camPosition.position, Time.deltaTime * CameraStiffness);
        transform.LookAt(lookAt);
    }
}
