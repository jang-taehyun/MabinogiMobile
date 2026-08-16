using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotation : MonoBehaviour
{
    CinemachineInputAxisController RotationInputController = null!;

    void Start()
    {
        RotationInputController = GetComponent<CinemachineInputAxisController>();
        if (RotationInputController is null)
            Debug.Log("cinemachine input axis controller not find");
    }

    public void SetEnableCinemachineInputAxisController(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            RotationInputController.enabled = true;
        }
        else if(context.canceled)
        {
            RotationInputController.enabled = false;
        }
    }
}
