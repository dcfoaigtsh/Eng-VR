using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfirmDialog : MonoBehaviour
{
    public GameObject dialogPanel;

    public void ShowDialog()
    {
        dialogPanel.SetActive(true);
    }

    public void OnConfirmYes()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnConfirmNo()
    {
        dialogPanel.SetActive(false);
    }
}
