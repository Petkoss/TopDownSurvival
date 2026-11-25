using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>Short status messages ("Game saved") plus a one-line controls hint.</summary>
    public class MessageToast : MonoBehaviour
    {
        [SerializeField] float duration = 2f;
        [SerializeField, TextArea] string controlsHint =
            "WASD move   E / Space / LMB gather   Tab inventory   F5 save   F9 load   F10 new world";

        Text _message;
        float _hideAt;

        void Awake()
        {
            var canvas = UIFactory.CreateCanvas("Message Canvas", transform, 20);

            _message = UIFactory.CreateText("Message", canvas.transform, 24, TextAnchor.UpperCenter, Color.white);
            var rect = (RectTransform)_message.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            _message.text = string.Empty;

            var hint = UIFactory.CreateText("Controls", canvas.transform, 14, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.75f));
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 1f);
            hintRect.pivot = new Vector2(0f, 1f);
            hintRect.anchoredPosition = new Vector2(10f, -8f);
            hint.text = controlsHint;
        }

        public void Show(string message)
        {
            _message.text = message;
            _hideAt = Time.unscaledTime + duration;
        }

        void Update()
        {
            if (_message.text.Length > 0 && Time.unscaledTime >= _hideAt) _message.text = string.Empty;
        }
    }
}
