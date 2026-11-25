using System.Collections.Generic;
using Survival.InventorySystem;
using Survival.UI;
using Survival.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Survival.Player
{
    /// <summary>Finds the best nearby resource node and harvests it while the gather button is held.</summary>
    public class PlayerGatherer : MonoBehaviour
    {
        [SerializeField] Inventory inventory;
        [SerializeField] PlayerMovement movement;
        [SerializeField] MessageToast toast;
        [SerializeField] float range = 1.25f;
        [SerializeField] float cooldown = 0.35f;
        [Tooltip("How strongly nodes in the facing direction are preferred over closer ones.")]
        [SerializeField] float facingBias = 0.6f;

        static readonly List<Collider2D> s_Hits = new List<Collider2D>(32);

        InputAction _gather;
        float _nextGatherTime;
        ResourceNode _target;
        ContactFilter2D _filter;

        public ResourceNode Target => _target;

        /// <summary>Origin of the gather reach (roughly the player's chest).</summary>
        Vector2 Origin => (Vector2)transform.position + new Vector2(0f, 0.4f);

        void Awake()
        {
            if (inventory == null) inventory = GetComponent<Inventory>();
            if (movement == null) movement = GetComponent<PlayerMovement>();

            _filter = new ContactFilter2D().NoFilter();
            _filter.useTriggers = true;

            _gather = new InputAction("Gather", InputActionType.Button);
            _gather.AddBinding("<Keyboard>/e");
            _gather.AddBinding("<Keyboard>/space");
            _gather.AddBinding("<Mouse>/leftButton");
            _gather.AddBinding("<Gamepad>/buttonWest");
        }

        void OnEnable() => _gather.Enable();
        void OnDisable() => _gather.Disable();
        void OnDestroy() => _gather.Dispose();

        void Update()
        {
            var best = FindTarget();
            if (best != _target)
            {
                if (_target != null) _target.SetHighlighted(false);
                _target = best;
                if (_target != null) _target.SetHighlighted(true);
            }

            if (_gather.IsPressed() && Time.time >= _nextGatherTime && _target != null && !PointerOverUI())
            {
                TryGather(_target);
                _nextGatherTime = Time.time + cooldown;
            }
        }

        /// <summary>Hits <paramref name="node"/> once if the inventory has room for its yield.</summary>
        public bool TryGather(ResourceNode node)
        {
            if (node == null) return false;
            if (!inventory.CanAdd(node.DropItem, node.YieldPerHit))
            {
                if (toast != null) toast.Show("Inventory full");
                return false;
            }
            node.Hit(inventory);
            return true;
        }

        public ResourceNode FindTarget()
        {
            s_Hits.Clear();
            Physics2D.OverlapCircle(Origin, range, _filter, s_Hits);

            ResourceNode best = null;
            float bestScore = float.MaxValue;
            Vector2 facing = movement != null ? movement.Facing : Vector2.down;

            foreach (var hit in s_Hits)
            {
                var node = hit.GetComponentInParent<ResourceNode>();
                if (node == null) continue;
                Vector2 toNode = (Vector2)node.transform.position + new Vector2(0f, 0.3f) - Origin;
                float distance = toNode.magnitude;
                float alignment = distance > 0.001f ? Vector2.Dot(toNode / distance, facing) : 1f;
                float score = distance - alignment * facingBias;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }
            return best;
        }

        static bool PointerOverUI()
        {
            return EventSystem.current != null && Mouse.current != null &&
                   Mouse.current.leftButton.isPressed && EventSystem.current.IsPointerOverGameObject();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Origin, range);
        }
    }
}
