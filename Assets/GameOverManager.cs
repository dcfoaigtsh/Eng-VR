using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI 元件")]
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI commentText;

    private LearningMode currentMode;

    void Start()
    {
        // 取得目前模式
        currentMode = ModeManager.Instance.currentMode;

        // 顯示分數資料（從 PlayerPrefs 讀取）
        float acc = PlayerPrefs.GetFloat("Accuracy", 100f);
        float time = PlayerPrefs.GetFloat("TimeSpent", 320f);

        SetResults(acc, time);
    }

    public void SetResults(float accuracy, float timeInSeconds)
    {
        accuracyText.text = $"Accuracy:{accuracy:F1}%";

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        timeText.text = $"Time Spent:{minutes} min {seconds} sec";

        commentText.text = GenerateComment(accuracy, timeInSeconds);
    }

    private string GenerateComment(float accuracy, float time)
    {
        if (accuracy >= 90f && time < 90f)
            return "Awesome! You are fast and accurate!";
        else if (accuracy >= 80f)
            return "Good job! Keep it up!";
        else
            return "Try again! You will get better!";
    }

    public void OnPlayAgain()
    {
        switch (currentMode)
        {
            case LearningMode.Standard:
                SceneManager.LoadScene("Standard");
                break;
            case LearningMode.ASD:
                SceneManager.LoadScene("ASDmode");
                break;
            case LearningMode.ID:
                SceneManager.LoadScene("IDmode");
                break;
            case LearningMode.SLD:
                SceneManager.LoadScene("SLDmode");
                break;
            default:
                Debug.LogError("無法判斷模式，回到主畫面");
                SceneManager.LoadScene("MainMenu");
                break;
        }
    }

    public void OnReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
