using System;
using System.Collections;
using UnityEngine;

namespace AldhaDev.Match3
{
    public enum Directions
    {
        X,
        Y,
        Z,
        XY,
        YZ,
        XZ,
        XYZ
    }
    public static class TransformAnimationExtend
    {
        #region Base animations
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="directions">To Indicate the axis to work with</param>
        /// <param name="factor">Could be positive or negative, and shows the size to grow</param>
        public static void TransformLocalScale(this Transform transform, float factor, Directions directions = Directions.XY)
        {
            transform.localScale = VectorUtils.GetVector(directions) * factor;
        }
        
        public static void TransformRotation(this Transform transform, float factor, Directions directions = Directions.XYZ)
        {
            transform.rotation = Quaternion.Euler(VectorUtils.GetVector(directions)*factor);
        }
        
        public static void TransformLocalRotation(this Transform transform, float factor, Directions directions = Directions.XYZ)
        {
            transform.localRotation = Quaternion.Euler(VectorUtils.GetVector(directions)*factor);
        }
        
        public static void TransformLocalPosition(this Transform transform, float factor, Directions directions = Directions.XYZ)
        {
            transform.localPosition = VectorUtils.GetVector(directions) * factor;
        }

        public static void TransformPosition(this Transform transform, float factor, Directions directions = Directions.XYZ)
        {
            transform.position = VectorUtils.GetVector(directions) * factor;
        }

        #endregion

        public static IEnumerator ScaleAnimation(this Transform transform, AnimationCurve animCurve, float initialSize, float endSize, float time, float delay, Action action)
        {
            transform.TransformLocalScale(initialSize);
            float timeIndex = 0;

            float timeStep = Time.deltaTime;
            
            while (timeIndex <= time)
            {
                timeIndex = Mathf.Clamp01(timeIndex);
                transform.TransformLocalScale(endSize*animCurve.Evaluate(timeIndex/time));
                timeIndex += timeStep;
                yield return new WaitForSeconds(timeStep);
            }

            yield return new WaitForSeconds(delay);
            
            while (timeIndex <= time)
            {
                timeIndex = Mathf.Clamp01(timeIndex);
                transform.TransformLocalScale(endSize*animCurve.Evaluate(1-timeIndex/time));
                timeIndex += timeStep;
                yield return new WaitForSeconds(timeStep);
            }
            transform.TransformLocalScale(initialSize);
            yield return null;
            
            action?.Invoke();
        }
    }
}
