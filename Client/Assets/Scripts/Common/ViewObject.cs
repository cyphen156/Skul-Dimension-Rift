using UnityEngine;

namespace Assets.Scripts.Common
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ViewObject : MonoBehaviour
    {
        [SerializeField]
        protected uint objectKey;

        protected SpriteRenderer spriteRenderer;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
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
