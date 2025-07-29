using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoHomeFunction : MonoBehaviour
{
    public void GoHome()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
