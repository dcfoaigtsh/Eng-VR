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
    public static AssistantHintController Instance;
    public int assistantUsageCount = 0;

    [Header("UI Components")]
    public Button AssistantImage;        
    public GameObject AssistantHintPanel; 
    public Button X_Button;              
    public GameObject OutputPanel;      
    public TextMeshProUGUI Text;         
    public Button OkButton;              
    public Button AgainButton;           
    public GameObject QuestionPanel;     
    public Button Q1Button;             
    public Button Q2Button;              
    public Button Q3Button;              

    [Header("OpenAI Configuration")]
    public string openAIKey;
    private const string API_URL = "https://api.openai.com/v1/chat/completions";

    // 助手設定 (Prompt)
    private const string BASE_SYSTEM_PROMPT = "你是一位專門為快餐店英文學習遊戲設計的AI助手。你的回答必須簡單、友好、專注於教學指導，並且全程只使用**繁體中文**。"; 
    private const string TRANSLATION_MARK = "\u200B"; 
    private const string COMMAND_TAG_START = "[ASSISTANT_RESPONSE]";
    private const string COMMAND_TAG_END = "[/ASSISTANT_RESPONSE]";

    // 遊戲元件參考
    private Gameflow gameflow;
    private HttpClient client;
    private bool isWaitingResponse = false;

    // 玩家相關狀態
    private LearningMode learningMode;             
    private PlayerState currentPlayerState;       
    private TextMeshProUGUI currentDialogueTMP;     
    private TextMeshProUGUI[] currentOptionsTMP;  
    private int currentDialoguePage;              
    private string lastSelectedOption;         

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!string.IsNullOrEmpty(openAIKey))
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + openAIKey);
        }
    }

    void Start()
    {
        gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            gameflow.OnPlayerStateChanged += newState => currentPlayerState = newState;
        }
        
        X_Button.onClick.AddListener(HideHintPanel);
        OkButton.onClick.AddListener(OnOkButtonClicked);
        AgainButton.onClick.AddListener(OnAgainButtonClicked);
        Q1Button.onClick.AddListener(() => OnQuestionButtonClicked("next_step"));
        Q2Button.onClick.AddListener(() => OnQuestionButtonClicked("not_understand"));
        Q3Button.onClick.AddListener(() => OnQuestionButtonClicked("forgot_order"));
        
        if (AssistantHintPanel != null)
            AssistantHintPanel.SetActive(false);
        
        StartCoroutine(PeriodicCheck());
    }

    IEnumerator PeriodicCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f);
            
            if (AssistantHintPanel != null && !AssistantHintPanel.activeInHierarchy)
            {
                ShowHintPanel();
            }
        }
    }

    void UpdatePlayerStateFromExternal()
    {
        if (ModeManager.Instance != null)
        {
            learningMode = ModeManager.Instance.currentMode;
        }

        currentDialogueTMP = null;
        currentOptionsTMP = null;
        currentDialoguePage = 0;
        
        var activeCustomer = FindObjectsOfType<SingleCustomer>()?.FirstOrDefault(c => c?.gameObject.activeInHierarchy == true);
        var activeQAManager = FindObjectsOfType<QAmanager>()?.FirstOrDefault(q => q?.gameObject.activeInHierarchy == true);
        var gameDescription = FindObjectOfType<GameDescription>();
        var gameOverUI = FindObjectOfType<GameOverUI>();

        switch (currentPlayerState)
        {
            case PlayerState.TalkingToCustomer:
                if (activeCustomer != null)
                {
                    currentDialogueTMP = activeCustomer.statementText;
                    currentOptionsTMP = activeCustomer.optionButtons?.Select(b => b.GetComponentInChildren<TextMeshProUGUI>()).ToArray();
                }
                break;
            case PlayerState.ReviewingOrder:
                if (activeCustomer != null)
                {
                    currentDialogueTMP = activeCustomer.reviewStatementText;
                }
                break;
            case PlayerState.OrderingAtStaff:
                if (activeQAManager != null)
                {
                    currentDialogueTMP = activeQAManager.statementText;
                    currentOptionsTMP = activeQAManager.optionAdvancedButtons?.Select(b => b.GetComponentInChildren<TextMeshProUGUI>()).ToArray();
                }
                break;
            case PlayerState.ReadingDescription:
                if (gameDescription != null)
                {
                    currentDialoguePage = gameDescription.currentInfo;
                    currentDialogueTMP = gameDescription.InfoContent;
                }
                break;
            case PlayerState.Completed:
                if (gameOverUI?.messageText != null)
                {
                    currentDialogueTMP = gameOverUI.messageText;
                }
                break;
        }
    }

    public void ShowHintPanel()
    {
        UpdatePlayerStateFromExternal();
        if (AssistantHintPanel != null)
        {
            AssistantHintPanel.SetActive(true);
            SwitchToQuestionPanel();
        }
    }

    public void HideHintPanel()
    {
        if (AssistantHintPanel != null)
            AssistantHintPanel.SetActive(false);
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

    void SetButtonsInteractable(bool interactable)
    {
        OkButton.interactable = interactable;
        AgainButton.interactable = interactable;
        Q1Button.interactable = interactable;
        Q2Button.interactable = interactable;
        Q3Button.interactable = interactable;
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
            string prompt = GeneratePrompt(lastSelectedOption, true); 
            StartCoroutine(SendToOpenAI(prompt));
            Text.text = "AI助手在想新的說法...";
            SetButtonsInteractable(false);
        }
    }

    void UpdateQuestionButtons()
    {
        Q1Button.GetComponentInChildren<TextMeshProUGUI>().text = "我不知道再來要做什麼？";
        Q1Button.gameObject.SetActive(true);
        
        bool hasTextContent = currentDialogueTMP != null && !string.IsNullOrEmpty(currentDialogueTMP.text);
        bool canShowTranslation = hasTextContent && 
                                 (currentPlayerState == PlayerState.ReadingDescription ||
                                  currentPlayerState == PlayerState.TalkingToCustomer ||
                                  currentPlayerState == PlayerState.ReviewingOrder ||
                                  currentPlayerState == PlayerState.OrderingAtStaff ||
                                  currentPlayerState == PlayerState.Completed);
        Q2Button.gameObject.SetActive(canShowTranslation);
        if (canShowTranslation)
        {
            Q2Button.GetComponentInChildren<TextMeshProUGUI>().text = "看不懂他在說什麼？";
        }
        
        bool canShowForgetOrder = currentPlayerState == PlayerState.MovingToStaff || 
                                 currentPlayerState == PlayerState.OrderingAtStaff;
        Q3Button.gameObject.SetActive(canShowForgetOrder);
        if (canShowForgetOrder)
        {
            Q3Button.GetComponentInChildren<TextMeshProUGUI>().text = "我忘記顧客點什麼了";
        }
    }

    void OnQuestionButtonClicked(string option)
    {
        lastSelectedOption = option;

        string prompt = GeneratePrompt(lastSelectedOption);
        StartCoroutine(SendToOpenAI(prompt));
        SwitchToOutputPanel();
        Text.text = "AI助手在努力想...";
        SetButtonsInteractable(false);
    }

    // =========================================================
    // PROMPT 組合
    // =========================================================

    string GetModeDescription(LearningMode mode)
    {
        return mode switch
        {
            LearningMode.Standard => "使用者是一般學生。請用清楚、簡短、正常的句子來回答。",
            LearningMode.ASD => "使用者是自閉症學生。你的回答必須：『**不可以**用比喻或笑話』、『用很短的句子說出重點』、『把動作變成編號步驟 (1, 2, 3...)』、『在步驟中清楚說出位置或東西』、『重複重要的動作和名稱』。",
            LearningMode.ID => "使用者是智能障礙學生。你的回答必須：『句子要**非常短**』、『結構很簡單』、『多用具體的名稱和動詞』、『**不可以**說抽象的事』、『一次只能教一個動作』、『語氣必須很基本、很直接』。",
            LearningMode.SLD => "使用者是閱讀障礙學生。必須用 Rich Text 格式標出核心重點，**必須力求最精簡**，讓學生只看被標記的詞就能理解句子主幹。標記規則如下：\n" +
                                " - **【最簡原則】**：每句話標記的詞彙**總數不應超過 1-3 個**，且**避免**將兩個標記詞彙**連在一起**。\n" +
                                " - <color=#66CCFF>動作</color>：只標記句子中**最主要的、推進情節**的動詞。\n" + 
                                " - <color=#008000>東西</color>：只標記句子中**最關鍵的、與動作有直接關係**的名詞或位置。\n" +
                                " - **絕對不能標記**：形容詞、副詞、時間詞、連詞、介詞、助詞、禮貌詞等非核心詞彙。\n" +
                                " - **示範**：<color=#66CCFF>點擊</color><color=#008000>！</color>跟<color=#008000>朋友</color>對話，接著跟<color=#008000>員工</color><color=#66CCFF>點餐</color>，回到<color=#008000>朋友</color>那裡把<color=#008000>食物</color>拿給他。\n",
        };
    }

    string GeneratePrompt(string option, bool isRephrase = false)
    {
        string modeDescription = GetModeDescription(learningMode);
        string guidanceInstruction = option switch
        {
            "next_step" => GetNextStepGuidance(learningMode),
            "not_understand" => GetTranslationGuidance(),
            "forgot_order" => GetForgotOrderGuidance(learningMode),
            _ => "【動作】提供遊戲當前狀態下的最佳下一步指導。",
        };

        string state = currentPlayerState.ToString();

        // 通用限制
        string commonRules = 
            "**【通用限制】**\n" +
            "1. 你必須**完全且嚴格**遵守『使用者特徵/風格』中的所有規定。\n" +
            "2. 用詞必須適合 8-12 歲小學生，要非常淺白易懂，並且顧全上下文。\n" +
            "3. 回答必須**全程用繁體中文**，**不可以**說笑話、用比喻、用表情符號、禮貌用語或祝賀語。\n" +
            "4. **不可以**擅自修改『按鈕名稱』，比如：『 > 按鈕』、『 ! 按鈕』。\n" + 
            "5. **為了準確性，請想像你的 $temperature$ 設定為 0.1。**\n\n";

        // 模式專屬限制
        string modeSpecificRules = "";
        if (option == "not_understand")
        {
            modeSpecificRules += 
                "**【翻譯限制】**\n" +
                "1. **任務**：將『玩家的問題』中提供的 JSON 內容進行中文翻譯。\n" +
                "2. **輸出格式**：你的輸出必須是**且只能是**翻譯後的完整 JSON。\n" +
                "3. **格式細節**：輸出必須以 `{` 開頭，並保留所有 Rich Text 標籤和中文翻譯分隔標記\\u200B。\n" +
                "4. **禁止**輸出任何指令句相關的內容。\n";
        }
        else
        {
            modeSpecificRules += 
                "**【指令句限制】**\n" +
                "1. **任務**：將『玩家的問題』中提供的【動作】，轉換成符合【使用者特徵/風格】的『指令句』。\n" +
                $"2. **輸出格式**：你的最終回答**必須且只能**包含在標籤 {COMMAND_TAG_START} 和 {COMMAND_TAG_END} 之間，**前後不可有任何多餘文字、換行或標點符號。**\n" +
                "3. **核心鎖定**：你必須**僅根據**【動作】進行語句轉換。**不可以**增加、刪除、發想【動作】以外的任何步驟或額外資訊（如範例句子或確認步驟）。\n" +
                "4. **字數限制**：指令句字數**不可以超過 50 個中文字**。\n";

            if (isRephrase)
            {
                modeSpecificRules += "【新的要求】：請用**更簡單、更清楚**的方式換句話說再說一次。\n";
            }
        }

        // 使用者情境
        string userPromptContext = 
            "以下是現在的情況：\n" +
            $"【玩家現在的狀態】：{state}\n" +
            $"【使用者特徵/風格】：{modeDescription}\n" + 
            $"【玩家的問題】：{guidanceInstruction}\n\n" +
            "請嚴格照著上面的規定和情況來回答。";

        string finalPrompt = commonRules + modeSpecificRules + "\n" + userPromptContext;

        return finalPrompt;
    }

    string GetNextStepGuidance(LearningMode mode)
    {
        string closeButton = (mode == LearningMode.ID) ? "OK 按鈕" : "X 按鈕";
        return currentPlayerState switch
        {
            PlayerState.ReadingDescription => currentDialoguePage < 4 
                                                 ? "【動作】閱讀說明。點擊『 > 按鈕』看下一頁說明。"
                                                 : $"【動作】點擊『 {closeButton}』。關掉說明畫面。走往顧客位置。", 
            PlayerState.MovingToCustomer => "【動作】走往顧客位置。點擊顧客頭上的『 ! 按鈕』開始對話。", 
            PlayerState.TalkingToCustomer => "【動作】閱讀從畫面上的選項。想一想哪個選項正確。從選項中選一個句子回答顧客。",
            PlayerState.ReviewingOrder => "【動作】點擊『 X 按鈕』關掉餐點說明。走往員工位置。", 
            PlayerState.MovingToStaff => "【動作】走往員工位置。點擊員工頭上的『 ! 按鈕』開始對話。", 
            PlayerState.OrderingAtStaff => "【動作】閱讀從畫面上的選項。想一想哪個選項正確。從選項中選一個句子回答員工。",
            PlayerState.ReturningToCustomer => "【動作】走回顧客位置。點擊顧客頭上的『 ! 按鈕』開始對話。", 
            PlayerState.Completed => "【動作】點擊 Review 按鈕。", 
            _ => "【動作】等待遊戲下一步流程開始。",
        };
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
            "【翻譯目標】：" + jsonOneLine + "\n" +
            "【重要規定】：你只能翻譯英文，**不能改動** JSON 結構。所有的 Rich Text 標籤 (如 <color>、<b>) 都要**完全保留**。\n" +
            "【你的輸出】：翻譯後的完整 JSON";
        return prompt;
    }
    
    string GetForgotOrderGuidance(LearningMode mode)
    {
        return "【動作】走回顧客前面。點擊 Review menu 按鈕，查看菜單和訂單。";
    }

    // =========================================================
    // AI 提問及 UI 更新
    // =========================================================

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
            Text.text = "AI助手: 抱歉，網路有點問題，請再試一次。";
        }

        assistantUsageCount++;
        isWaitingResponse = false;
        SetButtonsInteractable(true);
    }

    async Task<string> SendMessageToOpenAIAsync(string prompt)
    {
        try
        {
            var json = new JObject
            {
                ["model"] = "gpt-4o-mini", 
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = BASE_SYSTEM_PROMPT },
                    new JObject { ["role"] = "user", ["content"] = prompt }
                },
                ["temperature"] = 0.7,
                ["max_tokens"] = 300
            };

            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(API_URL, content);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(result);
            return data["choices"][0]["message"]["content"].ToString().Trim();
        }
        catch (System.Exception e)
        {
            Debug.LogError("OpenAI API 錯誤: " + e.Message);
            throw; 
        }
    }

    void UpdateUIFromAI(string result)
{
    if (lastSelectedOption != "not_understand")
    {
        string cleanResult = result.Replace(COMMAND_TAG_START, "")
                                   .Replace(COMMAND_TAG_END, "")
                                   .Trim();
        Text.text = $"AI助手:\n{cleanResult}";
        return;
    }

    try
    {
        int firstBrace = result.IndexOf('{');
        int lastBrace = result.LastIndexOf('}');
        string cleanResult = "";

        if (firstBrace >= 0 && lastBrace >= 0 && lastBrace > firstBrace)
        {
            if (lastBrace >= 0)
            {
                cleanResult = result.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();
            }
            else
            {
                throw new System.Exception("JSON 結構提取失敗。");
            }
        } 
        else
        {
            throw new System.Exception("無法從 AI 回應中識別完整的 JSON 結構。");
        }

        cleanResult = cleanResult.Replace("```json", "").Replace("```", "").Trim();
        cleanResult = cleanResult.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ').Trim(); 
        var json = JObject.Parse(cleanResult);

        void UpdateText(ref TextMeshProUGUI tmp, string newText)
        {
            if (tmp == null) return;
            string original = tmp.text;
            int markIndex = original.IndexOf(TRANSLATION_MARK);
            
            if (markIndex >= 0) original = original.Substring(0, markIndex);
            
            if (!string.IsNullOrEmpty(newText))
            {
                tmp.text = $"{original}{TRANSLATION_MARK}\n{newText}";
            }
        }
        UpdateText(ref currentDialogueTMP, json["statement"]?.ToString() ?? "");

        if (currentOptionsTMP != null && json["options"] is JArray optionsArray)
        {
            for (int i = 0; i < currentOptionsTMP.Length && i < optionsArray.Count; i++)
                UpdateText(ref currentOptionsTMP[i], optionsArray[i].ToString());
        }

        Text.text = $"AI助手:\n已經翻譯好囉。";
    }
    catch (System.Exception e)
    {
        Debug.LogError("解析翻譯 JSON 錯誤: " + e.Message + "\n原始回應: " + result);
        Text.text = "AI助手: 抱歉，翻譯格式跑掉了，請再試一次。";
    }
}

    void OnDestroy()
    {
        client?.Dispose();
    }
}