using Assets.Scripts.Common;
using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Item
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ItemView : ViewObject, IInteractable
    {
        public void Interact()
        {
        }
    }
}