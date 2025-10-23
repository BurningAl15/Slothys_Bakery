using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.DesignPatterns {
    public class ObjectProperties {
        public enum ObjectType {
            PowerUp,
            Particles
        }
    }

    [System.Serializable]
    public class ObjectPoolItem {
        public int amountToPool;
        public GameObject objectToPool;
        public ObjectProperties.ObjectType objectType;
        public int objectID;
        public bool shouldExpand;
    }
}

namespace INART.SlothyBakery.DesignPatterns {
    public class GameObjectPool : MonoBehaviour {
        public static GameObjectPool instance;

        const string tag_PowerUp = "PowerUp";
        const string tag_Particles = "Particles";

        //const string tag_Bullet = "Bullet";

        [SerializeField] List<ObjectPoolItem> itemsToPool = new List<ObjectPoolItem> ();
        [SerializeField] List<GameObject> pooledObjects = new List<GameObject> ();

        private void Awake () {
            if (instance == null)
                instance = this;
            CreateElementsInPool_Complex ();
        }
        
        void CreateElementsInPool_Complex () {
            pooledObjects = new List<GameObject> ();
            int particleCounter = 0;
            int powerUpCounter = 0;
            // int bulletCounter = 0;
            // int enemyCounter = 0;

            bool isDifferent = false;

            foreach (ObjectPoolItem item in itemsToPool) {
                for (int i = 0; i < item.amountToPool; i++) {
                    SetTag (item);

                    if (!isDifferent) {
                        switch (item.objectType) {
                            case ObjectProperties.ObjectType.PowerUp:
                                item.objectID = powerUpCounter;
                                item.objectToPool.GetComponent<ObjectInGame> ().objectID = item.objectID;
                                powerUpCounter++;
                                break;
                            case ObjectProperties.ObjectType.Particles:
                                item.objectID = particleCounter;
                                item.objectToPool.GetComponent<ObjectInGame> ().objectID = item.objectID;
                                particleCounter++;
                                break;
                        }
                        isDifferent = true;
                    }
                    GameObject obj = (GameObject) Instantiate (item.objectToPool);
                    obj.SetActive (false);
                    pooledObjects.Add (obj);

                    if (i == item.amountToPool - 1) {
                        isDifferent = false;
                    }
                }
            }
        }

        void SetID(ObjectPoolItem _item, int _counter)
        {
            _item.objectID = _counter;
            _counter++;
        }

        public GameObject GetPooledObject (string tag) {
            for (int i = 0; i < pooledObjects.Count; i++) {
                if (!pooledObjects[i].activeInHierarchy && pooledObjects[i].tag == tag) {
                    return pooledObjects[i];
                }
            }
            foreach (ObjectPoolItem item in itemsToPool) {
                if (item.objectToPool.tag == tag) {
                    if (item.shouldExpand) {
                        GameObject obj = (GameObject) Instantiate (item.objectToPool);
                        obj.SetActive (false);
                        pooledObjects.Add (obj);
                        return obj;
                    }
                }
            }
            return null;
        }

        public GameObject GetPooledObject (string tag, int id) {
            for (int i = 0; i < pooledObjects.Count; i++) {
                //if (!pooledObjects[i].activeInHierarchy && pooledObjects[i].tag == tag && pooledObjects[i].GetComponent<Enemy>().GetEnemyType()==id) {
                // && pooledObjects[i].GetComponent<ObjectInGame> ().objectID == id
                if (!pooledObjects[i].activeInHierarchy && pooledObjects[i].tag == tag ) {
                    return pooledObjects[i];
                }
            }
            foreach (ObjectPoolItem item in itemsToPool) {
                if (item.objectToPool.CompareTag (tag)) {
                    if (item.shouldExpand) {
                        GameObject obj = (GameObject) Instantiate (item.objectToPool);
                        obj.SetActive (false);
                        pooledObjects.Add (obj);
                        return obj;
                    }
                }
            }
            return null;
        }

        void SetTag (ObjectPoolItem _item) {
            _item.objectToPool.GetComponent<ObjectInGame> ().objectType = _item.objectType;
            switch (_item.objectType) {
                case ObjectProperties.ObjectType.PowerUp:
                    _item.objectToPool.tag = tag_PowerUp;
                    break;
                case ObjectProperties.ObjectType.Particles:
                    _item.objectToPool.tag = tag_Particles;
                    break;
            }
        }

    }
}