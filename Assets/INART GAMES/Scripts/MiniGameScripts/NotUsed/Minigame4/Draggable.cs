using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace INART.SlothyBakery.Base
{
    using Core;
    /// <summary>
    /// All Colliders start disabled
    /// </summary>
    public class Draggable : MonoBehaviour
    {
        private Vector3 screenSpace;
        private Vector3 offset;

        private float notRecognitionZoneTimer;
        private float recognitionZoneTimer;
        [SerializeField] private float scaleTimer;

        //For interaction
        [SerializeField] private bool isSelected;

        [FormerlySerializedAs("isTouched")] [SerializeField]
        private bool isBeingDragged;

        //To check if draggable is in initial zone or recognition zone
        [SerializeField] private bool isInInitialPosition;
        [SerializeField] private bool isInRecognitionZone;

        private bool isActive;

        private Vector3 initialPosition;
        private Vector3 destinyPosition;

        [SerializeField] private float lerpDelay;

        private Collider2D collider;

        public int id;
        bool breakDraggable = true;

        private Camera cam;

        private SpriteRenderer spriteRenderer;

        public void BreakDraggable()
        {
            breakDraggable = true;
        }

        private void Awake()
        {
            collider = GetComponent<Collider2D>();
            cam = Camera.main;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void InitializeDraggable(bool reSetPosition = false)
        {
            if (reSetPosition)
                initialPosition = transform.localPosition;

            notRecognitionZoneTimer = lerpDelay - 0.1f;
            recognitionZoneTimer = 0;

            collider.enabled = true;

            isSelected = false;
            isBeingDragged = false;
            isInInitialPosition = false;
            isInRecognitionZone = false;
            breakDraggable = false;
        }


        private void Update()
        {
            if (GameStates._instance.IsGamePlaying())
            {
                if (!breakDraggable)
                {
                    if (!isInRecognitionZone)
                    {
                        //If we select other draggable after this and draggable was not in recognition zone
                        //runs this part of code
                        if (!isSelected)
                        {
                            if (transform.position != initialPosition) isInInitialPosition = false;
                            if (!isInInitialPosition)
                            {
                                notRecognitionZoneTimer += Time.deltaTime;
                                if (notRecognitionZoneTimer <= lerpDelay)
                                {
                                    collider.enabled = false;
                                    transform.position = Vector3.Lerp(transform.position, initialPosition,
                                        InverseLerpTimer(notRecognitionZoneTimer));
                                    transform.localScale = Vector3.Lerp(transform.localScale,
                                        DraggableAccornManager.instance.Get_MinScale(),
                                        InverseLerpTimer(notRecognitionZoneTimer));
                                }
                                else if (notRecognitionZoneTimer > lerpDelay)
                                {
                                    collider.enabled = true;
                                    notRecognitionZoneTimer = 0;

                                    isInInitialPosition = true;
                                    spriteRenderer.sortingOrder = 10;

                                    isBeingDragged = false;
                                }
                            }
                        }

                        //If we select this draggable, runs this part of code
                        if (isSelected)
                        {
                            //If we are clicking down over this draggable
                            if (!isBeingDragged)
                            {
                                if (transform.position != initialPosition) isInInitialPosition = false;
                                if (!isInInitialPosition)
                                {
                                    notRecognitionZoneTimer += Time.deltaTime;
                                    if (notRecognitionZoneTimer >= 0 && notRecognitionZoneTimer < lerpDelay / 10)
                                    {
                                        collider.enabled = true;
                                    }
                                    else if (notRecognitionZoneTimer >= lerpDelay / 10 &&
                                             notRecognitionZoneTimer <= lerpDelay)
                                    {
                                        collider.enabled = false;
                                        transform.position = Vector3.Lerp(transform.position, initialPosition,
                                            InverseLerpTimer(notRecognitionZoneTimer));
                                        transform.localScale = Vector3.Lerp(transform.localScale,
                                            DraggableAccornManager.instance.Get_MinScale(),
                                            InverseLerpTimer(notRecognitionZoneTimer));
                                    }
                                    else if (notRecognitionZoneTimer > lerpDelay)
                                    {
                                        collider.enabled = true;
                                        notRecognitionZoneTimer = 0;
                                        isInInitialPosition = true;
                                        spriteRenderer.sortingOrder = 10;

                                        isBeingDragged = false;
                                    }
                                }
                                else if (isInInitialPosition)
                                {
                                    transform.localScale = DraggableAccornManager.instance.Get_MinScale();
                                    isSelected = false;
                                    // print("CCC");
                                }
                            }
                            //If we are stop clicking over this draggable
                            else if (isBeingDragged)
                            {
                                notRecognitionZoneTimer = 0;
                            }
                        }
                    }
                    //if the draggable is in recognition zone and we stop clicking over it
                    //run this part of code
                    else if (isInRecognitionZone && !isBeingDragged)
                    {
                        isActive = false;
                        spriteRenderer.sortingOrder = 10;

                        if (transform.position != destinyPosition)
                        {
                            isActive = true;
                            recognitionZoneTimer += Time.fixedDeltaTime;
                            // print("1");
                        }

                        if (isActive)
                        {
                            if (recognitionZoneTimer <= lerpDelay)
                            {
                                collider.enabled = false;
                                transform.position = Vector3.Lerp(transform.position, destinyPosition,
                                    InverseLerpTimer(recognitionZoneTimer));
                                // print("2");
                            }
                            else if (recognitionZoneTimer >= lerpDelay)
                            {
                                collider.enabled = false;
                                recognitionZoneTimer = 0;
                                // print("3");
                            }
                        }
                        else
                        {
                            if (recognitionZoneTimer <= lerpDelay)
                            {
                                if (transform.localScale.magnitude > 1.732052f)
                                {
                                    transform.localScale = Vector3.Lerp(transform.localScale,
                                        DraggableAccornManager.instance.Get_MinScale(),
                                        InverseLerpTimer(scaleTimer));
                                }
                                else if (transform.localScale.magnitude <= 1.732052f)
                                {
                                    return;
                                }
                            }
                            else if (recognitionZoneTimer >= lerpDelay)
                            {
                                recognitionZoneTimer = 0;
                            }
                        }
                    }
                }
            }
        }

        private void OnMouseDown()
        {
            if (!isInRecognitionZone)
            {
                isSelected = true;
                isBeingDragged = true;

                spriteRenderer.sortingOrder = 12;

                DraggableAccornManager.instance.SetSelectedAccorn(this);
                screenSpace = cam.WorldToScreenPoint(transform.position);
                offset = transform.position -
                         cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                             screenSpace.z));
                transform.localScale = DraggableAccornManager.instance.Get_MaxScale();
            }
            else
            {
                isInRecognitionZone = false;
            }
        }

        private void OnMouseDrag()
        {
            if (isBeingDragged)
            {
                //To improve width, use the width of the collider (width + boxcollider.width/2)
                var currentScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenSpace.z);

                //convert the screen mouse position to world point and adjust with offset
                var currentPosition = cam.ScreenToWorldPoint(currentScreenSpace) + offset;

                //update the position of the object in the world
                transform.position = currentPosition;
            }
        }

        private void OnMouseUp()
        {
            isBeingDragged = false;
            //isSelected=false;
        }

        /// <summary>
        /// To know if we are in recognition zone and validate it
        /// </summary>
        /// <param name="_recognitionZonePosition">Sends recognition position to the position to the draggable</param>
        public void IsInRecognitionZone(Vector3 _recognitionZonePosition)
        {
            collider.enabled = false;
            isInRecognitionZone = true;
            transform.position = _recognitionZonePosition;
            destinyPosition = _recognitionZonePosition;
        }

        /// <summary>
        /// Called when the draggable is not in the recognition zone
        /// </summary>
        public void IsNotInRecognitionZone()
        {
            isInRecognitionZone = false;
            collider.enabled = true;
        }

        /// <summary>
        /// Set isSelected to false
        /// </summary>
        public void NotSelected()
        {
            isSelected = false;
        }

        /// <summary>
        /// Set isSelected to true
        /// </summary>
        public void IsSelected()
        {
            isSelected = true;
        }

        /// <summary>
        /// To get the isTouched value
        /// </summary>
        /// <returns>Boolean value</returns>
        public bool GetIsTouched()
        {
            return isBeingDragged;
        }

        /// <summary>
        /// Transform any float value to a value between 0 and lerpDelay
        /// To complete the needed value to lerp the position of the draggable
        /// </summary>
        /// <param name="_timer"></param>
        /// <returns> Float value between 0 and lerpDelay </returns>
        private float InverseLerpTimer(float _timer)
        {
            // return Mathf.InverseLerp(0, lerpDelay, _timer);
            // print("___AAA___");
            return _timer / lerpDelay;
        }
    }
}