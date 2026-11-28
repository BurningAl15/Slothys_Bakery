using Fatcat.Slothy.Bakery.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.Tests
{
    /// <summary>The answer keypad types only digits and one "/" (never first), and deletes with the empty key.</summary>
    public class FractionKeypadEditTests
    {
        [Test]
        public void Keypad_TypesDigitsAndOneSlash_AndDeletes()
        {
            var go = new GameObject("pad");
            var fieldGo = new GameObject("field", typeof(RectTransform));
            try
            {
                var field = fieldGo.AddComponent<TMP_InputField>();
                var pad = go.AddComponent<FractionKeypad>();
                var so = new UnityEditor.SerializedObject(pad);
                so.FindProperty("target").objectReferenceValue = field;
                so.ApplyModifiedPropertiesWithoutUndo();

                pad.Press("/");                 // "/" cannot come first
                Assert.AreEqual("", field.text);
                pad.Press("2"); pad.Press("/"); pad.Press("/"); pad.Press("2");
                Assert.AreEqual("2/2", field.text);
                pad.Press("");                  // backspace
                Assert.AreEqual("2/", field.text);
                pad.Press("1"); pad.Press("2"); pad.Press("3");
                Assert.AreEqual("2/123", field.text, "five characters at most");
                pad.Press("4");
                Assert.AreEqual("2/123", field.text);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(fieldGo);
            }
        }
    }
}
