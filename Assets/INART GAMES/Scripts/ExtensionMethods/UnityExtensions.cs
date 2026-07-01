using UnityEngine;

namespace INART.SlothyBakery.Base
{
    /// <summary>
    /// Extension methods for Transform components.
    /// Provides convenient helper methods for common transform operations.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Resets the local transform to identity (position = 0, rotation = 0, scale = 1).
        /// </summary>
        /// <param name="transform">The transform to reset</param>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Resets only the local position to zero.
        /// </summary>
        /// <param name="transform">The transform to reset</param>
        public static void ResetLocalPosition(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Resets only the local rotation to identity.
        /// </summary>
        /// <param name="transform">The transform to reset</param>
        public static void ResetLocalRotation(this Transform transform)
        {
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Resets only the local scale to one.
        /// </summary>
        /// <param name="transform">The transform to reset</param>
        public static void ResetLocalScale(this Transform transform)
        {
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Sets the X component of local position.
        /// </summary>
        public static void SetLocalX(this Transform transform, float x)
        {
            Vector3 pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }

        /// <summary>
        /// Sets the Y component of local position.
        /// </summary>
        public static void SetLocalY(this Transform transform, float y)
        {
            Vector3 pos = transform.localPosition;
            pos.y = y;
            transform.localPosition = pos;
        }

        /// <summary>
        /// Sets the Z component of local position.
        /// </summary>
        public static void SetLocalZ(this Transform transform, float z)
        {
            Vector3 pos = transform.localPosition;
            pos.z = z;
            transform.localPosition = pos;
        }
    }

    /// <summary>
    /// Extension methods for RectTransform components.
    /// Provides convenient helper methods for UI transform operations.
    /// </summary>
    public static class RectTransformExtensions
    {
        /// <summary>
        /// Sets the anchored position X component.
        /// </summary>
        public static void SetAnchoredX(this RectTransform rectTransform, float x)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x = x;
            rectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// Sets the anchored position Y component.
        /// </summary>
        public static void SetAnchoredY(this RectTransform rectTransform, float y)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.y = y;
            rectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// Sets the size delta (width and height).
        /// </summary>
        public static void SetSize(this RectTransform rectTransform, float width, float height)
        {
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Sets the width (size delta X).
        /// </summary>
        public static void SetWidth(this RectTransform rectTransform, float width)
        {
            Vector2 size = rectTransform.sizeDelta;
            size.x = width;
            rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// Sets the height (size delta Y).
        /// </summary>
        public static void SetHeight(this RectTransform rectTransform, float height)
        {
            Vector2 size = rectTransform.sizeDelta;
            size.y = height;
            rectTransform.sizeDelta = size;
        }
    }

    /// <summary>
    /// Extension methods for GameObject.
    /// Provides convenient helper methods for common GameObject operations.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Gets or adds a component of type T.
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Destroys all children of this GameObject.
        /// </summary>
        public static void DestroyChildren(this GameObject gameObject)
        {
            Transform transform = gameObject.transform;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Destroys all children immediately (editor-safe).
        /// </summary>
        public static void DestroyChildrenImmediate(this GameObject gameObject)
        {
            Transform transform = gameObject.transform;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Sets the layer recursively for this GameObject and all children.
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }
    }
}
