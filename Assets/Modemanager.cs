using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LearningMode
{
    Standard,
    ASD,
    ID,
    SLD
}

public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance;
    public LearningMode currentMode;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetMode(int modeIndex)
    {
        currentMode = (LearningMode)modeIndex;
    }
    
}
