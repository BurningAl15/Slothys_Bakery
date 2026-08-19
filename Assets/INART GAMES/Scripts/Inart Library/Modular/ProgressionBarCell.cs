using UnityEngine;
using UnityEngine.UI;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Represents a single bar unit inside ModularProgressionCanvas.
    ///
    /// This component only holds the bar fill image.
    /// Lock icons and VFX are managed separately by ModularProgressionCanvas
    /// in their own containers so they always render on top of bars.
    ///
    /// PREFAB SETUP:
    ///   Root GameObject → ProgressionBarCell component
    ///     - barImage : Image with ImageType = Filled, FillMethod = Horizontal
    /// </summary>
    public class ProgressionBarCell : MonoBehaviour
    {
        [Tooltip("Fill image (ImageType = Filled, FillMethod = Horizontal).")]
        [SerializeField] public Image barImage;

        // Runtime fill value — tracked so DOTween always starts from the
        // correct position even if an animation is interrupted.
        [System.NonSerialized] public float FillValue = 0f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (barImage == null)
                Debug.LogWarning("[ProgressionBarCell] barImage is not assigned.", this);
        }
#endif
    }
}
