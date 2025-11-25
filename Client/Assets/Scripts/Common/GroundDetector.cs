using System;
using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    public bool isGrounded;

    public Action<bool> isGroundedChanged;

    private void Awake()
    {
        isGrounded = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        SetIsGrounded(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        SetIsGrounded(false);
    }

    private void SetIsGrounded(bool grounded)
    {
        if (isGrounded != grounded)
        {
            isGrounded = grounded;
            isGroundedChanged?.Invoke(isGrounded);
        }
    }
}
