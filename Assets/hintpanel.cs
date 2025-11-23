using UnityEngine;
using TMPro;

public class AssistantHintControllerr : MonoBehaviour
{
    public GameObject hintPanel;
    public TextMeshProUGUI hintText;

    public void ShowHint(string message)
    {
        if (hintPanel != null && hintText != null)
        {
            hintText.text = message;
            hintPanel.SetActive(true);
        }
    }

    public void HideHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }
}
