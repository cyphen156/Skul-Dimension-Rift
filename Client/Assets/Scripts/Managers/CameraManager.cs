using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [Header("Scanned Virtual Cameras")]
    private Dictionary<string, CinemachineCamera> virtualCameras = new Dictionary<string, CinemachineCamera>();

    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private string playerFollowCameraName = "VCam_PlayerFollow";

    [SerializeField] private Transform playerTransform;
    [SerializeField] private PolygonCollider2D currentBounds;

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
    public void RegisterCamera(CinemachineCamera vcam)
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
    public void UnregisterCamera(CinemachineCamera vcam)
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

    public CinemachineCamera GetCamera(string name)
    {
        CinemachineCamera result;

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

        CinemachineCamera vcam = GetCamera(playerFollowCameraName);

        if (vcam == null)
        {
            return;
        }

        vcam.Follow = playerTransform;
        vcam.LookAt = playerTransform;
    }

    public void ClearPlayerFollow()
    {
        playerTransform = null;

        CinemachineCamera vcam = GetCamera(playerFollowCameraName);

        if (vcam == null)
        {
            return;
        }

        vcam.Follow = null;
        vcam.LookAt = null;
    }

    public void SetBounds(PolygonCollider2D bounds)
    {
        currentBounds = bounds;

        if (currentBounds == null)
        {
            return;
        }

        foreach (KeyValuePair<string, CinemachineCamera> pair in virtualCameras)
        {
            CinemachineCamera vcam = pair.Value;

            if (vcam == null)
            {
                continue;
            }

            ApplyBoundsToCamera(vcam);
        }
    }

    private void ApplyBoundsToCamera(CinemachineCamera vcam)
    {
        CinemachineConfiner2D confiner = vcam.GetComponent<CinemachineConfiner2D>();

        if (confiner == null)
        {
            return;
        }

        confiner.BoundingShape2D = currentBounds;
        confiner.InvalidateBoundingShapeCache();
    }
}
