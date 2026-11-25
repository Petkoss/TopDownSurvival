using System.Collections.Generic;
using Survival.InventorySystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Always-visible hotbar (first row of slots) plus a full inventory window toggled with Tab / I.
    /// In the window, click a slot then another to move, merge or swap stacks.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] Inventory inventory;
        [SerializeField, Min(1)] int columns = 8;
        [SerializeField] float slotSize = 52f;
        [SerializeField] float spacing = 6f;

        readonly List<InventorySlotView> _hotbar = new List<InventorySlotView>();
        readonly List<InventorySlotView> _grid = new List<InventorySlotView>();
        GameObject _window;
        Text _tooltip;
        int _selected = -1;
        InputAction _toggle, _close;

        public bool IsOpen => _window != null && _window.activeSelf;
        public Inventory Inventory => inventory;

        void Awake()
        {
            _toggle = new InputAction("Toggle Inventory", InputActionType.Button);
            _toggle.AddBinding("<Keyboard>/tab");
            _toggle.AddBinding("<Keyboard>/i");
            _toggle.AddBinding("<Gamepad>/select");
            _close = new InputAction("Close Inventory", binding: "<Keyboard>/escape");
            Build();
        }

        void OnEnable()
        {
            _toggle.Enable();
            _close.Enable();
            if (inventory != null) inventory.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            _toggle.Disable();
            _close.Disable();
            if (inventory != null) inventory.Changed -= Refresh;
        }

        void OnDestroy()
        {
            _toggle.Dispose();
            _close.Dispose();
        }

        void Update()
        {
            if (_toggle.WasPressedThisFrame()) SetOpen(!IsOpen);
            else if (_close.WasPressedThisFrame() && IsOpen) SetOpen(false);
        }

        public void SetOpen(bool open)
        {
            _window.SetActive(open);
            Select(-1);
            _tooltip.text = string.Empty;
        }

        void Build()
        {
            var canvas = UIFactory.CreateCanvas("Inventory Canvas", transform, 10);
            int slotCount = inventory != null ? inventory.SlotCount : columns;
            int hotbarCount = Mathf.Min(columns, slotCount);

            // Hotbar, bottom centre.
            var hotbar = CreateRow(canvas.transform, "Hotbar", hotbarCount);
            hotbar.anchorMin = hotbar.anchorMax = new Vector2(0.5f, 0f);
            hotbar.pivot = new Vector2(0.5f, 0f);
            hotbar.anchoredPosition = new Vector2(0f, 14f);
            for (int i = 0; i < hotbarCount; i++) _hotbar.Add(CreateSlot(hotbar, i));

            // Full inventory window, centre.
            int rows = Mathf.CeilToInt(slotCount / (float)columns);
            var window = UIFactory.CreateImage("Inventory Window", canvas.transform, new Color(0.09f, 0.07f, 0.10f, 0.92f));
            _window = window.gameObject;
            var windowRect = (RectTransform)window.transform;
            float gridWidth = columns * slotSize + (columns - 1) * spacing;
            float gridHeight = rows * slotSize + (rows - 1) * spacing;
            windowRect.sizeDelta = new Vector2(gridWidth + 40f, gridHeight + 100f);
            windowRect.anchoredPosition = new Vector2(0f, 40f);
            var windowOutline = window.gameObject.AddComponent<Outline>();
            windowOutline.effectColor = new Color(0.45f, 0.33f, 0.25f, 1f);
            windowOutline.effectDistance = new Vector2(3f, -3f);

            var title = UIFactory.CreateText("Title", window.transform, 22, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.7f));
            UIFactory.Stretch((RectTransform)title.transform, 10f);
            title.text = "Inventory";

            var grid = UIFactory.CreateRect("Grid", window.transform);
            grid.sizeDelta = new Vector2(gridWidth, gridHeight);
            grid.anchoredPosition = new Vector2(0f, 0f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(slotSize, slotSize);
            layout.spacing = new Vector2(spacing, spacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            for (int i = 0; i < slotCount; i++) _grid.Add(CreateSlot(grid, i));

            _tooltip = UIFactory.CreateText("Tooltip", window.transform, 18, TextAnchor.LowerCenter, Color.white);
            UIFactory.Stretch((RectTransform)_tooltip.transform, 12f);

            _window.SetActive(false);
        }

        RectTransform CreateRow(Transform parent, string name, int count)
        {
            var row = UIFactory.CreateRect(name, parent);
            row.sizeDelta = new Vector2(count * slotSize + (count - 1) * spacing, slotSize);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row;
        }

        InventorySlotView CreateSlot(Transform parent, int index)
        {
            var view = InventorySlotView.Create(parent, index, slotSize);
            view.Clicked = OnSlotClicked;
            view.HoverChanged = OnSlotHover;
            return view;
        }

        void OnSlotClicked(int index)
        {
            if (!IsOpen) return;
            if (_selected < 0)
            {
                if (!inventory.GetSlot(index).IsEmpty) Select(index);
                return;
            }
            int from = _selected;
            Select(-1);
            inventory.MoveOrSwap(from, index);
        }

        void OnSlotHover(int index)
        {
            if (!IsOpen) return;
            if (index < 0 || inventory.GetSlot(index).IsEmpty)
            {
                _tooltip.text = string.Empty;
                return;
            }
            var slot = inventory.GetSlot(index);
            _tooltip.text = $"{slot.item.displayName}  ×{slot.count}";
        }

        void Select(int index)
        {
            _selected = index;
            foreach (var v in _grid) v.SetSelected(v.Index == index);
            foreach (var v in _hotbar) v.SetSelected(v.Index == index);
        }

        void Refresh()
        {
            if (inventory == null) return;
            foreach (var v in _hotbar) v.Bind(inventory.GetSlot(v.Index));
            foreach (var v in _grid) v.Bind(inventory.GetSlot(v.Index));
        }
    }
}
