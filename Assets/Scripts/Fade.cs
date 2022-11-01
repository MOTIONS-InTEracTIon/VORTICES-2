using System.Collections;
using UnityEngine;

// This script gets put in a root and will change the alpha of all renderers
namespace Vortices
{
    public class Fade : MonoBehaviour
    {
        public bool fadeOnStart = false;
        public float fadeDuration = 1.0f;
        public float lowerAlpha {get; set;}
        public bool isRunning;
        public int fadeOps;

        private Renderer[] objectRenderer;
        private CanvasGroup[] canvasGroups;

        void Start()
        {
            if (fadeOnStart)
            {
                FadeIn();
            }
            lowerAlpha = 0;
        }

        private void GetObjects()
        {
            objectRenderer = transform.GetComponentsInChildren<Renderer>(true);
            canvasGroups = transform.GetComponentsInChildren<CanvasGroup>();
        }

        public void FadeIn()
        {
            GetObjects();
            for (int i = 0; i < objectRenderer.Length; i++)
            {
                StartCoroutine(FadeRoutine(lowerAlpha, 1, objectRenderer[i], null));
            }
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                StartCoroutine(FadeRoutine(lowerAlpha, 1, null, canvasGroups[i]));
            }

        }

        public void FadeOut()
        {
            GetObjects();
            for (int i = 0; i < objectRenderer.Length; i++)
            {
                StartCoroutine(FadeRoutine(1, lowerAlpha, objectRenderer[i], null));
            }
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                StartCoroutine(FadeRoutine(1, lowerAlpha, null, canvasGroups[i]));
            }
        }

        public IEnumerator FadeInCoroutine()
        {
            isRunning = true;
            fadeOps = 0;
            GetObjects();
            for (int i = 0; i < objectRenderer.Length; i++)
            {
                StartCoroutine(FadeRoutine(lowerAlpha, 1, objectRenderer[i], null));
            }
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                StartCoroutine(FadeRoutine(lowerAlpha, 1, null, canvasGroups[i]));
            }

            while (fadeOps > 0)
            {
                yield return null;
            }

            isRunning = false;
        }

        public IEnumerator FadeOutCoroutine()
        {
            isRunning = true;
            fadeOps = 0;
            GetObjects();
            for (int i = 0; i < objectRenderer.Length; i++)
            {
                StartCoroutine(FadeRoutine(1, lowerAlpha, objectRenderer[i], null));
            }
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                StartCoroutine(FadeRoutine(1, lowerAlpha, null, canvasGroups[i]));
            }

            while (fadeOps > 0)
            {
                yield return null;
            }

            isRunning = false;
        }

        public IEnumerator FadeRoutine(float alphaIn, float alphaOut, Renderer renderer, CanvasGroup canvas)
        {
            fadeOps++;
            if (renderer != null)
            {
                float timer = 0;
                Color actualColor = renderer.material.color;
                Material[] materialList = renderer.materials;

                while (timer <= fadeDuration)
                {
                    float newAlpha = Mathf.Lerp(alphaIn, alphaOut, timer / fadeDuration);
                    foreach (Material material in materialList)
                    {
                        // Special Cases
                        if (material.name == "screen (Instance)" && alphaOut < 1)
                        {
                            MaterialUtils.SetupBlendMode(material, MaterialUtils.BlendMode.Transparent);
                        }
                        else if (material.name == "screen (Instance)")
                        {
                            MaterialUtils.SetupBlendMode(material, MaterialUtils.BlendMode.Opaque);
                        }
                        // Ignore

                        material.color = new Color(actualColor.r, actualColor.g, actualColor.b, newAlpha);
                    }

                    timer += Time.deltaTime;
                    yield return null;
                }

                foreach (Material material in materialList)
                {
                    material.color = new Color(actualColor.r, actualColor.g, actualColor.b, alphaOut);
                    fadeOps--;
                }
            }
            else if (canvas != null)
            {
                float timer = 0;

                while (timer <= fadeDuration)
                {
                    float newAlpha = Mathf.Lerp(alphaIn, alphaOut, timer / fadeDuration);
                    canvas.alpha = newAlpha;

                    timer += Time.deltaTime;
                    yield return null;
                }

                canvas.alpha = alphaOut;
            }
        }
    }
}
