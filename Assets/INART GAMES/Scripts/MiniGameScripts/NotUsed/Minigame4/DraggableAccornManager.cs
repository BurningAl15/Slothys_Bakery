using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    public class DraggableAccornManager : MonoBehaviour
    {
        public static DraggableAccornManager instance;

        [SerializeField] private List<Draggable> draggableAccorns = new List<Draggable>();
        [SerializeField] private Draggable currentAccorn;

        [SerializeField] private List<SignDraggable> draggableSigns = new List<SignDraggable>();
        [SerializeField] private SignDraggable currentSigns;


        [SerializeField] private GameObject parent;

        public GameObject Parent
        {
            get => parent;
            set => parent = value;
        }

        [SerializeField] private float maxScale;
        [SerializeField] private float minScale;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this) Destroy(gameObject);
        }

        public void SetSelectedAccorn(Draggable _accorn)
        {
            for (var i = 0; i < draggableAccorns.Count; i++) draggableAccorns[i].NotSelected();
            _accorn.IsSelected();
            currentAccorn = _accorn;
            currentAccorn.transform.localScale = Vector3.one * maxScale;
        }

        public void SetSelectedAccorn(SignDraggable _sign)
        {
            for (var i = 0; i < draggableSigns.Count; i++) draggableSigns[i].NotSelected();
            _sign.IsSelected();
            currentSigns = _sign;
            currentSigns.transform.localScale = Vector3.one * maxScale;
        }

        public void CleanSelectedAccorn()
        {
            for (var i = 0; i < draggableAccorns.Count; i++)
            {
                draggableAccorns[i].NotSelected();
                // draggableAccorns[i].transform.parent=parent.transform;
            }

            currentAccorn = null;
        }

        public void TurnOnAccorns()
        {
            for (var i = 0; i < draggableAccorns.Count; i++) draggableAccorns[i].enabled = true;
        }

        public void TurnOffAccorns()
        {
            for (var i = 0; i < draggableAccorns.Count; i++) draggableAccorns[i].enabled = false;
        }

        public Vector3 Get_MaxScale()
        {
            return Vector3.one * maxScale;
        }

        public Vector3 Get_MinScale()
        {
            return Vector3.one * minScale;
        }
    }
}