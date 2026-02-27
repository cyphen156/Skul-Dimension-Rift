using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingText : MonoBehaviour
{
    [SerializeField] private TMP_Text dotsText;
    private Coroutine _running;

    private static readonly string[] Dots =
    {
        "",
        ".",
        "..",
        "..."
    };

    private void Awake()
    {
        if (dotsText == null)
        {
            dotsText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
        }

        _running = StartCoroutine(C_Animate());
    }

    private void OnDisable()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        if (dotsText != null)
        {
            dotsText.text = "";
        }
    }

    private IEnumerator C_Animate()
    {
        int index = 0;

        while (true)
        {
            if (dotsText != null)
            {
                dotsText.text = Dots[index];
            }

            index++;

            if (index >= Dots.Length)
            {
                index = 0;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }
}
