using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.DesignPatterns
{
    public class ObjectInGame : MonoBehaviour
    {
        [Header("Parent Properties to Identify")]
        public ObjectProperties.ObjectType objectType;

        public int objectID;
    }
}