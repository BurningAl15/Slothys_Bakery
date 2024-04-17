using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    using SoundManager;

    /// <summary>
    /// All Colliders start disabled
    /// </summary>
    public class BreadDivisionCollider : MonoBehaviour
    {
        [SerializeField] private Draggable tempDraggable = null;
        [SerializeField] private int id;

        [SerializeField] private Collider2D collider;

        // private void OnEnable()
        // {
        //     collider=GetComponent<Collider2D>();
        //     collider.enabled = false;
        // }

        public void Init()
        {
            if (collider == null)
                collider = GetComponent<Collider2D>();

            collider.enabled = true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Draggable"))
                if (other.GetComponent<Draggable>() != null)
                {
                    if (other.GetComponent<Draggable>().id == id)
                    {
                        var _tempDraggable = other.GetComponent<Draggable>();
                        tempDraggable = _tempDraggable;
                        if (_tempDraggable.GetIsTouched())
                        {
                            // Recipee._instance.Check(tempDraggable.productType);
                            // Recipee._instance.CheckAllowedEatingObjects(tempDraggable.productType);
                            _tempDraggable.IsInRecognitionZone(transform.position);
                            _tempDraggable.NotSelected();
                            CounterManager._instance.AddCounter();
                            SoundManager._instance.Run_SFX(SoundManager.SoundType.Selection1);
                            collider.enabled = false;
                        }
                    }
                }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Draggable"))
                if (other.GetComponent<Draggable>() != null)
                {
                    var _tempDraggable = other.GetComponent<Draggable>();
                    if (_tempDraggable.GetIsTouched())
                    {
                        _tempDraggable.IsNotInRecognitionZone();
                        _tempDraggable.IsSelected();
                    }

                    tempDraggable = null;
                }
        }

        public void ResetColliderEnabling()
        {
            if (tempDraggable != null)
            {
                var _tempDraggable = tempDraggable;
                ;
                if (!_tempDraggable.GetIsTouched())
                {
                    _tempDraggable.IsNotInRecognitionZone();
                    _tempDraggable.IsSelected();
                }
            }

            collider.enabled = true;
        }
    }
}
