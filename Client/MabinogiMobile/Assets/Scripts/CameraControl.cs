using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    public float RotateSpeed { get; set; } = 10.0f;
    public float TargetDistance { get; set; } = 15.0f;

    Transform TargetTransform = null!;
    bool IsRotating = false;
    float yaw = 0.0f;
    float pitch = 0.0f;

    void OnEnable()
    {
        Character character = GetComponentInParent<Character>();
        if (character == null)
        {
            Debug.Log("character componenet is not find");
            return;
        }

        TargetTransform = character.GetComponent<Transform>();
        if (TargetTransform == null)
        {
            Debug.Log("transform componenet is not find");
            return;
        }
    }

    public void SetCameraRotatingState(InputAction.CallbackContext context)
    {
        if (context.performed)
            IsRotating = true;
        else
            IsRotating = false;
    }

    public void RotateCamera(InputAction.CallbackContext context)
    {
        if (IsRotating)
        {
            Vector2 mouseDelta = context.ReadValue<Vector2>();
            if (mouseDelta != Vector2.zero)
            {
                yaw += mouseDelta.x * RotateSpeed * Time.deltaTime;
                pitch -= mouseDelta.y * RotateSpeed * Time.deltaTime;
                
                // 뒤집힘 방지
                pitch = Mathf.Clamp(pitch, 0.0f, 80.0f);

                // 이동 //
                // target에서 camera로 향하는 벡터를 rotation만큼 회전
                Vector3 TargetToCamera = Vector3.back * TargetDistance;
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0.0f);
                transform.localPosition = rotation * TargetToCamera;

                // 회전 //
                transform.LookAt(TargetTransform);
            }
        }
    }
}
