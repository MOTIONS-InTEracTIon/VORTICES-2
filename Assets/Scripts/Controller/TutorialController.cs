using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    // Components
    //[SerializeField] private TutorialDialog tutorialDialog;

    
    // Data

    public static TutorialController instance;

    #region Initialize
    public void Initialize()
    {
        // Instance initializing
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

    }
    #endregion


}
