using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
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
    private LearningMode learningMode;              // 學習模式
    private PlayerState currentPlayerState;         // 目前玩家狀態
    private TextMeshProUGUI currentDialogueTMP;     // 目前對話 TMP
    private TextMeshProUGUI[] currentOptionsTMP;    // 目前選項 TMP
    private int currentDialoguePage;                // 目前對話頁面
    private string lastSelectedOption;              // 上次選擇的問題選項

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
        Q1Button.onClick.AddListener(() => OnQuestionButtonClicked("next_step"));
        Q2Button.onClick.AddListener(() => OnQuestionButtonClicked("not_understand"));
        Q3Button.onClick.AddListener(() => OnQuestionButtonClicked("forgot_order"));
        
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

        currentDialogueTMP = null;
        currentOptionsTMP = null;
        currentDialoguePage = 0;
        
        // 從活躍顧客獲取對話和餐點資訊
        allCustomers = FindObjectsOfType<SingleCustomer>();
        SingleCustomer activeCustomer = allCustomers?.FirstOrDefault(c => c?.gameObject.activeInHierarchy == true);
        if (currentPlayerState == PlayerState.TalkingToCustomer && activeCustomer != null)
        {
            currentDialogueTMP = activeCustomer.statementText;
            currentOptionsTMP = activeCustomer.optionButtons?.Select(b => b.GetComponentInChildren<TextMeshProUGUI>()).ToArray();
        }
        
        // 從 QA 管理器獲取對話
        allQAmanagers = FindObjectsOfType<QAmanager>();
        QAmanager activeQAManager = allQAmanagers?.FirstOrDefault(q => q?.gameObject.activeInHierarchy == true);
        if (currentPlayerState == PlayerState.OrderingAtStaff && activeQAManager != null)
        {
            currentDialogueTMP = activeQAManager.statementText;
            currentOptionsTMP = activeQAManager.optionAdvancedButtons?.Select(b => b.GetComponentInChildren<TextMeshProUGUI>()).ToArray();
        }

        // 從遊戲說明獲取文字
        gameDescription = FindObjectOfType<GameDescription>();
        if (currentPlayerState == PlayerState.ReadingDescription && gameDescription != null)
        {
            currentDialoguePage = gameDescription.currentInfo;
            currentDialogueTMP = gameDescription.InfoContent;
        }

        // 從遊戲結束畫面獲取資訊
        gameOverUI = FindObjectOfType<GameOverUI>();
        if (currentPlayerState == PlayerState.Completed && gameOverUI?.messageText != null)
        {
            currentDialogueTMP = gameOverUI.messageText;
        }
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
        
        // 選項2: 看不懂英文 - 在有對話內容時顯示
        bool hasTextContent = currentDialogueTMP != null && !string.IsNullOrEmpty(currentDialogueTMP.text);
        bool canShowTranslation = hasTextContent && 
                                 (currentPlayerState == PlayerState.ReadingDescription ||
                                  currentPlayerState == PlayerState.TalkingToCustomer ||
                                  currentPlayerState == PlayerState.OrderingAtStaff ||
                                  currentPlayerState == PlayerState.Completed);
        Q2Button.gameObject.SetActive(canShowTranslation);
        if (canShowTranslation)
        {
            Q2Button.GetComponentInChildren<TextMeshProUGUI>().text = "看不懂他在說什麼？";
        }
        
        // 選項3: 忘記餐點 - 在需要記住餐點的階段顯示
        bool canShowForgetOrder = currentPlayerState == PlayerState.MovingToStaff || 
                                  currentPlayerState == PlayerState.OrderingAtStaff;
        Q3Button.gameObject.SetActive(canShowForgetOrder);
        if (canShowForgetOrder)
        {
            Q3Button.GetComponentInChildren<TextMeshProUGUI>().text = "我忘記顧客點什麼了";
        }
    }

    // 處理問題按鈕點擊事件
    void OnQuestionButtonClicked(string option)
    {
        lastSelectedOption = option;

        string prompt = GeneratePrompt(lastSelectedOption);
        StartCoroutine(SendToOpenAI(prompt));
        SwitchToOutputPanel();
        Text.text = "AI助手思考中...";
        SetButtonsInteractable(false);
    }

    // 生成發送給 OpenAI 的提示語
    string GeneratePrompt(string option)
    {

        string mode = learningMode switch
        {
            LearningMode.Standard => "STD 模式：一般生，語句正常，操作流程正常，可稍微簡短但仍清楚。",
            LearningMode.ASD => "ASD 模式：減少刺激，語句穩定一致，動作拆得細，避免跳躍。",
            LearningMode.ID  => "ID 模式：句子短，動作拆明確，標明位置與步驟，語速慢。",
            LearningMode.SLD => "SLD 模式：文字清晰、句子乾淨，結構穩定，英文直白說明。",
            _ => "STD 模式：一般生。"
        };

        string state = currentPlayerState.ToString();

        string question = option switch
        {
            "next_step" => GetNextStepGuidance(),
            "not_understand" => GetTranslationGuidance(),
            "forgot_order" => GetForgotOrderGuidance(),
            _ => ""
        };

        string prompt = $"你是快餐店英文學習遊戲的AI助手。\n" +
                        "語氣友善但專注任務。\n" +
                        "用繁體中文回答。\n\n" +
                        "回答內容只依照【玩家問題】本身。\n" +
                        "【遊戲情境】與【玩家狀態】只能用來判斷語句表達方式，不可額外延伸或添加超出問題的提醒。\n\n" +
                        "不要加入多餘的鼓勵、祝賀、安慰或泛用建議。\n" +
                        "不用 Emoji，可在需要時使用顏文字。\n" +
                        "語句要直白清楚，動作拆分清楚，位置描述明確。\n" +
                        "總字數不超過 50 字。\n\n" +
                        $"以下是資訊：\n" +
                        $"【遊戲情境】：快餐店英文學習模擬 - {mode}\n" +
                        $"【玩家狀態】：{state}\n" +
                        $"【玩家問題】：{question}\n\n" +
                        "請依規則作答。";

        if (showDebugInfo) Debug.Log($"選擇問題 {lastSelectedOption}，生成提示語:\n{prompt}");

        return prompt;
    }

    string GetNextStepGuidance()
    {
        switch (currentPlayerState)
        {
            case PlayerState.ReadingDescription:
                if (currentDialoguePage < 4) // 如果不是最後一頁
                {
                    return "玩家不知道看完當前說明頁面後該做什麼。請建議：『點擊 Next 按鈕，繼續閱讀下一頁的說明』。";
                }
                else // 最後一頁
                {
                    if (learningMode == LearningMode.ID) // ID 模式
                    {
                        return "玩家不知道看完遊戲說明後該做什麼。請建議：『點擊 OK 按鈕，關閉說明，跟著地上的箭頭，走到顧客前面』。";
                    }
                    else
                    {
                        return "玩家不知道看完遊戲說明後該做什麼。請建議：『點擊 X 按鈕，關閉說明，跟著地上的箭頭，走到顧客前面』。";
                    }
                }
            case PlayerState.MovingToCustomer:
                return "玩家正在走向顧客但不知道該做什麼。請建議：『跟著地上的箭頭，走到顧客前面，點擊顧客頭上的 ! 按鈕』。";
            case PlayerState.TalkingToCustomer:
                return "玩家正在與顧客對話但不知道下一步。請建議：『想一想顧客在說什麼，選一個句子回答顧客，記住顧客的餐點』。";
            case PlayerState.MovingToStaff:
                return "玩家正在走向員工但不知道該做什麼。請建議：『跟著地上的箭頭，走到員工前面，點擊員工頭上的 ! 按鈕』。";
            case PlayerState.OrderingAtStaff:
                return "玩家正在與員工點餐但不知道該做什麼。請建議：『想一想員工在說什麼，選一個句子回答員工，完成點餐』。";
            case PlayerState.ReturningToCustomer:
                return "玩家正在返回顧客但不知道該做什麼。請建議：『跟著地上的箭頭，走回顧客前面，點擊顧客頭上的 ! 按鈕』。";
            case PlayerState.Completed:
                return "玩家完成遊戲但不知道該做什麼。請建議：『恭喜你已經完成遊戲，點擊 Review 按鈕複習單字』。";
            default:
                return "玩家不知道下一步該做什麼。請根據當前狀態提供具體指引。";
        }
    }

    string GetTranslationGuidance()
    {
        string dialogueText = currentDialogueTMP != null ? currentDialogueTMP.text : "";
        var optionsList = currentOptionsTMP != null
            ? currentOptionsTMP.Where(t => t != null).Select(t => t.text).ToList()
            : new List<string>();

        var jsonObject = new JObject
        {
            ["statement"] = dialogueText,
            ["options"] = new JArray(optionsList)
        };

        string jsonOneLine = jsonObject.ToString(Newtonsoft.Json.Formatting.None);

        string prompt =
            "你只能翻譯，不得修改任何格式。\n" +
            "請翻譯以下 JSON 內 \"statement\" 與 \"options\" 的英文文字。\n" +
            "以下規則必須完全遵守：\n" +
            "1. 所有 Rich Text 標籤 (如 <b>、<i>、<color=#XXXXXX> 等) 必須完整保留，不可刪除、移動、修改或新增。\n" +
            "2. 只能替換文字內容，翻譯後的標籤仍需套在對應詞語上。\n" +
            "3. JSON 不能被改動：鍵名、層級、陣列、順序全部維持不變。\n" +
            "4. 回覆內容禁止包含說明、註解、額外訊息。\n" +
            "5. 回覆時必須輸出唯一一段 JSON。\n\n" +
            "【翻譯目標】" + jsonOneLine + "\n" +
            "【請直接給翻譯後的 JSON】";

        return prompt;
    }

    string GetForgotOrderGuidance()
    {
        return "玩家忘記顧客點什麼了。請建議：『請跟著地上的箭頭，走回顧客前面，再問一次顧客想要什麼，記住餐點。』";
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
            UpdateUIFromAI(task.Result);
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
                ["model"] = "gpt-4o-mini",
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = "你是快餐店英文學習遊戲的AI助手。用友善、專注任務的語氣，繁體中文回答，保持簡潔，最多50字。" },
                    new JObject { ["role"] = "user", ["content"] = prompt }
                },
                ["temperature"] = 0.7,
                ["max_tokens"] = 300
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

    // 根據 AI 回應更新 UI
    void UpdateUIFromAI(string result)
    {
        const string TRANSLATION_MARK = "\u200B"; // 零寬空格作為翻譯標記
        Debug.Log("AI 回應: " + result);

        if (lastSelectedOption != "not_understand")
        {
            Text.text = $"AI助手:\n{result}";
            return;
        }

        try
        {
            var json = JObject.Parse(result);

            void UpdateText(ref TextMeshProUGUI tmp, string newText)
            {
                if (tmp == null) return;
                string original = tmp.text;
                int markIndex = original.IndexOf(TRANSLATION_MARK);
                if (markIndex >= 0) original = original.Substring(0, markIndex);
                tmp.text = $"{original}{TRANSLATION_MARK}\n{newText}";
            }

            UpdateText(ref currentDialogueTMP, json["statement"]?.ToString() ?? "");

            if (currentOptionsTMP != null && json["options"] is JArray optionsArray)
            {
                for (int i = 0; i < currentOptionsTMP.Length && i < optionsArray.Count; i++)
                    UpdateText(ref currentOptionsTMP[i], optionsArray[i].ToString());
            }

            Text.text = "AI助手: 已經翻譯成中文啦。";
        }
        catch (System.Exception e)
        {
            Debug.LogError("解析翻譯 JSON 錯誤: " + e.Message);
            Text.text = "AI助手: 抱歉，發生錯誤，請稍後再試。";
        }
    }

    void OnDestroy()
    {
        client?.Dispose();
    }
}