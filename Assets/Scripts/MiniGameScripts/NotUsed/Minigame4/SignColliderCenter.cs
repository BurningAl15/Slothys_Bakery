using UnityEngine;

namespace INART.SlothyBakery.Base
{
    using SoundManager;
    public class SignColliderCenter : MonoBehaviour
    {
        [SerializeField] private SignDraggable tempDraggable;
        [SerializeField] private Collider2D collider;

        [SerializeField] private int id = -1;

        public void Init()
        {
            if (collider == null)
                collider = GetComponent<Collider2D>();

            collider.enabled = true;
        }

        public void Deactivate()
        {
            collider.enabled = false;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Draggable"))
                if (other.GetComponent<SignDraggable>() != null)
                {
                    var _tempDraggable = other.GetComponent<SignDraggable>();
                    // print(_tempDraggable);
                    tempDraggable = _tempDraggable;
                    if (_tempDraggable.GetIsTouched())
                    {
                        _tempDraggable.IsInRecognitionZone(transform.position);
                        _tempDraggable.NotSelected();
                        // ValueManager._instance.SetValue(_tempDraggable.id);
                        SoundManager._instance.Run_SFX(SoundManager.SoundType.Selection1);
                        collider.enabled = false;
                    }
                }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Draggable"))
                if (other.GetComponent<SignDraggable>() != null)
                {
                    var _tempDraggable = other.GetComponent<SignDraggable>();
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