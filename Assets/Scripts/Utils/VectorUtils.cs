using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using System.Collections.Generic;

namespace AldhaDev.Match3
{
    public static class VectorUtils
    {
        public static Vector3 GetVector(Directions directions = Directions.XYZ)
        {
            switch (directions)
            {
                case Directions.X:
                    return Vector3.right;
                case Directions.Y:
                    return Vector3.up;
                case Directions.Z:
                    return Vector3.forward;
                case Directions.XY:
                    return Vector3.right + Vector3.up;
                case Directions.XZ:
                    return Vector3.right + Vector3.forward;
                case Directions.YZ:
                    return Vector3.up + Vector3.forward;
                default:
                case Directions.XYZ:
                    return Vector3.one;
            }
        }
        
        public static Vector3 GetCenterOfChildren(List<Transform> childs)
        {
            Vector3 center = Vector3.zero;
            int childCount = childs.Count;

            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    center += childs[i].position;
                }
                center /= childCount;
            }

            return center;
        }
    }
}
