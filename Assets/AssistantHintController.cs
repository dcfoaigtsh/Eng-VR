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
    private Gameflow gameflow;
    private SingleCustomer[] allCustomers;
    private QAmanager[] allQAmanagers;
    private GameDescription gameDescription;
    private GameOverUI gameOverUI;

    // HTTP 客戶端
    private HttpClient client;
    private bool isWaitingResponse = false;

    // 玩家相關狀態
    private LearningMode learningMode = LearningMode.Standard;    // 學習模式
    private PlayerState currentPlayerState;                       // 目前玩家狀態
    private int currentDialoguePage = 0;                          // 目前對話頁面
    private string currentDialogueText = "";                      // 目前對話文字

    // 上次選擇的問題選項
    private string lastSelectedOption = "";

    void Awake()
    {
        // 初始化 HTTP 客戶端
        if (!string.IsNullOrEmpty(openAIKey))
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + openAIKey);
        }
    }

    void Start()
    {
        // 訂閱遊戲流程的狀態變更事件
        gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            gameflow.OnPlayerStateChanged += newState => currentPlayerState = newState;
        }
        
        // 綁定按鈕事件
        X_Button.onClick.AddListener(HideHintPanel);
        OkButton.onClick.AddListener(OnOkButtonClicked);
        AgainButton.onClick.AddListener(OnAgainButtonClicked);
        Q1Button.onClick.AddListener(() => OnQuestionButtonClicked(1));
        Q2Button.onClick.AddListener(() => OnQuestionButtonClicked(2));
        Q3Button.onClick.AddListener(() => OnQuestionButtonClicked(3));
        Q4Button.onClick.AddListener(() => OnQuestionButtonClicked(4));
        
        // 初始隱藏提示面板
        if (AssistantHintPanel != null)
            AssistantHintPanel.SetActive(false);
        
        // 啟動定時檢查協程
        StartCoroutine(PeriodicCheck());
    }

    // 定時檢查協程
    IEnumerator PeriodicCheck()
    {
        while (true)
        {
            // 每分鐘檢查一次
            yield return new WaitForSeconds(60f);
            
            // 如果面板未開啟，則自動顯示提示面板
            if (AssistantHintPanel != null && !AssistantHintPanel.activeInHierarchy)
            {
                if (showDebugInfo)
                    Debug.Log("定時檢查：每一分鐘顯示提示面板");
                ShowHintPanel();
            }
        }
    }

    // 從外部元件更新玩家狀態和相關資訊
    void UpdatePlayerStateFromExternal()
    {
        // 從模式管理器獲取學習模式
        if (ModeManager.Instance != null)
        {
            learningMode = ModeManager.Instance.currentMode;
        }

        // 重置對話文字
        currentDialogueText = "";
        
        // 從活躍顧客獲取對話和訂單資訊
        allCustomers = FindObjectsOfType<SingleCustomer>();
        SingleCustomer activeCustomer = GetActiveCustomer();
        if (currentPlayerState == PlayerState.TalkingToCustomer && activeCustomer != null && activeCustomer.statementText != null)
        {
            currentDialogueText = activeCustomer.statementText.text;
        }
        
        // 從 QA 管理器獲取對話
        allQAmanagers = FindObjectsOfType<QAmanager>();
        QAmanager activeQAManager = GetActiveQAManager();
        if (currentPlayerState == PlayerState.OrderingAtStaff && activeQAManager != null && activeQAManager.statementText != null)
        {
            currentDialogueText = activeQAManager.statementText.text;
        }

        // 從遊戲說明獲取文字
        gameDescription = FindObjectOfType<GameDescription>();
        if (currentPlayerState == PlayerState.ReadingDescription && gameDescription != null)
        {
            if (gameDescription.InfoContent != null)
            {
                currentDialogueText = gameDescription.InfoContent.text;
            }
            currentDialoguePage = gameDescription.currentInfo;
            if (currentDialoguePage == 2 || currentDialoguePage == 3)
            {
                currentDialogueText = "當前是圖像說明頁面，展示遊戲操作指引";
            }
        }

        // 從遊戲結束畫面獲取資訊
        gameOverUI = FindObjectOfType<GameOverUI>();
        if (currentPlayerState == PlayerState.Completed && gameOverUI != null && gameOverUI.messageText != null)
        {
            currentDialogueText = gameOverUI.messageText.text;
        }
    }

    // 獲取當前活躍的顧客
    SingleCustomer GetActiveCustomer()
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

    // 獲取當前活躍的 QA 管理器
    QAmanager GetActiveQAManager()
    {
        if (allQAmanagers != null)
        {
            foreach (var manager in allQAmanagers)
            {
                if (manager != null && manager.gameObject.activeInHierarchy)
                {
                    return manager;
                }
            }
        }
        return null;
    }

    // 顯示提示面板
    public void ShowHintPanel()
    {
        UpdatePlayerStateFromExternal();
        if (AssistantHintPanel != null)
        {
            AssistantHintPanel.SetActive(true);
            SwitchToQuestionPanel();
        }
    }

    // 隱藏提示面板
    public void HideHintPanel()
    {
        if (AssistantHintPanel != null)
            AssistantHintPanel.SetActive(false);
    }

    // 切換到問題選擇面板
    void SwitchToQuestionPanel()
    {
        if (QuestionPanel != null) QuestionPanel.SetActive(true);
        if (OutputPanel != null) OutputPanel.SetActive(false);
        UpdateQuestionButtons();
    }

    // 切換到輸出面板
    void SwitchToOutputPanel()
    {
        if (QuestionPanel != null) QuestionPanel.SetActive(false);
        if (OutputPanel != null) OutputPanel.SetActive(true);
    }

    // 設置按鈕是否可互動
    void SetButtonsInteractable(bool interactable)
    {
        OkButton.interactable = interactable;
        AgainButton.interactable = interactable;
        Q1Button.interactable = interactable;
        Q2Button.interactable = interactable;
        Q3Button.interactable = interactable;
        Q4Button.interactable = interactable;
    }

    // 確定按鈕點擊事件
    void OnOkButtonClicked()
    {
        SwitchToQuestionPanel();
        HideHintPanel();
    }

    // 重新解釋按鈕點擊事件
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

    // 更新問題按鈕的顯示狀態
    void UpdateQuestionButtons()
    {
        // 選項1: 不知道下一步 - 始終顯示
        Q1Button.GetComponentInChildren<TextMeshProUGUI>().text = "我不知道再來要做什麼？";
        Q1Button.gameObject.SetActive(true);
        
        // 選項2: 一直答錯 - 在點餐階段顯示
        bool canShowWrongOption = (currentPlayerState == PlayerState.OrderingAtStaff);
        Q2Button.gameObject.SetActive(canShowWrongOption);
        if (canShowWrongOption)
        {
            Q2Button.GetComponentInChildren<TextMeshProUGUI>().text = "這裡一直答錯";
        }
        
        // 選項3: 看不懂英文 - 在有對話內容時顯示
        bool hasTextContent = !string.IsNullOrEmpty(currentDialogueText);
        bool canShowTranslation = hasTextContent && 
                                 (currentPlayerState == PlayerState.ReadingDescription ||
                                  currentPlayerState == PlayerState.TalkingToCustomer ||
                                  currentPlayerState == PlayerState.OrderingAtStaff ||
                                  currentPlayerState == PlayerState.Completed);
        Q3Button.gameObject.SetActive(canShowTranslation);
        if (canShowTranslation)
        {
            Q3Button.GetComponentInChildren<TextMeshProUGUI>().text = "看不懂他在說什麼？";
        }
        
        // 選項4: 忘記訂單 - 在需要記住訂單的階段顯示
        bool canShowForgetOrder = currentPlayerState == PlayerState.MovingToStaff || 
                                  currentPlayerState == PlayerState.OrderingAtStaff;
        Q4Button.gameObject.SetActive(canShowForgetOrder);
        if (canShowForgetOrder)
        {
            Q4Button.GetComponentInChildren<TextMeshProUGUI>().text = "我忘記客人點什麼了";
        }
    }

    // 處理問題按鈕點擊事件
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
            Debug.Log($"選擇問題 {lastSelectedOption}，生成提示語:\n{prompt}");
        }
        
        StartCoroutine(SendToOpenAI(prompt));
        SwitchToOutputPanel();
        Text.text = "AI助手思考中...";
        SetButtonsInteractable(false);
    }

    // 生成發送給 OpenAI 的提示語
    string GeneratePrompt(string option)
    {
        string prompt = $"【遊戲情境】快餐店英文學習模擬 - {learningMode}模式\n";
        // TODO: 增加各模式下遊戲有何不同的說明，以便 AI 理解並根據模式調整回答的內容、用句式

        prompt += $"【玩家狀態】{currentPlayerState}\n";
        
        prompt += $"【玩家問題】";
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
        
        prompt += "\n\n請用中文回答，不要使用Emoji，用句簡潔明瞭，最多50字。";
        return prompt;
    }

    string GetNextStepGuidance()
    {
        switch (currentPlayerState)
        {
            case PlayerState.ReadingDescription:
                if (currentDialoguePage < 4) // 如果不是最後一頁
                {
                    return "玩家不知道看完當前說明頁面後該做什麼。請建議：『點擊 Next 按鈕繼續閱讀下一頁說明』。";
                }
                else // 最後一頁
                {
                    if (learningMode == LearningMode.ID) // ID 模式
                    {
                        return "玩家不知道看完遊戲說明後該做什麼。請建議：『點擊 OK 按鈕關閉說明，然後跟著地上的箭頭走，前往顧客那裡』。";
                    }
                    else
                    {
                        return "玩家不知道看完遊戲說明後該做什麼。請建議：『點擊 X 按鈕關閉說明，然後跟著地上的箭頭走，前往顧客那裡』。";
                    }
                }
            case PlayerState.MovingToCustomer:
                return "玩家正在走向顧客但不知道該做什麼。請建議：『跟著地上的箭頭走到顧客那裡，點擊顧客頭上的 ! 按鈕開始對話』。";
            case PlayerState.TalkingToCustomer:
                return "玩家正在與顧客對話但不知道下一步。請建議：『理解顧客的對話內容，在對話框下面選擇最合適的回覆按鈕，並記住訂單內容』。";
            case PlayerState.MovingToStaff:
                return "玩家正在走向員工但不知道該做什麼。請建議：『跟著地上的箭頭走到員工那裡，點擊員工頭上的 ! 按鈕開始點餐』。";
            case PlayerState.OrderingAtStaff:
                return "玩家正在與員工點餐但不知道該做什麼。請建議：『理解顧客的對話內容，根據顧客訂單，在對話框下面選擇最合適的回覆按鈕，並完成點餐』。";
            case PlayerState.ReturningToCustomer:
                return "玩家正在返回顧客但不知道該做什麼。請建議：『跟著地上的箭頭走回顧客那裡交餐』。";
            default:
                return "玩家不知道下一步該做什麼。請根據當前狀態提供具體指引。";
        }
    }

    string GetWrongAnswerGuidance()
    {
        return "玩家在點餐時一直答錯。請鼓勵玩家並建議：『沒關係！請跟著地上的箭頭走回顧客那裡，再問一次他想要什麼，仔細記住訂單內容。』";
    }

    string GetTranslationGuidance()
    {
        string textToTranslate = currentDialogueText;
        if (currentPlayerState == PlayerState.ReadingDescription && (currentDialoguePage == 2 || currentDialoguePage == 3)) // 圖像說明頁面
        {
            return "玩家看不懂圖像說明頁面。請建議：『這是圖像說明頁面，展示遊戲操作指引。請仔細觀察圖片內容以了解如何進行遊戲。』";
        }
        else if (!string.IsNullOrEmpty(textToTranslate))
        {
            return $"玩家看不懂英文內容。請將這句話翻譯成中文：『{textToTranslate}』";
        }
        return "玩家看不懂當前內容。請解釋當前情境。";
    }

    string GetForgotOrderGuidance()
    {
        return "玩家忘記客人點什麼了。請建議：『請跟著地上的箭頭走回顧客那裡，再問一次他想要什麼，仔細記住訂單內容。』";
    }

    // 發送請求到 OpenAI 並處理回應
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

    // 非同步發送訊息到 OpenAI
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

    void OnDestroy()
    {
        client?.Dispose();
    }
}