using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 結算畫面管理：顯示 Accuracy、Time，並分開提供「答題率回饋」與「時間回饋」。
/// 時間門檻：
///   - ASD/ID：最佳 60–120 秒
///   - STD/SLD：最佳 180–300 秒
/// 需要：ModeManager.Instance.currentMode 與 LearningMode 列舉。
/// 讀取：PlayerPrefs("Accuracy")：0–100；PlayerPrefs("TimeSpent")：秒數
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("UI 元件（必填）")]
    public TextMeshProUGUI accuracyText;          // ex: "Accuracy: 85.0%"
    public TextMeshProUGUI timeText;              // ex: "Time Spent: 2 min 10 sec"
    public TextMeshProUGUI assistantUsageCountText; // ex: "Assistant Usage Count: 3 times"

    [Header("分離顯示回饋（必填）")]
    public TextMeshProUGUI accuracyFeedbackText;  // ex: "Accuracy Feedback: Good work—keep practicing!"
    public TextMeshProUGUI timeFeedbackText;      // ex: "Time Feedback: Great pacing for ASD/ID 👍"

    [Header("（可選）總結欄位")]
    public TextMeshProUGUI commentText;           // 若不想用可留空

    [Header("時間回饋門檻（秒）")]
    [Tooltip("ASD/ID 模式建議 60–120 秒")]
    public float asdIdOptimalMin = 60f;
    public float asdIdOptimalMax = 300f;

    [Tooltip("STD/SLD 模式建議 180–300 秒")]
    public float stdSldOptimalMin = 60f;
    public float stdSldOptimalMax = 300f;

    [Header("顏色回饋（可選）")]
    public bool useColorFeedback = true;
    public Color goodColor = new Color(0.0f, 0.6f, 0.0f);     // 深綠
    public Color warnColor = new Color(1.0f, 0.65f, 0.0f);    // 橘
    public Color badColor  = new Color(0.8f, 0.0f, 0.0f);     // 紅

    private LearningMode currentMode;

    private int assistantUsageCount;

    void Start()
    {
        // 取得模式
        currentMode = ModeManager.Instance != null
            ? ModeManager.Instance.currentMode
            : LearningMode.Standard; // 安全預設

        // 取得assistantUsageCount
        assistantUsageCount = AssistantHintController.Instance != null
            ? AssistantHintController.Instance.assistantUsageCount
            : 0;

        // 讀取數值（若沒存過就給 0）
        float acc = Mathf.Clamp(PlayerPrefs.GetFloat("Accuracy", 0f), 0f, 100f);
        float time = Mathf.Max(0f, PlayerPrefs.GetFloat("TimeSpent", 0f));

        SetResults(acc, time);
    }

    /// <summary>
    /// 對外顯示結算結果（也可供單元測試或其他流程直接呼叫）
    /// </summary>
    public void SetResults(float accuracyPercent, float timeInSeconds)
    {
        // --- 顯示數值 ---
        if (accuracyText != null)
            accuracyText.text = $"Accuracy: {accuracyPercent:F1}%";

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            timeText.text = $"Time Spent: {minutes} min {seconds} sec";
        }
        if (assistantUsageCountText != null)
            assistantUsageCountText.text = $"Assistant Usage Count: {assistantUsageCount} times";

        // --- 個別回饋 ---
        string accFeedback  = GenerateAccuracyFeedback(accuracyPercent, out Color accColor);
        string timeFeedback = GenerateTimeFeedback(currentMode, timeInSeconds, out Color timeColor);

        if (accuracyFeedbackText != null)
        {
            accuracyFeedbackText.text = accFeedback;
            if (useColorFeedback) accuracyFeedbackText.color = accColor;
        }

        if (timeFeedbackText != null)
        {
            timeFeedbackText.text = timeFeedback;
            if (useColorFeedback) timeFeedbackText.color = timeColor;
        }

        if (commentText != null)
        {
            // 若想顯示總結就合併（可依需調整）
            commentText.text = $"{accFeedback}\n{timeFeedback}";
        }
    }

    /// <summary>
    /// 答題率回饋（僅看第一次作答的正確率，建議在紀錄 Accuracy 時已處理）
    /// </summary>
    private string GenerateAccuracyFeedback(float accuracy, out Color color)
    {
        if (accuracy >= 90f)
        {
            color = goodColor;
            return "Accuracy Feedback: Excellent accuracy! ";
        }
        else if (accuracy >= 75f)
        {
            color = warnColor;
            return "Accuracy Feedback: Good work—keep practicing!";
        }
        else if (accuracy >= 60f)
        {
            color = warnColor;
            return "Accuracy Feedback: Getting there—review tricky items.";
        }
        else
        {
            color = badColor;
            return "Accuracy Feedback: Keep trying—focus on key words and hints.";
        }
    }

    /// <summary>
    /// 依模式（ASD/ID vs STD/SLD）提供時間回饋
    /// </summary>
    private string GenerateTimeFeedback(LearningMode mode, float timeSec, out Color color)
    {
        float minOptimal, maxOptimal;
        bool isAsdId = (mode == LearningMode.ASD || mode == LearningMode.ID);

        if (isAsdId)
        {
            minOptimal = asdIdOptimalMin;
            maxOptimal = asdIdOptimalMax;

            if (timeSec < minOptimal)
            {
                color = warnColor;
                return "Time Feedback: Very fast—wonderful performance!";
            }
            else if (timeSec <= maxOptimal)
            {
                color = goodColor;
                return "Time Feedback: Great practice, keep it up!";
            }
            else if (timeSec <= maxOptimal + 60f) // 2–3 分
            {
                color = warnColor;
                return "Time Feedback: A bit slow—try to keep steady focus.";
            }
            else
            {
                color = badColor;
                return "Time Feedback: Consider simplifying steps to speed up.";
            }
        }
        else // STD 或 SLD
        {
            minOptimal = stdSldOptimalMin;
            maxOptimal = stdSldOptimalMax;

            if (timeSec < minOptimal)
            {
                color = warnColor;
                return "Time Feedback: Fast—make sure you didn’t miss details.";
            }
            else if (timeSec <= maxOptimal)
            {
                color = goodColor;
                return "Time Feedback: Great practice, keep it up!";
            }
            else if (timeSec <= maxOptimal + 120f) // 5–7 分
            {
                color = warnColor;
                return "Time Feedback: Slightly slow—use cues and previews.";
            }
            else
            {
                color = badColor;
                return "Time Feedback: Try chunking steps to improve speed.";
            }
        }
    }

    // ===== Buttons =====
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
