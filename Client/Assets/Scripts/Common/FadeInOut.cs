using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 스프라이트 또는 텍스트를 서서히 나타나게/사라지게 하는 스크립트
/// </summary>
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

    private bool useChildren = false;
    private readonly List<SpriteRenderer> childSprites = new List<SpriteRenderer>();
    private readonly List<TMP_Text> childTmps = new List<TMP_Text>();
    private readonly List<Color> childSpriteBaseColors = new List<Color>();
    private readonly List<Color> childTmpBaseColors = new List<Color>();

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
            var srs = GetComponentsInChildren<SpriteRenderer>(true);
            var tmps = GetComponentsInChildren<TMP_Text>(true);

            if ((srs != null && srs.Length > 0) || (tmps != null && tmps.Length > 0))
            {
                useChildren = true;

                if (srs != null)
                {
                    foreach (var sr in srs)
                    {
                        if (sr == null)
                        {
                            continue;
                        }
                        childSprites.Add(sr);
                        var c = sr.color; c.a = 1f; // 알파는 항상 덮어씀
                        childSpriteBaseColors.Add(c);
                    }
                }

                if (tmps != null)
                {
                    foreach (var t in tmps)
                    {
                        if (t == null)
                        {
                            continue;
                        }
                        childTmps.Add(t);
                        var c = t.color; c.a = 1f;
                        childTmpBaseColors.Add(c);
                    }
                }
            }
            else
            {
                Debug.LogError("FadeInOut: No SpriteRenderer or TMP_Text found on this object or its children.");
            }
        }
    }

    private void OnEnable()
    {
        currentTime = 0f;
        isFadeIn = true;
        delayPassed = false;
        ApplyAlpha(0f);
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
            {
                return;
            }

            // delay 끝나자마자 초기화
            delayPassed = true;
            currentTime = 0f;
        }

        float duration = isFadeIn ? fadeInTime : fadeOutTime;
        float t = duration > 0f ? Mathf.Clamp01(currentTime / duration) : 1f;
        float alpha = isFadeIn ? t : (1f - t);

        Color c = originalColor;
        c.a = alpha;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = c;
        }
        else if (tmpText != null)
        {
            tmpText.color = c;
        }

        ApplyAlpha(alpha);

        if (currentTime >= duration)
        {
            currentTime = 0f;
            isFadeIn = !isFadeIn;
        }
    }
    private void ApplyAlpha(float a)
    {
        if (!useChildren)
        {
            if (spriteRenderer != null)
            {
                var c = originalColor; c.a = a;
                spriteRenderer.color = c;
            }
            else if (tmpText != null)
            {
                var c = originalColor; c.a = a;
                tmpText.color = c;
            }
            return;
        }

        // 자식들 전체 적용
        for (int i = 0; i < childSprites.Count; i++)
        {
            var sr = childSprites[i];
            if (sr == null)
            {
                continue;
            }
            var c = childSpriteBaseColors[i];
            c.a = a;
            sr.color = c;
        }

        for (int i = 0; i < childTmps.Count; i++)
        {
            var t = childTmps[i];
            if (t == null)
            {
                continue;
            }
            var c = childTmpBaseColors[i]; c.a = a;
            t.color = c;
        }
    }
}