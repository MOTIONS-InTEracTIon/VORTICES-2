using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Vortices
{
    public class TutorialPanel : MonoBehaviour
    {
        // Component
        public Transform originHeadTransform;
        public List<GameObject> tutorialDialogs;

        // Settings
        public float smoothTime = 0.5f;
        public float distanceFromHead = 2.0f;

        // Data
        private Vector3 velocity = Vector3.zero;
        private Vector3 targetPosition;
        private Vector3 initialOffset;
        private int actualCardId;
        private bool categorizedFirst;
        private bool otherSelected;

        // Input Data
        public InputActionReference selectButton;

        // Coroutine
        public bool isCardChangeRunning;

        #region Initialize

        private void OnEnable()
        {
            // Subscribe to events
            selectButton.action.performed += NextCardSelect;
            MoveGizmo.onElementsMoved += NextCardEvent;
            RighthandTools.onCategorized += NextCardEvent;
            Element.onSelected += NextCardEvent;
            MuseumFloor.teleportedFloor += NextCardEvent;

        }

        private void OnDisable()
        {
            // Unsubscribe to events
            selectButton.action.performed -= NextCardSelect;
            MoveGizmo.onElementsMoved -= NextCardEvent;
            RighthandTools.onCategorized -= NextCardEvent;
            Element.onSelected -= NextCardEvent;
            MuseumFloor.teleportedFloor -= NextCardEvent;
        }

        public void Initialize(GameObject originHead)
        {
            originHeadTransform = originHead.transform;
            initialOffset = transform.position - originHeadTransform.position;
            actualCardId = -1;
            selectButton.action.Enable();


            ChangeVisibleComponent(0);
        }

        private void Update()
        {
            // Position of object away from head
            Vector3 offset = originHeadTransform.forward * distanceFromHead;
            targetPosition = originHeadTransform.position + offset;

            // Face the camera 
            transform.LookAt(originHeadTransform);

            // Follow rotation of camera
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        #endregion

        #region Data Operations

        virtual public void NextCardSelect(InputAction.CallbackContext context)
        {
            if (isCardChangeRunning)
            {
                return;
            }

            switch (actualCardId)
            {
                case 5:
                    ChangeVisibleComponent(actualCardId + 1);
                    SessionManager.instance.spawnController.StartSession(false, null);
                    SessionManager.instance.loggingController.LogSessionStatus("Start");
                    return;
                case 7:
                    // Needs user to move the elements 
                case 9:
                    // Needs user to select an element with A
                case 11:
                case 12:
                    // Needs user to categorize an element using the select button
                case 13:
                    // Needs user to press the exit button
                    return;
                default:
                    ChangeVisibleComponent(actualCardId + 1);
                    return;
            }
        }

        public void NextCardEvent(object sender, EventArgs e)
        {
            if (isCardChangeRunning)
            {
                return;
            }

            if (actualCardId == 7 && (sender is MoveGizmo || sender is MuseumFloor))
            {
                ChangeVisibleComponent(actualCardId + 1);
            }

            if (actualCardId == 9 && sender is Element)
            {
                ChangeVisibleComponent(actualCardId + 1);
            }

            if (actualCardId == 11 && sender is RighthandTools)
            {
                ChangeVisibleComponent(actualCardId + 1);
                categorizedFirst = true;
            }

            if (actualCardId == 12 && sender is Element && categorizedFirst)
            {
                otherSelected = true;
            }

            if (actualCardId == 12 && sender is RighthandTools && otherSelected)
            {
                ChangeVisibleComponent(actualCardId + 1);
            }
        }

        public void StopTutorial()
        {
            SessionManager.instance.loggingController.LogSessionStatus("Stop");
            StartCoroutine(SessionManager.instance.spawnController.StopSession());
        } 

        #endregion

        #region Tutorial switching

        // UI Cards will change according to their goals one by one
        public void ChangeVisibleComponent(int componentId)
        {
            StartCoroutine(ChangeComponent(componentId));
        }
        private IEnumerator ChangeComponent(int componentId)
        {
            isCardChangeRunning = true;
            // FadeOut actual component
            if(actualCardId != -1)
            {
                FadeUI actualCardFader = tutorialDialogs[actualCardId].GetComponent<FadeUI>();
                yield return StartCoroutine(actualCardFader.FadeOut());
                actualCardFader.gameObject.SetActive(false);

            }
            actualCardId = componentId;
            // FadeIn new component
            tutorialDialogs[componentId].gameObject.SetActive(true);
            FadeUI newComponentFader = tutorialDialogs[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
            yield return new WaitForSeconds(1);
            isCardChangeRunning = false;
        }

        #endregion
    }
}

