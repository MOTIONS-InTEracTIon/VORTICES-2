using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vuplex.WebView;
using UnityEditor;

namespace Vortices
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public FadeScreen fadeScreen;
        private float blackScreenDuration = 2.0f;
        public int sceneTarget { get; set; }

        private void Start()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.quitting += OnQuitting;
            #endif
        }

        public void GoToScene()
        {
            StartCoroutine(GoToSceneRoutine());
        }

        IEnumerator GoToSceneRoutine()
        {
            fadeScreen.FadeOut();

            //Launch new scene
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneTarget);
            operation.allowSceneActivation = false;

            float timer = 0;
            while(timer <= fadeScreen.fadeDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            timer = 0;
            while(timer <= blackScreenDuration && !operation.isDone)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            operation.allowSceneActivation = true;
        }

        // Handle application exit
        private async void OnApplicationQuit()
        {
            await StandaloneWebView.TerminateBrowserProcess();
        }


        private void OnQuitting()
        {
            StartCoroutine(ClearWebData());
        }

        private IEnumerator ClearWebData()
        {
            yield return StartCoroutine(StandaloneWebView.TerminateBrowserProcess().AsIEnumerator());
        }
    }
}
