using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

namespace Vortices
{
    // This is a experience input controller, it lets the launcher controller know what inputs it can map to and will execute these bindings when opened    
    public class InputController : MonoBehaviour
    {
        // Data
        private string filePath;

        // Settings
        // This list must be filled with Input actions manually using the Unity inspector so they will be saved to a json file at first execution
        // Note that the "first execution" has to be made to generate this input file then uploaded as a release
        public List<InputActionReference> inputActions;


        #region Data Operation

        public void Initialize()
        {
            filePath = Application.dataPath + "/input_mapping.json";

            // Create JSON
            if (!File.Exists(filePath) || JsonChecker.IsJsonEmpty(filePath));
            {
                SaveInputActions();
            }
            // Load Bindings

        }

        private InputActionData[] GenerateInputActionData()
        {
            List<InputActionData> availableActions = new List<InputActionData>();

            foreach (var action in inputActions)
            {
                InputActionData input = new InputActionData();

                input.actionMap = action.action.actionMap.name;
                input.actionName = action.action.name;
                input.actionType = action.action.type.ToString();
                input.controlType = action.action.expectedControlType;

                availableActions.Add(input);
            }

            return availableActions.ToArray();
        }

        #endregion

        #region Persistence

        private void SaveInputActions()
        {
            InputActionBindingsData bindingsData = new InputActionBindingsData();

            bindingsData.inputActions = GenerateInputActionData();
            string json = JsonUtility.ToJson(bindingsData, true);

            File.WriteAllText(filePath, json);
        }

        #endregion
    }

    #region Persistence Classes

    [Serializable]
    public class InputActionBindingsData
    {
        public InputActionData[] inputActions;
    }

    [Serializable]
    public class InputActionData
    {
        public string actionMap;
        public string actionName;
        public string actionType;
        public string controlType;
    }

    #endregion
}