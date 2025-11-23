using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class AssistantHintController : MonoBehaviour
{
    [Header("UI Components")]
    public Button AssistantImage;            // 打開面板的 Image 按鈕
    public GameObject AssistantHintPanel;    // 主要提示面板
    public Button X_Button;                  // 關閉按鈕 (X)
    public GameObject OutputPanel;           // 輸出面板
    public TextMeshProUGUI Text;             // 輸出文字 (TMP)
    public Button OkButton;                  // 確定按鈕
    public Button AgainButton;               // 重新解釋按鈕
    public GameObject QuestionPanel;         // 問題選擇面板
    public Button Q1Button;                  // 問題1按鈕
    public Button Q2Button;                  // 問題2按鈕  
    public Button Q3Button;                  // 問題3按鈕
    public Button Q4Button;                  // 問題4按鈕

    [Header("OpenAI Configuration")]
    public string openAIKey;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    [Header("Debug Settings")]
    public bool showDebugInfo = true;

    // 遊戲元件參考
    private STD_Gameflow gameflow;
    private STD_SingleCustomer[] allCustomers;
    private STD_QAmanager qaManager;
    private STD_GameDescription gameDescription;

    // HTTP 客戶端
    private HttpClient client;
    private bool isWaitingResponse = false;

    // 玩家狀態
    private PlayerState currentPlayerState;
    private string currentDialogueText = "";
    private string currentOrder = "";
    private float accuracy = 0f;
    private LearningMode learningMode = LearningMode.Standard;
    private string gameDescriptionText = "";
    private int currentDescriptionPage = 0;
    private string lastSelectedOption = "";

    void Awake()
    {
        if (!string.IsNullOrEmpty(openAIKey))
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + openAIKey);
        }
    }

    void Start()
    {
        FindGameComponents();
        
        // 綁定按鈕事件
        X_Button.onClick.AddListener(HideHintPanel);
        OkButton.onClick.AddListener(OnOkButtonClicked);
        AgainButton.onClick.AddListener(OnAgainButtonClicked);
        
        Q1Button.onClick.AddListener(() => OnQuestionButtonClicked(1));
        Q2Button.onClick.AddListener(() => OnQuestionButtonClicked(2));
        Q3Button.onClick.AddListener(() => OnQuestionButtonClicked(3));
        Q4Button.onClick.AddListener(() => OnQuestionButtonClicked(4));
        
        SetupAssistantImage();
        
        if (AssistantHintPanel != null)
            AssistantHintPanel.SetActive(false);
        
        StartCoroutine(PeriodicCheck());
    }

    IEnumerator PeriodicCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); // 每分鐘檢查一次
            
            if (AssistantHintPanel != null && !AssistantHintPanel.activeInHierarchy)
            {
                UpdatePlayerStateFromExternal();
                // 如果玩家卡在閱讀說明狀態，自動提供幫助
                if (currentPlayerState == PlayerState.ReadingDescription)
                {
                    if (showDebugInfo)
                        Debug.Log("🕒 定時檢查：玩家在閱讀說明，顯示提示面板");
                    ShowHintPanel();
                }
            }
        }
    }

    void SetupAssistantImage()
    {
        if (AssistantImage != null)
        {
            Button imageButton = AssistantImage.GetComponent<Button>();
            if (imageButton == null)
            {
                imageButton = AssistantImage.gameObject.AddComponent<Button>();
                imageButton.transition = Selectable.Transition.None;
            }
            imageButton.onClick.AddListener(OnAssistantImageClicked);
            return;
        }
        
        GameObject imageObj = GameObject.Find("AssistantImage");
        if (imageObj != null)
        {
            Button imageButton = imageObj.GetComponent<Button>();
            if (imageButton == null)
            {
                imageButton = imageObj.AddComponent<Button>();
                imageButton.transition = Selectable.Transition.None;
            }
            imageButton.onClick.AddListener(OnAssistantImageClicked);
            AssistantImage = imageButton;
        }
    }

    void FindGameComponents()
    {
        gameflow = FindObjectOfType<STD_Gameflow>();
        allCustomers = FindObjectsOfType<STD_SingleCustomer>();
        qaManager = FindObjectOfType<STD_QAmanager>();
        gameDescription = FindObjectOfType<STD_GameDescription>();
        
        if (gameflow != null)
        {
            gameflow.OnPlayerStateChanged += OnPlayerStateChanged;
        }
    }

    void OnPlayerStateChanged(PlayerState newState)
    {
        currentPlayerState = newState;
        if (showDebugInfo)
            Debug.Log($"🔄 收到狀態更新: {newState}");
    }

    void Update()
    {
        UpdatePlayerStateFromExternal();
        CheckGameDescriptionText();
    }

    void UpdatePlayerStateFromExternal()
    {
        if (gameflow != null)
        {
            currentPlayerState = gameflow.currentPlayerState;
            accuracy = gameflow.GetAccuracyPercent();
        }
        
        // 從活躍顧客獲取對話和訂單資訊
        STD_SingleCustomer activeCustomer = GetActiveCustomer();
        if (activeCustomer != null && activeCustomer.statementText != null)
        {
            currentDialogueText = activeCustomer.statementText.text;
            currentOrder = ExtractOrderFromQuestion(currentDialogueText);
        }
        
        // 從 QA 管理器獲取對話
        if (qaManager != null && qaManager.gameObject.activeInHierarchy && qaManager.statementText != null)
        {
            currentDialogueText = qaManager.statementText.text;
        }
        
        if (ModeManager.Instance != null)
        {
            learningMode = ModeManager.Instance.currentMode;
        }
    }

    void CheckGameDescriptionText()
    {
        gameDescription = FindObjectOfType<STD_GameDescription>();
        if (currentPlayerState == PlayerState.ReadingDescription && gameDescription != null)
        {
            gameDescriptionText = "";
            currentDescriptionPage = gameDescription.currentInfo;
            if (gameDescription.InfoContent != null)
            {
                gameDescriptionText = gameDescription.InfoContent.text;
            }
            if (currentDescriptionPage == 2 || currentDescriptionPage == 3)
            {
                gameDescriptionText = "當前是圖像說明頁面，展示遊戲操作指引";
            }
        }
    }

    STD_SingleCustomer GetActiveCustomer()
    {
        if (allCustomers != null)
        {
            foreach (var customer in allCustomers)
            {
                if (customer != null && customer.gameObject.activeInHierarchy)
                {
                    return customer;
                }
            }
        }
        return null;
    }

    string ExtractOrderFromQuestion(string question)
    {
        if (string.IsNullOrEmpty(question)) return "未知訂單";
        
        question = question.ToLower();
        
        if (question.Contains("burger") || question.Contains("漢堡")) return "漢堡";
        if (question.Contains("coffee") || question.Contains("咖啡")) return "咖啡";
        if (question.Contains("pizza") || question.Contains("披薩")) return "披薩";
        if (question.Contains("sandwich") || question.Contains("三明治")) return "三明治";
        if (question.Contains("fries") || question.Contains("薯條")) return "薯條";
        if (question.Contains("cola") || question.Contains("可樂")) return "可樂";
        if (question.Contains("ice cream") || question.Contains("冰淇淋")) return "冰淇淋";
        if (question.Contains("salad") || question.Contains("沙拉")) return "沙拉";
        
        return "訂單處理中";
    }

    public void OnAssistantImageClicked()
    {
        ShowHintPanel();
    }

    public void ShowHintPanel()
    {
        if (AssistantHintPanel != null)
        {
            AssistantHintPanel.SetActive(true);
            SwitchToQuestionPanel();
            
            // 根據當前狀態顯示不同的問候語
            Text.text = GetGreetingByState();
        }
    }

    string GetGreetingByState()
    {
        switch (currentPlayerState)
        {
            case PlayerState.ReadingDescription:
                return $"正在閱讀遊戲說明（第{currentDescriptionPage + 1}頁）？需要什麼幫助？";
            case PlayerState.MovingToCustomer:
                return "正在前往顧客的路上？需要指引嗎？";
            case PlayerState.TalkingToCustomer:
                return "正在與顧客對話？遇到什麼問題？";
            case PlayerState.MovingToStaff:
                return "正在前往員工點餐？需要幫助嗎？";
            case PlayerState.OrderingAtStaff:
                return "正在與員工點餐？有什麼困難？";
            case PlayerState.ReturningToCustomer:
                return "正在返回顧客交餐？需要協助嗎？";
            case PlayerState.Completed:
                return "任務已完成！還有其他問題嗎？";
            default:
                return "需要幫忙嗎？請選擇一個問題：";
        }
    }

    void SwitchToQuestionPanel()
    {
        if (QuestionPanel != null) QuestionPanel.SetActive(true);
        if (OutputPanel != null) OutputPanel.SetActive(false);
        UpdateQuestionButtons();
    }

    void SwitchToOutputPanel()
    {
        if (QuestionPanel != null) QuestionPanel.SetActive(false);
        if (OutputPanel != null) OutputPanel.SetActive(true);
    }

    void UpdateQuestionButtons()
    {
        // 選項1: 不知道下一步 - 始終顯示
        Q1Button.GetComponentInChildren<TextMeshProUGUI>().text = "我不知道再來要做什麼？";
        Q1Button.gameObject.SetActive(true);
        
        // 選項2: 一直答錯 - 只有在點餐階段且答錯時顯示
        bool canShowWrongOption = (currentPlayerState == PlayerState.OrderingAtStaff) && (accuracy < 100f);
        Q2Button.gameObject.SetActive(canShowWrongOption);
        if (canShowWrongOption)
        {
            Q2Button.GetComponentInChildren<TextMeshProUGUI>().text = "這裡一直答錯";
        }
        
        // 選項3: 看不懂英文 - 在有對話內容時顯示（包括遊戲說明）
        bool hasTextContent = !string.IsNullOrEmpty(currentDialogueText) || !string.IsNullOrEmpty(gameDescriptionText);
        bool canShowTranslation = hasTextContent && 
                                 (currentPlayerState == PlayerState.ReadingDescription ||
                                  currentPlayerState == PlayerState.TalkingToCustomer ||
                                  currentPlayerState == PlayerState.OrderingAtStaff);
        Q3Button.gameObject.SetActive(canShowTranslation);
        if (canShowTranslation)
        {
            Q3Button.GetComponentInChildren<TextMeshProUGUI>().text = "看不懂他在說什麼？";
        }
        
        // 選項4: 忘記訂單 - 在需要記住訂單的階段顯示
        bool canShowForgetOrder = (currentPlayerState == PlayerState.MovingToStaff || 
                                  currentPlayerState == PlayerState.OrderingAtStaff) && 
                                  !string.IsNullOrEmpty(currentOrder);
        Q4Button.gameObject.SetActive(canShowForgetOrder);
        if (canShowForgetOrder)
        {
            Q4Button.GetComponentInChildren<TextMeshProUGUI>().text = "我忘記客人點什麼了";
        }
    }

    void OnQuestionButtonClicked(int questionNumber)
    {
        switch (questionNumber)
        {
            case 1: lastSelectedOption = "next_step"; break;
            case 2: lastSelectedOption = "keep_wrong"; break;
            case 3: lastSelectedOption = "not_understand"; break;
            case 4: lastSelectedOption = "forgot_order"; break;
        }

        string prompt = GeneratePrompt(lastSelectedOption);
        
        if (showDebugInfo)
        {
            Debug.Log(prompt);
        }
        
        StartCoroutine(SendToOpenAI(prompt));
        SwitchToOutputPanel();
        Text.text = "AI助手思考中...";
        SetButtonsInteractable(false);
    }

    string GeneratePrompt(string option)
    {
        string prompt = $"【遊戲情境】快餐店英文學習模擬 - {learningMode}模式\n";
        prompt += $"【玩家狀態】{currentPlayerState}\n";
        prompt += $"【答對率】{accuracy:F1}%\n";
        
        // 根據狀態提供不同的文字內容
        if (currentPlayerState == PlayerState.ReadingDescription)
        {
            prompt += $"【當前頁面】第{currentDescriptionPage + 1}頁\n";
            if (!string.IsNullOrEmpty(gameDescriptionText))
            {
                prompt += $"【遊戲說明】{gameDescriptionText}\n";
            }
            else
            {
                prompt += $"【遊戲說明】圖像說明頁面\n";
            }
        }
        else if (!string.IsNullOrEmpty(currentDialogueText))
        {
            prompt += $"【當前對話】{currentDialogueText}\n";
        }
        
        if (!string.IsNullOrEmpty(currentOrder))
        {
            prompt += $"【顧客訂單】{currentOrder}\n";
        }
        
        prompt += $"\n【玩家問題】";
        
        Debug.Log($"選擇的問題選項: {option}");
        switch (option)
        {
            case "next_step":
                prompt += GetNextStepGuidance();
                break;
            case "keep_wrong":
                prompt += GetWrongAnswerGuidance();
                break;
            case "not_understand":
                prompt += GetTranslationGuidance();
                break;
            case "forgot_order":
                prompt += GetForgotOrderGuidance();
                break;
        }
        
        prompt += "\n\n請用中文回答，簡潔明瞭，最多50字。";
        return prompt;
    }

    string GetNextStepGuidance()
    {
        switch (currentPlayerState)
        {
            case PlayerState.ReadingDescription:
                if (currentDescriptionPage < 4) // 如果不是最後一頁
                {
                    return "玩家不知道看完當前說明頁面後該做什麼。請指引玩家『點擊 Next 按鈕繼續閱讀下一頁說明』。";
                }
                else // 最後一頁
                {
                    return "玩家不知道看完遊戲說明後該做什麼。請指引玩家『點擊 Close 按鈕關閉說明，然後跟著地上的箭頭走，前往顧客那裡』。";
                }
            case PlayerState.MovingToCustomer:
                return "玩家正在走向顧客但不知道該做什麼。請指引玩家『點擊顧客頭上的驚嘆號開始對話』。";
            case PlayerState.TalkingToCustomer:
                return "玩家正在與顧客對話但不知道下一步。請指引玩家『仔細聽顧客的需求，記住訂單內容，然後跟著箭頭走向員工點餐』。";
            case PlayerState.MovingToStaff:
                return "玩家正在走向員工但不知道該做什麼。請指引玩家『跟著箭頭走到員工面前，點擊員工開始點餐』。";
            case PlayerState.OrderingAtStaff:
                return "玩家正在與員工點餐但不知道該做什麼。請指引玩家『選擇正確的餐點選項來完成點餐』。";
            case PlayerState.ReturningToCustomer:
                return "玩家正在返回顧客但不知道該做什麼。請指引玩家『跟著箭頭走回顧客那裡交餐』。";
            default:
                return "玩家不知道下一步該做什麼。請根據當前狀態提供具體指引。";
        }
    }

    string GetWrongAnswerGuidance()
    {
        return "玩家在點餐時一直答錯。請鼓勵玩家並建議：『沒關係！請跟著地上的箭頭往回走，再問一次顧客他想要什麼，仔細記住訂單內容。』";
    }

    string GetTranslationGuidance()
    {
        // 優先使用遊戲說明文字，如果沒有則使用對話文字
        string textToTranslate = currentPlayerState == PlayerState.ReadingDescription ? 
                                gameDescriptionText : currentDialogueText;
        if (!string.IsNullOrEmpty(textToTranslate))
        {
            return $"玩家看不懂英文內容。請將這句話翻譯成中文並簡單解釋：『{textToTranslate}』";
        }
        else if (currentPlayerState == PlayerState.ReadingDescription && (currentDescriptionPage == 2 || currentDescriptionPage == 3))
        {
            return "玩家看不懂圖像說明頁面。請用中文描述圖像中展示的遊戲操作指引。";
        }
        return "玩家看不懂當前內容。請用中文解釋當前情境。";
    }

    string GetForgotOrderGuidance()
    {
        return "玩家忘記客人點什麼了。請建議：『請跟著地上的箭頭往回走，再問一次顧客他點的餐是什麼。』";
    }

    IEnumerator SendToOpenAI(string prompt)
    {
        if (isWaitingResponse || client == null) yield break;
        isWaitingResponse = true;

        var task = SendMessageToOpenAIAsync(prompt);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCompletedSuccessfully)
        {
            string response = task.Result;
            Text.text = $"AI助手:\n{response}";
        }
        else
        {
            Text.text = "AI助手: 抱歉，發生錯誤，請稍後再試。";
        }

        isWaitingResponse = false;
        SetButtonsInteractable(true);
    }

    // 其他方法保持不變...
    async Task<string> SendMessageToOpenAIAsync(string prompt)
    {
        try
        {
            var json = new JObject
            {
                ["model"] = "gpt-3.5-turbo",
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = "你是快餐店英文學習遊戲的AI助手。用友善、鼓勵的語氣，用繁體中文回答，保持簡潔（最多50字）。" },
                    new JObject { ["role"] = "user", ["content"] = prompt }
                },
                ["temperature"] = 0.7,
                ["max_tokens"] = 100
            };

            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(result);
            return data["choices"][0]["message"]["content"].ToString().Trim();
        }
        catch (System.Exception e)
        {
            Debug.LogError("OpenAI API 錯誤: " + e.Message);
            return "抱歉，發生錯誤，請稍後再試。";
        }
    }

    void SetButtonsInteractable(bool interactable)
    {
        OkButton.interactable = interactable;
        AgainButton.interactable = interactable;
        Q1Button.interactable = interactable;
        Q2Button.interactable = interactable;
        Q3Button.interactable = interactable;
        Q4Button.interactable = interactable;
    }

    void OnOkButtonClicked()
    {
        SwitchToQuestionPanel();
        HideHintPanel();
    }

    void OnAgainButtonClicked()
    {
        if (!string.IsNullOrEmpty(lastSelectedOption))
        {
            string prompt = GeneratePrompt(lastSelectedOption) + " 請用更詳細、更直白的方式重新解釋。";
            StartCoroutine(SendToOpenAI(prompt));
            Text.text = "重新解釋中...";
            SetButtonsInteractable(false);
        }
    }

    public void HideHintPanel()
    {
        if (AssistantHintPanel != null)
            AssistantHintPanel.SetActive(false);
    }

    void OnDestroy()
    {
        client?.Dispose();
        if (gameflow != null)
            gameflow.OnPlayerStateChanged -= OnPlayerStateChanged;
    }
}