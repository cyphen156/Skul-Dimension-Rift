using Assets.Scripts.Data;
using Assets.Scripts.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PromptWidget : MonoBehaviour, IWidget
{
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image promptImage;
    [SerializeField] private Sprite interactionSprite;
    [SerializeField] private bool isInteracting;

    private void Awake()
    {
        promptText = GetComponentInChildren<TMP_Text>(includeInactive:true);

        Image[] imgs = GetComponentsInChildren<Image>(includeInactive:true);
        foreach (Image img in imgs)
        {
            if (img.gameObject.name == "PromptImage")
            {
                promptImage = img;
                break;
            }
        }

        if (promptImage != null)
        {
            interactionSprite = ResourceManager.instance.GetControlSprite("Interaction", false);
            promptImage.sprite = interactionSprite;
        }
    }

    private void OnEnable()
    {
        // Set the interaction sprite when the widget is enabled
        // check if the sprite needs to be updated
        isInteracting = false;

        Refresh("");
    }

    private void OnDisable()
    {
        isInteracting = false;
    }


    public void SetPrompt(PromptType type)
    {
        switch(type)
        {
            case PromptType.Enter:
                promptText.text = "들어가기";
                break;
            case PromptType.Interact:
                promptText.text = "대화하기";
                break;
            case PromptType.blessing:
                promptText.text = "축복받기";
                break;
            default:
                promptText.text = string.Empty;
                break;
        }
    }

    public void SetInteracting(bool flag)
    {
        if (isInteracting == flag)
        {
            return;
        }

        isInteracting = flag;

        Refresh(null);
    }

    public void Refresh(string data)
    {
        // 유저가 상호작용 중일 때
        // 이미지 스프라이트를 하이라이트로 변경
        if (data != null && promptText != null)
        {
            promptText.text = data;
        }

        if (promptImage == null)
        {
            return;
        }

        Sprite newSprite = ResourceManager.instance.GetControlSprite("Interaction", isInteracting);

        if (promptImage.sprite != newSprite)
        {
            promptImage.sprite = newSprite;
        }
    }
}
