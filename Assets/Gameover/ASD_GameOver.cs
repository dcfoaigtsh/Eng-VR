using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ASD_GameOverUI : MonoBehaviour
{
    public GameObject gameOverPanel;         // 整個面板 (GameObject)
    public TextMeshProUGUI messageText;      // 顯示結束訊息的 TMP
    public Button closeButton;               // X 按鈕

    void Start()
    {
        gameOverPanel.SetActive(false); // 一開始隱藏面板
        closeButton.onClick.AddListener(() => gameOverPanel.SetActive(false));
    }

    public void ShowGameOver()
    {
         Debug.Log("📣 ShowGameOver() 被呼叫了！");
    
        if (gameOverPanel != null && messageText != null)
        {
            messageText.text = "All Order Completed!\n Thanks for your service \n Game over!";
            gameOverPanel.SetActive(true);
        }
    }
}
