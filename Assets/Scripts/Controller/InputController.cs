using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

namespace Vortices
{
    public enum Mode
    {
        VrController = 0,      
    }

    // This is a experience input controller, it lets the launcher controller know what inputs it can map to and will execute these bindings when opened    
    public class InputController : MonoBehaviour
    {
        // Data
        private string filePath;
        private List<InputActionModeData> inputActionModeData;

        // Settings
        // This list will be filled with all input actions from the Asset, different assets means different modes
        // Note that the "first execution" has to be made to generate this input file then uploaded as a release
        public List<InputActionAsset> inputActionAssets;

        #region Data Operation
        public void Initialize()
        {
            // Create JSON
            filePath = Path.GetDirectoryName(Application.dataPath) + "/input_mapping.json";

            // Create file to communicate with launcher if there is none
            SaveInputActions();
            // Load Bindings
            LoadInputActions();
            // Override Bindings
            OverrideBindings();
        }

        #endregion

        #region Sending Input Action Data to Launcher

        private List<InputActionModeData> GenerateInputActionModeData()
        {
            // Generates one InputActionMode for each control mode implemented in the application
            List<InputActionModeData> allInputModeData = new List<InputActionModeData>();

            // VORTICES-2 has two modes
            // First mode is VR Controller 

            InputActionModeData vrControllerMode = new InputActionModeData();
            vrControllerMode.modeName = Enum.GetName(typeof(Mode), Mode.VrController);
            vrControllerMode.inputActions = GenerateInputActionData((int) Mode.VrController);
            allInputModeData.Add(vrControllerMode);

            // Insert second mode...

            return allInputModeData;
        }

        private List<InputActionData> GenerateInputActionData(int assetId)
        {
            List<InputActionData> availableActions = new List<InputActionData>();
            
            List<InputAction> inputActions = GetAllInputActionFromAsset(inputActionAssets[assetId]);

            foreach (InputAction inputAction in inputActions)
            {
                InputActionData input = new InputActionData();

                input.actionMap = inputAction.actionMap.name;
                input.actionName = inputAction.name;
                input.actionType = inputAction.type.ToString();
                input.controlType = inputAction.expectedControlType;
                input.resultPathBinding = "Default";

                availableActions.Add(input);
            }

            return availableActions;
        }

        private List<InputAction> GetAllInputActionFromAsset(InputActionAsset asset)
        {
            List<InputAction> inputActions = new List<InputAction>();
            if(asset == null)
            {
                Debug.LogError("InputActionAsset is not assigned.");
                return null;
            }

            foreach (var map in asset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    inputActions.Add(action);
                }
            }

            return inputActions;
        }

        #endregion

        #region Recieving Input Action Data from Launcher

        private void OverrideBindings()
        {
            if(inputActionModeData == null)
            {
                return;
            }

            // VORTICES-2 has two modes
            // First mode is VR Controller 
            InputActionModeData vrControllerMode = GetModeInputActionData(Enum.GetName(typeof(Mode), (int)Mode.VrController));
            List<InputAction> vrControllerInputActions = GetAllInputActionFromAsset(inputActionAssets[(int)Mode.VrController]);
            // Apply bindings
            OverrideModeBindings(vrControllerInputActions, vrControllerMode.inputActions);

            // Insert second mode...
        }

        private InputActionModeData GetModeInputActionData (string modeName)
        {
            foreach(InputActionModeData modeData in inputActionModeData) 
            {
                if(modeData.modeName == modeName)
                {
                    return modeData;
                }
            }

            return null;
        }

        private void OverrideModeBindings(List<InputAction> modeInputActions, List<InputActionData> overrideInputActions)
        {
            if(overrideInputActions == null)
            {
                return;
            }

            foreach(InputActionData action in overrideInputActions)
            {
                if(action.resultPathBinding == "Default")
                {
                    continue;
                }

                // Search for the original InputAction to override
                InputAction inputAction = GetInputActionFromList(modeInputActions, action);
                // Format the path in the overriding action
                List<string> path = FormatBindingPath(action.resultPathBinding);
                // Override original action
                ChangeBindingPath(inputAction, path[0], path[1]);
            }
        }

        private InputAction GetInputActionFromList(List<InputAction> inputActions, InputActionData inputAction)
        {
            foreach (InputAction action in inputActions)
            {
                if (action.actionMap.name == inputAction.actionMap &&
                    action.name == inputAction.actionName &&
                    action.type.ToString() == inputAction.actionType &&
                    action.expectedControlType == inputAction.controlType)
                {
                    return action;
                }
            }

            return null;
        }

        private List<string> FormatBindingPath(string resultPathBinding)
        {
            List<string> result = new List<string>();

            // Split the string
            string[] pathParts = resultPathBinding.Split('/');

            if (pathParts.Length >= 2)
            {
                string device = pathParts[0];
                string path = string.Join("/", pathParts, 1, pathParts.Length - 1);

                result.Add(device);
                result.Add(path);
            }

            return result;
        }

        private void ChangeBindingPath(InputAction action, string deviceName, string bindingPath)
        {
            if(action.bindings.Count > 0)
            {
                InputBinding originalBinding = action.bindings[0];

                originalBinding.overridePath = $"<{deviceName}>/{bindingPath}";
                //action.ApplyBindingOverride(0, $"<{deviceName}>/{bindingPath}");
                //TEST OVERRIDE AVERIGUA UNA FORMA DE CONECTAR BIEN ESTO, SIN ESTO, NO VAS A PODER CREAR ACCIONES CUSTOM COMO MOVER IMAGENES Y ETCS
                action.started += Test;
            }
        }

        public void Test(InputAction.CallbackContext context)
        {
            Debug.Log("La tecla fue presionada" + context.action.bindings[0].path);
        }


        #endregion

        #region Persistence

        private void SaveInputActions()
        {
            if (!File.Exists(filePath) || JsonChecker.IsJsonEmpty(filePath))
            {
                InputActionBindingsData bindingsData = new InputActionBindingsData();

                bindingsData.allInputActionModeData = GenerateInputActionModeData();
                string json = JsonUtility.ToJson(bindingsData, true);

                File.WriteAllText(filePath, json);
            }
        }

        private void LoadInputActions()
        {
            if (!File.Exists(filePath) || JsonChecker.IsJsonEmpty(filePath))
            {
                return;
            }
            
            string json = File.ReadAllText(filePath);

            // Load all data to overwrite

            inputActionModeData = JsonUtility.FromJson<InputActionBindingsData>(json).allInputActionModeData;
        }

        #endregion
    }

    #region Persistence Classes

    [Serializable]
    public class InputActionBindingsData
    {
        public List<InputActionModeData> allInputActionModeData;
    }

    [Serializable]
    public class InputActionModeData
    {
        public string modeName;
        public List<InputActionData> inputActions;
    }

    [Serializable]
    public class InputActionData
    {
        public string actionMap;
        public string actionName;
        public string actionType;
        public string controlType;
        public string resultPathBinding;
    }

    #endregion
}