using System;
using Survival.InventorySystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>One clickable inventory cell: background, icon and stack count.</summary>
    public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        static readonly Color NormalColor = new Color(0.16f, 0.13f, 0.17f, 0.85f);
        static readonly Color SelectedColor = new Color(0.85f, 0.65f, 0.25f, 0.95f);
        static readonly Color HoverColor = new Color(0.30f, 0.26f, 0.32f, 0.9f);

        Image _background;
        Image _icon;
        Text _count;
        bool _hovered;
        bool _selected;

        public int Index { get; private set; }
        public Action<int> Clicked;
        public Action<int> HoverChanged;

        public static InventorySlotView Create(Transform parent, int index, float size)
        {
            var bg = UIFactory.CreateImage($"Slot {index}", parent, NormalColor);
            ((RectTransform)bg.transform).sizeDelta = new Vector2(size, size);
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.04f, 0.06f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            var icon = UIFactory.CreateImage("Icon", bg.transform, Color.white);
            UIFactory.Stretch((RectTransform)icon.transform, size * 0.16f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var count = UIFactory.CreateText("Count", bg.transform, Mathf.RoundToInt(size * 0.32f), TextAnchor.LowerRight, Color.white);
            UIFactory.Stretch((RectTransform)count.transform, 3f);

            var view = bg.gameObject.AddComponent<InventorySlotView>();
            view.Index = index;
            view._background = bg;
            view._icon = icon;
            view._count = count;
            return view;
        }

        public void Bind(InventorySlot slot)
        {
            bool has = slot != null && !slot.IsEmpty;
            _icon.enabled = has && slot.item.icon != null;
            _icon.sprite = has ? slot.item.icon : null;
            _count.text = has && slot.count > 1 ? slot.count.ToString() : string.Empty;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            UpdateColor();
        }

        void UpdateColor()
        {
            _background.color = _selected ? SelectedColor : _hovered ? HoverColor : NormalColor;
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(Index);

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            UpdateColor();
            HoverChanged?.Invoke(Index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            UpdateColor();
            HoverChanged?.Invoke(-1);
        }
    }
}
