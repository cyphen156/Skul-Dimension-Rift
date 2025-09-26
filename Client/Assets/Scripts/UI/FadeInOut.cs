using UnityEngine;
using TMPro;

public class FadeInOut : MonoBehaviour
{
    public float fadeInTime = 2f;
    public float fadeOutTime = 2f;
    public float delayTime = 0f;
    private float currentTime;
    private bool isFadeIn = true;

    private SpriteRenderer spriteRenderer;
    private TMP_Text tmpText;
    private Color originalColor;

    public bool isLoop = true;
    public bool delayPassed = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tmpText = GetComponent<TMP_Text>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else if (tmpText != null)
        {
            originalColor = tmpText.color;
        }
        else
        {
            Debug.LogWarning("[FadeInOut] SpriteRenderer나 TMP_Text가 필요합니다.");
        }
    }

    private void OnEnable()
    {
        currentTime = 0f;
        isFadeIn = true;
        delayPassed = false;
    }

    private void Update()
    {
        if (!isLoop && !isFadeIn)
        {
            return;
        }
        currentTime += Time.deltaTime;

        if (!delayPassed)
        {
            if (currentTime < delayTime)
                return;

            // delay 끝나자마자 초기화
            delayPassed = true;
            currentTime = 0f;
        }

        float duration = isFadeIn ? fadeInTime : fadeOutTime;
        float alpha = isFadeIn ? currentTime / fadeInTime : 1f - (currentTime / fadeOutTime);
        alpha = Mathf.Clamp01(alpha);

        Color c = originalColor;
        c.a = alpha;

        if (spriteRenderer != null)
            spriteRenderer.color = c;
        else if (tmpText != null)
            tmpText.color = c;

        if (currentTime >= duration)
        {
            currentTime = 0f;
            isFadeIn = !isFadeIn;

            //if (!isLoop && !isFadeIn)
            //{
            //    enabled = false; 
            //}
        }
    }
}
