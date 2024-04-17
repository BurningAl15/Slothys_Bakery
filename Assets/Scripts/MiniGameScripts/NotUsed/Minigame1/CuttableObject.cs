using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace INART.SlothyBakery.Base
{
    public class CuttableObject : MonoBehaviour
    {
        [SerializeField]
        private enum CutTypes
        {
            Two_Horizontal,
            Two_Vertical,
            Four_Diagonal,
            Four_Horizontal_Modified,
            Four_Hori_Vert,
            Three_Horizontal,
            Three_Vertical,
            Six_Half_Diagonal,
            Six_Horizontal_Diagonal,
            Eight
        }

        [SerializeField] private CutTypes cutTypes;

        private enum Axis
        {
            x,
            y,
            xy
        }

        [SerializeField] private List<Transform> elements = new List<Transform>();

        [Range(0.01f, 0.05f)] [SerializeField] private float movementAmount = .05f;
        private Coroutine currentCoroutine = null;

        [SerializeField] private List<LineManager> lines = new List<LineManager>();
        private int linesCount = 0;

        private bool hasFinished = true;
        [SerializeField] private GameObject linesContainer;

        [SerializeField] private TextMeshPro fractionText;
        [SerializeField] private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

        #region Activate or Deactivate CuttableObject

        public void Deactivate()
        {
            hasFinished = true;
            linesContainer.SetActive(false);
        }

        public void Activate()
        {
            hasFinished = false;
            linesContainer.SetActive(true);
            SetAlpha_Fraction(0);
        }

        #endregion

        #region Lines Count Utils

        public void IncreaseLinesAmount()
        {
            linesCount++;
        }

        public void DecreaseLinesAmount()
        {
            linesCount--;
        }

        public int GetLinesAmount()
        {
            return linesCount;
        }

        #endregion

        #region Animation Utils

        private IEnumerator CallElements(float _delay)
        {
            yield return new WaitForSeconds(_delay);
            GetComponent<SpriteRenderer>().enabled = false;
            var maxTime = _delay;

            if (fractionText != null)
                StartCoroutine(TurnOnFraction(_delay));
            else
            {
#if UNITY_EDITOR
                print("Cuttable Object name " + this.name + " has not a asigned the fraction sprite");
#endif
            }

            while (_delay >= 0)
            {
                for (var i = 0; i < elements.Count; i++)
                    switch (cutTypes)
                    {
                        case CutTypes.Two_Horizontal:
                            if (transform.position.x < elements[i].transform.position.x)
                                AnimatePiece(elements[i], _delay, maxTime, 1);
                            else
                                AnimatePiece(elements[i], _delay, maxTime, -1);

                            break;
                        case CutTypes.Two_Vertical:
                            if (transform.position.y < elements[i].transform.position.y)
                                AnimatePiece(elements[i], _delay, maxTime, 1, Axis.y);
                            else
                                AnimatePiece(elements[i], _delay, maxTime, -1, Axis.y);

                            break;
                        case CutTypes.Three_Vertical:
                            if (i % 2 == 0)
                                if (transform.position.x < elements[i].transform.position.y)
                                    AnimatePiece(elements[i], _delay, maxTime, 1, Axis.y);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1, Axis.y);
                            break;

                        case CutTypes.Three_Horizontal:
                            if (i % 2 == 0)
                                if (transform.position.x < elements[i].transform.position.x)
                                    AnimatePiece(elements[i], _delay, maxTime, 1, Axis.x);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1, Axis.x);
                            break;

                        case CutTypes.Four_Diagonal:
                            if (transform.position.x < elements[i].transform.position.x)
                            {
                                if (transform.position.y < elements[i].transform.position.y)
                                    AnimatePiece(elements[i], _delay, maxTime, 1, 1);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, 1, -1);
                            }
                            else
                            {
                                if (transform.position.y < elements[i].transform.position.y)
                                    AnimatePiece(elements[i], _delay, maxTime, -1, 1);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1, -1);
                            }

                            break;

                        case CutTypes.Four_Hori_Vert:
                            if (i == 0 || i == 2)
                            {
                                if (transform.position.y < elements[i].transform.position.y)
                                    AnimatePiece(elements[i], _delay, maxTime, 1, Axis.y);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1, Axis.y);
                            }
                            else
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                    AnimatePiece(elements[i], _delay, maxTime, 1);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1);
                            }

                            break;

                        case CutTypes.Four_Horizontal_Modified:
                            if (i == 0 || i == 3)
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                    AnimatePiece(elements[i], _delay, maxTime, 1);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1);
                            }
                            else
                            {
                                if (transform.position.y < elements[i].transform.position.y)
                                    AnimatePiece(elements[i], _delay, maxTime, 1, Axis.y);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1, Axis.y);
                            }

                            break;

                        case CutTypes.Six_Half_Diagonal:
                            if (i >= 2 && i <= 3)
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                    AnimatePiece(elements[i], _delay, maxTime, 1);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1);
                            }
                            else
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                        AnimatePiece(elements[i], _delay, maxTime, 1, 1);
                                    else
                                        AnimatePiece(elements[i], _delay, maxTime, 1, -1);
                                }
                                else
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                        AnimatePiece(elements[i], _delay, maxTime, -1, 1);
                                    else
                                        AnimatePiece(elements[i], _delay, maxTime, -1, -1);
                                }
                            }

                            break;

                        case CutTypes.Six_Horizontal_Diagonal:
                            if (i >= 0 && i <= 1)
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                    AnimatePiece(elements[i], _delay, maxTime, 1);
                                else
                                    AnimatePiece(elements[i], _delay, maxTime, -1);
                            }
                            // if (i >= 2 && i <= 3)
                            else
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                        AnimatePiece(elements[i], _delay, maxTime, 1, 1);
                                    else
                                        AnimatePiece(elements[i], _delay, maxTime, 1, -1);
                                }
                                else
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                        AnimatePiece(elements[i], _delay, maxTime, -1, 1);
                                    else
                                        AnimatePiece(elements[i], _delay, maxTime, -1, -1);
                                }
                            }

                            break;

                        case CutTypes.Eight:
                            if (i >= 0 && i < 2)
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                    {
                                        //2
                                        AnimatePiece(elements[i], _delay, maxTime, .2f, 1f);
                                    }
                                }
                                else
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                    {
                                        //1
                                        AnimatePiece(elements[i], _delay, maxTime, -.2f, 1f);
                                    }
                                }
                            }
                            else if (i >= 2 && i < 4)
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                    {
                                        //2
                                        AnimatePiece(elements[i], _delay, maxTime, 1f, .2f);
                                    }
                                }
                                else
                                {
                                    if (transform.position.y < elements[i].transform.position.y)
                                    {
                                        //1
                                        AnimatePiece(elements[i], _delay, maxTime, -1f, .2f);
                                    }
                                }
                            }
                            else if (i >= 4 && i < 6)
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                {
                                    //2
                                    AnimatePiece(elements[i], _delay, maxTime, 1, -.2f);
                                }
                                else
                                {
                                    //1
                                    AnimatePiece(elements[i], _delay, maxTime, -1f, -.2f);
                                }
                            }
                            else
                            {
                                if (transform.position.x < elements[i].transform.position.x)
                                {
                                    //2
                                    AnimatePiece(elements[i], _delay, maxTime, .2f, -1f);
                                }
                                else
                                {
                                    //1
                                    AnimatePiece(elements[i], _delay, maxTime, -.2f, -1f);
                                }
                            }

                            break;

                        default:
                            print("Nothing");
                            break;
                    }

                yield return Time.fixedDeltaTime;
                _delay -= Time.fixedDeltaTime;
            }

            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                spriteRenderers[i].color = new Color(0, 0, 0, 0.5f);
            }

            gameObject.SetActive(false);
            currentCoroutine = null;
        }

        private Vector2 AnimatePiece(Transform _element, float _delay, float _maxTime, float _direction,
            Axis _axis = Axis.x)
        {
            var temp = _axis == Axis.x
                ? new Vector2(_element.position.x + movementAmount * _direction, _element.position.y)
                : new Vector2(_element.position.x, _element.position.y + movementAmount * _direction);

            return _element.position = Vector2.Lerp(_element.position, temp,
                1 - _delay / _maxTime);
        }


        private Vector2 AnimatePiece(Transform _element, float _delay, float _maxTime, float _direction_X,
            float _direction_Y)
        {
            var temp = new Vector2(_element.position.x + movementAmount * _direction_X,
                _element.position.y + movementAmount * _direction_Y);

            return _element.position = Vector2.Lerp(_element.position, temp,
                1 - _delay / _maxTime);
        }

        #endregion

        public void SetFractionText(string _fractionText)
        {
            fractionText.text = _fractionText;
        }

        public void SetFractionText(string _fractionText, string _effect)
        {
            fractionText.text = "<" + _effect + "> " + _fractionText + " </" + _effect + ">";
        }

        public void SetFractionText(string _fractionText, List<string> _effects)
        {
            fractionText.text = TextAnimatorCombination(_effects, _fractionText);
        }

        string TextAnimatorCombination(List<string> _effects, string _fractionText)
        {
            string initialCombination = MakeTag(_effects[0], _fractionText);
            for (int i = 1; i < _effects.Count; i++)
                initialCombination = MakeTag(_effects[i], initialCombination);

            return initialCombination;
        }

        string MakeTag(string _effect, string _fractionText)
        {
            return "<" + _effect + ">" + _fractionText + "</" + _effect + ">";
        }

        IEnumerator TurnOnFraction(float _delay)
        {
            var maxTime = _delay;
            float tempDelay = 0;

            //We Fade, the equals sign so we can see it
            while (tempDelay <= maxTime)
            {
                tempDelay += Time.fixedDeltaTime;
                SetAlpha_Fraction(tempDelay / maxTime);

                yield return Time.fixedDeltaTime;
            }
        }

        private void SetAlpha_Fraction(float _value)
        {
            var tempColor = fractionText.color;
            fractionText.color = new Color(tempColor.r, tempColor.g, tempColor.b, _value);
        }

        private void Update()
        {
            if (!hasFinished)
                if (linesCount >= lines.Count)
                {
                    if (currentCoroutine == null)
                        currentCoroutine = StartCoroutine(CallElements(.25f));
                    hasFinished = true;
                }
        }

        public bool HasFinished()
        {
            return hasFinished;
        }
    }
}
