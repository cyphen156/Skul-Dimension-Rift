using UnityEngine;

namespace Assets.Scripts.Common
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ViewObject : MonoBehaviour
    {
        [SerializeField] [HexView] 
        protected uint objectKey;

        protected SpriteRenderer spriteRenderer;
        protected Rigidbody2D rb;
        protected BoxCollider2D boxCollider;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            boxCollider = GetComponent<BoxCollider2D>();

        }

        protected virtual void Start()
        {
            if (objectKey != 0u)
            {
                ApplySprite();
            }
        }

        protected virtual void OnDisable()
        {
            objectKey = 0u;
        }

        public virtual void SetObjectKey(uint key)
        {
            objectKey = key;
            ApplySprite();
        }

        public uint GetObjectKey()
        {
            return objectKey;
        }

        protected virtual void ApplySprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (ResourceManager.instance == null)
            {
                return;
            }

            Sprite sprite = ResourceManager.instance.GetSprite(objectKey);

            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }
}
