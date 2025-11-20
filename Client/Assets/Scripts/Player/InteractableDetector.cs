using Assets.Scripts.Interface;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableDetector : MonoBehaviour
{
    public event Action<IInteractable> OnTargetChanged;

    private List<IInteractable> detectedObjects = new List<IInteractable>();
    private IInteractable currentTarget;

#if UNITY_EDITOR
    [SerializeField] List<GameObject> debugDetectedObjects = new List<GameObject>();
    [SerializeField] GameObject debugCurrentTarget;
    private void Update()
    {
        debugDetectedObjects.Clear();
        foreach (var obj in detectedObjects)
        {
            MonoBehaviour mb = obj as MonoBehaviour;
            if (mb != null)
            {
                debugDetectedObjects.Add(mb.gameObject);
            }
        }
        if (currentTarget != null)
        {
            MonoBehaviour mb = currentTarget as MonoBehaviour;
            if (mb != null)
            {
                debugCurrentTarget = mb.gameObject;
            }
            else
            {
                debugCurrentTarget = null;
            }
        }
        else
        {
            debugCurrentTarget = null;
        }
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable;

        if (other.TryGetComponent<IInteractable>(out interactable))
        {
            if (!detectedObjects.Contains(interactable))
            {
                detectedObjects.Add(interactable);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable;

        if (other.TryGetComponent<IInteractable>(out interactable))
        {
            if (detectedObjects.Contains(interactable))
            {
                detectedObjects.Remove(interactable);
            }
        }
    }

    private void FixedUpdate()
    {
        UpdateCurrentTarget();
    }

    private void UpdateCurrentTarget()
    {
        IInteractable previousTarget = currentTarget;

        if (detectedObjects.Count == 0)
        {
            currentTarget = null;
        }
        else
        {
            float nearestDistance = float.MaxValue;
            IInteractable nearest = null;

            foreach (IInteractable interactable in detectedObjects)
            {
                MonoBehaviour mb = interactable as MonoBehaviour;

                if (mb == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, mb.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }

            currentTarget = nearest;
        }

        if (previousTarget != currentTarget)
        {
            if (OnTargetChanged != null)
            {
                OnTargetChanged(currentTarget);
            }
        }
    }
}
