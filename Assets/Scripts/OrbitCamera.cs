using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              // 追従したい中心（Groundの中心 or タワー中心）
    public Vector3 targetOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Distance")]
    public float distance = 12f;
    public float minDistance = 5f;
    public float maxDistance = 30f;
    public float zoomSpeed = 5f;

    [Header("Rotation")]
    public float yaw = 45f;               // 左右角度
    public float pitch = 35f;             // 上下角度
    public float minPitch = 10f;
    public float maxPitch = 80f;
    public float rotateSpeed = 180f;

    [Header("Pan (optional)")]
    public float panSpeed = 0.02f;        // 右クリック + Shiftでパン
    public bool allowPan = true;

    [Header("Controls")]
    public MouseButton rotateButton = MouseButton.Right;
    public bool invertY = false;

    Vector3 panOffset = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        UpdateCamera();
    }

    void HandleInput()
    {
        // 回転：右クリックドラッグ
        if (Input.GetMouseButton((int)rotateButton))
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            yaw += mx * rotateSpeed * Time.deltaTime;
            pitch += (invertY ? my : -my) * rotateSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // ズーム：ホイール
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0001f)
        {
            distance -= wheel * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        // パン：右クリック + Shift（任意）
        if (allowPan && Input.GetMouseButton((int)rotateButton) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            // カメラの右・上方向にオフセット
            panOffset += (-transform.right * mx + -transform.up * my) * (distance * panSpeed);
        }

        // リセット：R
        if (Input.GetKeyDown(KeyCode.R))
        {
            panOffset = Vector3.zero;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void UpdateCamera()
    {
        Vector3 center = target.position + targetOffset + panOffset;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pos = center + rot * new Vector3(0f, 0f, -distance);

        transform.position = pos;
        transform.LookAt(center);
    }
        // ★ UI用：カメラ回転
    public void RotateCamera(float yawDelta, float pitchDelta)
    {
        yaw += yawDelta;
        pitch += pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    // ★ UI用：ズーム
    public void ZoomCamera(float delta)
    {
        distance -= delta;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    // ★ UI用：パン
    public void PanCamera(Vector2 dir)
    {
        panOffset += (-transform.right * dir.x + -transform.up * dir.y) * (distance * panSpeed);
    }

    public enum MouseButton { Left = 0, Right = 1, Middle = 2 }
}