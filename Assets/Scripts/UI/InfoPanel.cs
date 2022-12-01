using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vortices;


namespace Vortices
{
    public class InfoPanel : MonoBehaviour
    {
        // Other references
        [SerializeField] Button startButton;

        // Auxiliary References
        private SessionManager sessionManager;


        private void Start()
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            StartCoroutine(WaitForInit());
        }

        private IEnumerator WaitForInit()
        {
            yield return new WaitForSeconds(sessionManager.initializeTime + 1.0f);
            startButton.interactable = true;
        }

        public void Spawn ()
        {
            // Start the logger
            sessionManager.StartSession();
            sessionManager.loggingController.LogSessionStatus("Start");
        }

        public void Stop()
        {
            // Stop the logger
            sessionManager.loggingController.LogSessionStatus("Stop");
            sessionManager.StopSession();
        }
    }
}

