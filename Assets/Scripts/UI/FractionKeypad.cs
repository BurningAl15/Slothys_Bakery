using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.UI
{
    /// <summary>
    /// On-screen keypad for fraction answers (owner 2026-11-27: the answer takes only digits and "/", e.g. 2/2).
    /// The phone's keyboard never opens: the field is read-only and these keys write into it, so letters cannot
    /// get in and the keyboard does not cover the plates. Keys: 0-9, "/" (once, not first) and backspace.
    /// Built into the scenes by BakeryAnswerKeypadBuilder (Editor).
    /// </summary>
    public sealed class FractionKeypad : MonoBehaviour
    {
        [Serializable]
        public struct Key
        {
            public Button button;
            [Tooltip("The character it types; empty = backspace.")]
            public string value;
        }

        [SerializeField] private TMP_InputField target;
        [SerializeField] private Key[] keys = Array.Empty<Key>();
        [SerializeField, Min(1)] private int maxLength = 5;

        public TMP_InputField Target => target;

        private void Awake()
        {
            if (target != null)
            {
                target.readOnly = true;              // no OS keyboard, no stray characters
                target.shouldHideMobileInput = true;
                target.characterLimit = maxLength;
            }
            foreach (var k in keys)
            {
                if (k.button == null) continue;
                string v = k.value;
                var b = k.button;
                b.onClick.AddListener(() => Press(v, b.transform));
            }
        }

        /// <summary>Types <paramref name="value"/> (a digit or "/"), or deletes one character when it is empty.</summary>
        public void Press(string value, Transform feedback = null)
        {
            if (target == null) return;
            string text = target.text ?? string.Empty;
            if (string.IsNullOrEmpty(value))
            {
                if (text.Length > 0) text = text.Substring(0, text.Length - 1);
            }
            else if (Accepts(text, value[0]))
            {
                text += value[0];
            }
            else
            {
                if (feedback != null) { feedback.DOKill(true); feedback.DOShakePosition(0.25f, new Vector3(6f, 0f, 0f), 18).SetUpdate(true); }
                return;
            }
            target.text = text;
            if (feedback != null) { feedback.DOKill(true); feedback.DOPunchScale(Vector3.one * 0.12f, 0.18f, 6, 0.6f).SetUpdate(true); }
        }

        /// <summary>Digits always (up to the limit); one "/" and never first.</summary>
        public bool Accepts(string text, char c)
        {
            if (text.Length >= maxLength) return false;
            if (char.IsDigit(c)) return true;
            return c == '/' && text.Length > 0 && text.IndexOf('/') < 0;
        }
    }
}
