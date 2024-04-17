using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace INART.SlothyBakery
{
    using SceneTransition;
    using CanvasAnimator;
    using Core;

    public class CreateCuts : MonoBehaviour
    {
        [SerializeField] public static CreateCuts _instance;

        [SerializeField] private GameObject bladeTrailPrefab;
        public float minCuttingVelocity = .001f;

        [SerializeField] private bool isCutting = false;

        public bool IsCutting
        {
            get => isCutting;
        }

        private Vector2 previousPosition;

        [CanBeNull] private GameObject currentBladeTrail;

        private Rigidbody2D rgb;
        private Camera cam;
        private CircleCollider2D circleCollider2D;

        [SerializeField] private AudioSource cutSoundSource;

        [FormerlySerializedAs("_audioSource")] [SerializeField]
        private AudioSource crunchBreadSoundSource;

        private bool callOnce = false;


        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(this.gameObject);
        }

        private void Start()
        {
            cam = Camera.main;
            rgb = GetComponent<Rigidbody2D>();
            circleCollider2D = GetComponent<CircleCollider2D>();
            circleCollider2D.enabled = false;
            // cutSoundSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (GameStates._instance.GetGameState() == GameStates.GameState.PLAY)
            {
                if (Input.GetMouseButtonDown(0))
                    StartCutting();
                else if (Input.GetMouseButtonUp(0)) StopCutting();

                if (isCutting) UpdateCut();
            }
        }

        public void RunSound()
        {
            // cutSoundSource.Play();
            // crunchBreadSoundSource.volume = .7f;
            // if(!crunchBreadSoundSource.isPlaying)
            crunchBreadSoundSource.Play();
        }

        private void UpdateCut()
        {
            Vector2 newPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            rgb.position = newPosition;

            currentBladeTrail.transform.localPosition = rgb.position;
            currentBladeTrail.transform.parent = transform.parent;

            var velocity = (newPosition - previousPosition).magnitude * Time.deltaTime;
            if (velocity > minCuttingVelocity)
                circleCollider2D.enabled = true;
            else
                circleCollider2D.enabled = false;

            previousPosition = newPosition;
        }

        private void StartCutting()
        {
            isCutting = true;
            previousPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            currentBladeTrail = Instantiate(bladeTrailPrefab, previousPosition, Quaternion.identity);
            circleCollider2D.enabled = false;
        }

        private void StopCutting()
        {
            isCutting = false;
            currentBladeTrail.transform.SetParent(null);
            Destroy(currentBladeTrail, .25f);
            circleCollider2D.enabled = false;
            callOnce = false;
        }
    }
}