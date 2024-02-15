using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// throttle - gaz
// brake - fren
// steer - direksiyon

public class Car : MonoBehaviour
{
    public Axle[] axles;
    public float motorTorque = 1200f;
    public float reverseMotorTorque = 300f;
    public float maxSteerAngle = 30f;
    public float brakeTorque = 5000f;
    public Transform COM;
    private Rigidbody rigidbody;
    public Transform prefabTyreVisual;
    public float forwardSpeed = 0f;
    public float backwardSpeed = 0f;
    public float maxSpeed = 36f; // 36.0f * 3.6f = 130 kmh
    public float maxReverseSpeed = 6f; // 6.0f  * 3.6f = 20 kmh

    [Header("RPM Values")]
    public float minRPM = 1000f;
    public float maxRPM = 5000f;
    public float currentRPM = 1000f;
    public float rpmRevDuration = 3f;
    public float rpmDampingRatio = 2f;
    public AudioSource engineAudio;
    public float minPitch = 0.5f;
    public float maxPitch = 1.3f;
    public AnimationCurve rpmCurve;

    public Image needle;
    public Vector3 baseNeedleRot = new Vector3(0f, 0f, 207f);
    public Vector3 topNeedleRot = new Vector3(0f, 0f, 7f);

    public Image fillNitro;
    public int nitroInput;
    public float currentNitro = 100f;
    public float maxNitro = 100f;
    public float nitroDuration = 5f;
    public float nitroRegenerationRatio = 10f;
    public float nitroStrength = 6f;
    public AudioSource nitroSFX;
    public GameObject nitroParticleFXParent;

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
        nitroInput = Input.GetKey(KeyCode.LeftShift) == true ? 1 : 0;
        float steerInput = Input.GetAxis("Horizontal");

        forwardSpeed = Vector3.Dot(transform.forward, rigidbody.velocity);
        backwardSpeed = Vector3.Dot(transform.forward * -1, rigidbody.velocity);

        float currentTorque = GetRequiredTorque(forwardSpeed, maxSpeed, motorTorque);

        if (forwardSpeed < 0.001f && brakeInput > 0)
        {
            throttleInput = -1;
            brakeInput = 0;
            currentTorque = GetRequiredTorque(backwardSpeed, maxReverseSpeed, reverseMotorTorque);
        }

        for (int i = 0; i < axles.Length; i++)
        {
            var axle = axles[i];

            if (axle.applyTorque)
            {
                axle.left.motorTorque = throttleInput * currentTorque;
                axle.right.motorTorque = throttleInput * currentTorque;
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

        // Simulate Engine RPM and Play Audio

        float speedFactor = Mathf.InverseLerp(0, maxSpeed, forwardSpeed);
        float newRPMDuration = rpmRevDuration + speedFactor * 3f;

        // apply damping
        float rpmDamping = throttleInput == 0 ? rpmDampingRatio : 0f;
        currentRPM = Mathf.Lerp(currentRPM, minRPM, rpmDamping * Time.deltaTime);

        currentRPM = Mathf.MoveTowards(currentRPM, maxRPM, (Time.deltaTime / newRPMDuration) * maxRPM * Mathf.Abs(throttleInput));

        if (currentRPM >= maxRPM - 10 && speedFactor < 0.85f)
            currentRPM = minRPM;

        float rpmFactor = Mathf.InverseLerp(minRPM, maxRPM, currentRPM);
        float curvedRPMFactor = rpmCurve.Evaluate(rpmFactor);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, curvedRPMFactor);
        engineAudio.pitch = Mathf.MoveTowards(engineAudio.pitch, targetPitch, (Time.deltaTime / newRPMDuration) * maxPitch);

        // update needle visuals
        var targetNeedleRot = Vector3.Lerp(baseNeedleRot, topNeedleRot, curvedRPMFactor);
        var currentNeedleRot = needle.transform.localRotation.eulerAngles;
        needle.transform.localRotation = Quaternion.Euler(Vector3.Lerp(currentNeedleRot, targetNeedleRot, Time.deltaTime * newRPMDuration));


        // nitro

        currentNitro += Time.deltaTime * nitroRegenerationRatio;

        if (nitroInput > 0)
        {
            float nitroUsagePerSec = (maxNitro / nitroDuration);
            currentNitro -= nitroUsagePerSec * Time.deltaTime;

            nitroParticleFXParent.SetActive(true);

            if (!nitroSFX.isPlaying)
                nitroSFX.PlayDelayed(.1f);
        }
        else
        {
            nitroSFX.Stop();
            nitroParticleFXParent.SetActive(false);
        }

        fillNitro.fillAmount = currentNitro / maxNitro;
    }

    private void FixedUpdate()
    {
        if (nitroInput > 0 && currentNitro > 0f)
        {
            rigidbody.AddRelativeForce(Vector3.forward * nitroStrength, ForceMode.Acceleration);
        }
    }


    public float GetRequiredTorque(float speed, float maxSpeed, float maxMotorTorque)
    {
        float speedFactor = Mathf.InverseLerp(0f, maxSpeed, speed);
        float requiredMotorTorque = Mathf.Lerp(maxMotorTorque, 0, speedFactor * speedFactor);

        return requiredMotorTorque;
    }

    private void UpdateWheelVisual(WheelCollider wheelCollider)
    {
        Vector3 pos;
        Quaternion rot;

        wheelCollider.GetWorldPose(out pos, out rot);

        var wheelVisual = wheelCollider.transform.GetChild(0);

        Quaternion rotOffset = Quaternion.identity;

        if (wheelCollider.transform.localPosition.x < 0) // sol teker
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
