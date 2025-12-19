using UnityEngine;

public class KeyboardPosePreview : MonoBehaviour
{
    [Header("Assign bones (drag from PosePreview skeleton)")]
    public Transform leftUpperArm;
    public Transform leftLowerArm;
    public Transform rightUpperArm;
    public Transform rightLowerArm;
    public Transform spine;      // chest / spine
    public Transform hips;       // pelvis

    [Header("Tuning")]
    public float angleSpeedDeg = 90f; // degrees per second

    void Update()
    {
        float dt = Time.deltaTime;

        // ---- Right arm (I/K = upper arm pitch, O/L = lower arm pitch)
        if (rightUpperArm != null)
        {
            if (Input.GetKey(KeyCode.I)) rightUpperArm.Rotate(Vector3.right, -angleSpeedDeg * dt, Space.Self);
            if (Input.GetKey(KeyCode.K)) rightUpperArm.Rotate(Vector3.right,  angleSpeedDeg * dt, Space.Self);
        }
        if (rightLowerArm != null)
        {
            if (Input.GetKey(KeyCode.O)) rightLowerArm.Rotate(Vector3.right, -angleSpeedDeg * dt, Space.Self);
            if (Input.GetKey(KeyCode.L)) rightLowerArm.Rotate(Vector3.right,  angleSpeedDeg * dt, Space.Self);
        }

        // ---- Left arm (W/S = upper arm pitch, E/D = lower arm pitch)
        if (leftUpperArm != null)
        {
            if (Input.GetKey(KeyCode.W)) leftUpperArm.Rotate(Vector3.right, -angleSpeedDeg * dt, Space.Self);
            if (Input.GetKey(KeyCode.S)) leftUpperArm.Rotate(Vector3.right,  angleSpeedDeg * dt, Space.Self);
        }
        if (leftLowerArm != null)
        {
            if (Input.GetKey(KeyCode.E)) leftLowerArm.Rotate(Vector3.right, -angleSpeedDeg * dt, Space.Self);
            if (Input.GetKey(KeyCode.D)) leftLowerArm.Rotate(Vector3.right,  angleSpeedDeg * dt, Space.Self);
        }

        // ---- Body (A/F = spine bend, Q/R = hips yaw)
        if (spine != null)
        {
            if (Input.GetKey(KeyCode.A)) spine.Rotate(Vector3.right, -angleSpeedDeg * dt, Space.Self);
            if (Input.GetKey(KeyCode.F)) spine.Rotate(Vector3.right,  angleSpeedDeg * dt, Space.Self);
        }
        if (hips != null)
        {
            if (Input.GetKey(KeyCode.Q)) hips.Rotate(Vector3.up, -angleSpeedDeg * dt, Space.Self);
            if (Input.GetKey(KeyCode.R)) hips.Rotate(Vector3.up,  angleSpeedDeg * dt, Space.Self);
        }

        // ---- Reset (Backspace)
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ResetLocalRot(leftUpperArm);
            ResetLocalRot(leftLowerArm);
            ResetLocalRot(rightUpperArm);
            ResetLocalRot(rightLowerArm);
            ResetLocalRot(spine);
            ResetLocalRot(hips);
        }
    }

    void ResetLocalRot(Transform t)
    {
        if (t != null) t.localRotation = Quaternion.identity;
    }
}