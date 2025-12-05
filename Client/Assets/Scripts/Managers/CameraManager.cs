using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [Header("Scanned Virtual Cameras")]
    private Dictionary<string, CinemachineVirtualCamera> virtualCameras = new Dictionary<string, CinemachineVirtualCamera>();

    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineBrain brain;
    private string playerFollowCameraName = "VCam_PlayerFollow";

    private Transform playerTransform;
    private PolygonCollider2D currentBounds;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        mainCamera = Camera.main;
        brain = mainCamera.GetComponent<CinemachineBrain>();
    }


    /// <summary>
    /// use this Function
    /// when Camera Object is Awake On Load
    /// </summary>
    /// <param name="camera"></param>
    public void RegisterCamera(CinemachineVirtualCamera vcam)
    {
        if (vcam == null)
        {
            return;
        }

        string key = vcam.name;

        if (virtualCameras.ContainsKey(key))
        {
            virtualCameras[key] = vcam;
        }
        else
        {
            virtualCameras.Add(key, vcam);
        }

        if (playerTransform != null && key == playerFollowCameraName)
        {
            vcam.Follow = playerTransform;
            vcam.LookAt = playerTransform;
        }

        if (currentBounds != null)
        {
            ApplyBoundsToCamera(vcam);
        }
    }

    /// <summary>
    /// use this Function
    /// when Camera Object has been Destroyed
    /// </summary>
    /// <param name="camera"></param>
    public void UnregisterCamera(CinemachineVirtualCamera vcam)
    {
        if (vcam == null)
        {
            return;
        }

        string key = vcam.name;

        if (virtualCameras.ContainsKey(key))
        {
            virtualCameras.Remove(key);
        }
    }

    public CinemachineVirtualCamera GetCamera(string name)
    {
        CinemachineVirtualCamera result;

        if (virtualCameras.TryGetValue(name, out result))
        {
            return result;
        }

        return null;
    }

    public void SetPlayerFollow(Transform target)
    {
        playerTransform = target;

        if (playerTransform == null)
        {
            return;
        }

        CinemachineVirtualCamera vcam = GetCamera(playerFollowCameraName);

        if (vcam == null)
        {
            return;
        }

        vcam.Follow = playerTransform;
        vcam.LookAt = playerTransform;
    }

    public void SetBounds(PolygonCollider2D bounds)
    {
        currentBounds = bounds;

        if (currentBounds == null)
        {
            return;
        }

        foreach (KeyValuePair<string, CinemachineVirtualCamera> pair in virtualCameras)
        {
            CinemachineVirtualCamera vcam = pair.Value;

            if (vcam == null)
            {
                continue;
            }

            ApplyBoundsToCamera(vcam);
        }
    }

    private void ApplyBoundsToCamera(CinemachineVirtualCamera vcam)
    {
        CinemachineConfiner2D confiner = vcam.GetComponent<CinemachineConfiner2D>();

        if (confiner == null)
        {
            return;
        }

        confiner.m_BoundingShape2D = currentBounds;
    }
}
