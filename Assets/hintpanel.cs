using UnityEngine;
using TMPro;

public class AssistantHintController : MonoBehaviour
{
    public GameObject hintPanel;
    public TextMeshProUGUI hintText;

    public void ShowHint(string message)
    {
        if (hintPanel != null && hintText != null)
        {
            hintText.text = "Let’s look at the menu again!";
            hintPanel.SetActive(true);
        }
    }

    public void HideHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }
}
