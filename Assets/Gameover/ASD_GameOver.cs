using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ASD_GameOverUI : GameOverUI
{
    public GameObject gameOverPanel;         // 整個面板 (GameObject)
    public Button closeButton;               // X 按鈕
    public Button reviewButton;              // 新增的「Review Vocabulary」按鈕

    void Start()
    {
        gameOverPanel.SetActive(false); // 一開始隱藏面板
        closeButton.onClick.AddListener(() => gameOverPanel.SetActive(false));
        reviewButton.onClick.AddListener(OnReviewButtonClick); // 綁定 review 按鈕事件
        closeButton.gameObject.SetActive(false); 
    }

    public void ShowGameOver()
    {
        Debug.Log("📣 ShowGameOver() 被呼叫了！");

        if (gameOverPanel != null && messageText != null)
        {
            messageText.text = "All Orders Completed!\nThanks for your service.\nWould you like to review the words again?";
            gameOverPanel.SetActive(true);

        }
        closeButton.gameObject.SetActive(true);
        reviewButton.gameObject.SetActive(true); // 顯示 review 按鈕 
    }

    void OnReviewButtonClick()
    {
        // 設定是複習模式（可用 PlayerPrefs）
        PlayerPrefs.SetInt("IsReviewMode", 1);
        SceneManager.LoadScene("WordLearning"); // 替換為你實際的字卡場景名稱
    }
}
