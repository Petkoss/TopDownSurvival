using System;
using System.Collections;
using Survival.InventorySystem;
using Survival.Items;
using UnityEngine;

namespace Survival.World
{
    /// <summary>A harvestable object in the world (tree, rock, mushroom...).</summary>
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] ItemDefinition dropItem;
        [SerializeField, Min(1)] int yieldPerHit = 1;
        [SerializeField, Min(1)] int hitsToDeplete = 3;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Color highlightTint = new Color(1f, 1f, 0.65f, 1f);

        int _hits;
        Coroutine _shake;

        /// <summary>Unique id assigned by the world generator; used to remember harvested nodes.</summary>
        public string NodeId { get; set; }
        public ItemDefinition DropItem => dropItem;
        public int YieldPerHit => yieldPerHit;
        public int HitsRemaining => hitsToDeplete - _hits;

        public event Action<ResourceNode> Depleted;

        void Awake()
        {
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        }

        public void SetHighlighted(bool on)
        {
            if (visual != null) visual.color = on ? highlightTint : Color.white;
        }

        /// <summary>Hits the node once, putting the yield into <paramref name="inventory"/>. Returns true if it was depleted.</summary>
        public bool Hit(Inventory inventory)
        {
            if (_hits >= hitsToDeplete) return false;
            _hits++;
            inventory.Add(dropItem, yieldPerHit);

            if (_hits >= hitsToDeplete)
            {
                Depleted?.Invoke(this);
                Destroy(gameObject);
                return true;
            }

            if (_shake != null) StopCoroutine(_shake);
            if (visual != null && isActiveAndEnabled) _shake = StartCoroutine(Shake());
            return false;
        }

        IEnumerator Shake()
        {
            var t = visual.transform;
            const float duration = 0.15f;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float strength = 0.06f * (1f - time / duration);
                t.localPosition = new Vector3(Mathf.Sin(time * 90f) * strength, 0f, 0f);
                yield return null;
            }
            t.localPosition = Vector3.zero;
            _shake = null;
        }
    }
}
