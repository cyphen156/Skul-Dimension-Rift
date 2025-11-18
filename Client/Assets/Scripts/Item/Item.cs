using Assets.Scripts.Interface;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Item : MonoBehaviour, IInteractable
{
    [SerializeField] private int itemId;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    private void OnEnable()
    {
        if (itemId != -1)
        {
            Sprite sprite = ResourceManager.instance.GetItemSprite(itemId);

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }

    private void OnDisable()
    {
        itemId = -1;
    }
    public void SetItemInfo(int id, string itemName)
    {
        itemId = id;

        Sprite sprite = ResourceManager.instance.GetItemSprite(itemId);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    public int GetItemID()
    {
        return itemId;
    }

    public void Interact()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        
    }
}
