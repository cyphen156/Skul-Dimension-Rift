using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class VirtualCamera : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera vcam;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        if (CameraManager.instance != null)
        {
            CameraManager.instance.RegisterCamera(vcam);
        }
    }

    private void OnDestroy()
    {
        if (CameraManager.instance != null)
        {
            CameraManager.instance.UnregisterCamera(vcam);
        }
    }
}
