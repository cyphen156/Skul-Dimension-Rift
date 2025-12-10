using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class VirtualCamera : MonoBehaviour
{
    [SerializeField] CinemachineCamera vcam;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
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
