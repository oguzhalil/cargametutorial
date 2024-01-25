using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// throttle - gaz
// brake - fren
// steer - direksiyon

public class Car : MonoBehaviour
{
    public Axle[] axles;
    public float motorTorque = 1200f;
    public float maxSteerAngle = 30f;
    public float brakeTorque = 5000f;
    public Transform COM;
    private Rigidbody rigidbody;
    public Transform prefabTyreVisual;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.centerOfMass = COM.transform.localPosition;

        for (int i = 0; i < axles.Length; i++)
        {
            var axle = axles[i];

            Instantiate(prefabTyreVisual, axle.left.transform);
            Instantiate(prefabTyreVisual, axle.right.transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        int throttleInput = Input.GetKey(KeyCode.W) == true ? 1 : 0;
        int brakeInput = Input.GetKey(KeyCode.S) == true ? 1 : 0;
        int handbrakeInput = Input.GetKey(KeyCode.Space) == true ? 1 : 0;
        float steerInput = Input.GetAxis("Horizontal");

        for (int i = 0; i < axles.Length; i++)
        {
            var axle = axles[i];

            if (axle.applyTorque)
            {
                axle.left.motorTorque = throttleInput * motorTorque;
                axle.right.motorTorque = throttleInput * motorTorque;
            }

            if (axle.applySteering)
            {
                axle.left.steerAngle = steerInput * maxSteerAngle;
                axle.right.steerAngle = steerInput * maxSteerAngle;
            }

            axle.left.brakeTorque = brakeInput * brakeTorque;
            axle.right.brakeTorque = brakeInput * brakeTorque;

            if (axle.applyHandbrake && brakeInput == 0)
            {
                axle.left.brakeTorque = handbrakeInput * brakeTorque;
                axle.right.brakeTorque = handbrakeInput * brakeTorque;
            }

            UpdateWheelVisual(axle.left);
            UpdateWheelVisual(axle.right);

        }
    }

    private void UpdateWheelVisual(WheelCollider wheelCollider) 
    {
        Vector3 pos;
        Quaternion rot;

        wheelCollider.GetWorldPose(out pos, out rot);

        var wheelVisual = wheelCollider.transform.GetChild(0);

        Quaternion rotOffset = Quaternion.identity;

        if(wheelCollider.transform.localPosition.x < 0) // sol teker
        {
            rotOffset = Quaternion.Euler(0f, 180f, 0f);
        }

        wheelVisual.position = pos;
        wheelVisual.rotation = rot * rotOffset;
    }

    [System.Serializable]
    public class Axle
    {
        public WheelCollider left;
        public WheelCollider right;
        public bool applyTorque;
        public bool applySteering;
        public bool applyHandbrake;
    }
}
