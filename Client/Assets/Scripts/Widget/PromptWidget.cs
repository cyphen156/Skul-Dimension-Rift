using Assets.Scripts.Data;
using Assets.Scripts.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PromptWidget : MonoBehaviour, IWidget
{
    private TextMeshPro promptText;
    private Sprite interactionSprite;

    private void Awake()
    {
        promptText = GetComponentInChildren<TextMeshPro>(includeInactive:true);

        Image[] imgs = GetComponentsInChildren<Image>(includeInactive:true);
        foreach (Image img in imgs)
        {
            if (img.gameObject.name == "PromptImage")
            {
                interactionSprite = ResourceManager.instance.GetControlSprite("Interaction", false);
                img.sprite = interactionSprite;
                break;
            }
        }
    }

    private void OnEnable()
    {
        // Set the interaction sprite when the widget is enabled
        // check if the sprite needs to be updated
        Sprite newSprite = ResourceManager.instance.GetControlSprite("Interaction", false);
        if (interactionSprite != newSprite)
        {
            interactionSprite = newSprite;
        }
    }

    public void SetPrompt(PromptType type)
    {
        switch(type)
        {
            case PromptType.Enter:
                promptText.text = "들어가기";
                break;
            case PromptType.Interact:
                promptText.text = $"대화하기";
                break;
            case PromptType.blessing:
                promptText.text = "축복받기";
                break;
            default:
                promptText.text = "";
                break;
        }
    }
    public void Refresh(string data)
    {
        // No implementation needed for this widget
    }
}
