using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class SLD_SingleCustomer : SingleCustomer
{
    // [Header("UI")]
    public Button statementAudioButton;
    // public List<Button> optionButtons;
    public List<Button> optionAudioButtons;

    [Header("Flow")]
    public SLD_Gameflow customerManager;
    public GameObject completeIcon;

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee1;
    public NavMeshAgent agentForThisRoute;

    [Header("Audio")]
    public AudioSource audioSource;
    public float wrongHintDelay = 1f;
    public float stageAdvanceDelay = 0.5f;

    [Header("Randomization")]
    [Tooltip("若 >= 0，使用固定 seed 讓洗牌可重現；-1 則使用預設隨機。")]
    public int randomSeed = -1;

    [Header("Order Review UI")]
    [Tooltip("顧客頭上的『Review menu』按鈕")]
    public GameObject reviewMenuButton;   // 小按鈕
    [Tooltip("顧客的回顧菜單面板（顯示 I want a ...）")]
    public GameObject reviewPanel;        // menu panel

    [Header("Main Dialogue UI (顧客對話主面板)")]
    [Tooltip("顧客對話的大白板 Panel（問句 + 選項那塊）")]
    public GameObject mainDialoguePanel;  // 主對話 Panel

    private int currentStage = 0;
    private bool returningWithFood = false;
    private bool firstAttemptPending = true;

    private bool mainOptionsShuffled = false;
    private bool returnOptionsShuffled = false;

    [System.Serializable]
    public class QAOption
    {
        public string text;
        public Sprite image;
        public AudioClip audio;
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public AudioClip questionAudio;
        public List<QAOption> options;
        public int correctIndex;
    }

    [Header("Dialogue Data")]
    public List<Stage> stages;
    public List<Stage> returnDialogueStages;

    void Awake()
    {
        if (audioSource == null)
        {
            var go = new GameObject("SLD_OptionAudioSource");
            go.transform.SetParent(transform);
            audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void OnEnable()
    {
        if (randomSeed >= 0) Random.InitState(randomSeed);

        // 初始時關掉 Review UI，打開主對話 Panel
        if (reviewMenuButton != null) reviewMenuButton.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);
        if (mainDialoguePanel != null) mainDialoguePanel.SetActive(true);
        if (statementText != null) statementText.gameObject.SetActive(true);

        if (!returningWithFood)
        {
            if (!mainOptionsShuffled)
            {
                ShuffleOptionsInEachStage(stages);
                mainOptionsShuffled = true;
            }
            currentStage = 0;
        }
        else
        {
            if (!returnOptionsShuffled)
            {
                ShuffleOptionsInEachStage(returnDialogueStages);
                returnOptionsShuffled = true;
            }
            currentStage = 0;
        }

        ShowCurrentStage();
    }

    void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
    }

    // ======================== 顯示題目 ========================
    void ShowCurrentStage()
    {
        Gameflow gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            if (statementText.gameObject.activeInHierarchy)
            {
                gameflow.NotifyCustomerInteractionStarted();
            }
        }

        firstAttemptPending = true;

        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        if (currentList == null || currentList.Count == 0)
        {
            Debug.LogWarning("[SLD_SingleCustomer] 沒有題目資料！");
            if (!returningWithFood) FinishInteraction();
            else ShowFinalThanks();
            return;
        }

        if (currentStage >= currentList.Count)
        {
            if (!returningWithFood) FinishInteraction();
            else ShowFinalThanks();
            return;
        }

        Stage stage = currentList[currentStage];

        // 防呆
        if (stage.options == null || stage.options.Count == 0)
        {
            Debug.LogWarning($"[SLD_SingleCustomer] 第 {currentStage} 題沒有選項！");
            currentStage++;
            ShowCurrentStage();
            return;
        }
        if (stage.correctIndex < 0 || stage.correctIndex >= stage.options.Count)
        {
            Debug.LogWarning($"[SLD_SingleCustomer] 第 {currentStage} 題 correctIndex 無效，自動修正為 0。");
            stage.correctIndex = 0;
        }

        // 題目文字
        if (statementText != null)
            statementText.text = stage.question ?? "";

        // 題幹音檔
        if (statementAudioButton != null)
        {
            bool hasQAudio = (stage.questionAudio != null);
            statementAudioButton.gameObject.SetActive(hasQAudio);
            statementAudioButton.onClick.RemoveAllListeners();

            if (hasQAudio)
            {
                statementAudioButton.onClick.AddListener(() =>
                {
                    if (audioSource.isPlaying) audioSource.Stop();
                    audioSource.PlayOneShot(stage.questionAudio);
                });
            }
        }

        // 顯示選項
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var btn = optionButtons[i];
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                var opt = stage.options[i];
                var textComp = btn.GetComponentInChildren<TextMeshProUGUI>();
                var imageComp = btn.GetComponentInChildren<Image>();

                if (textComp) textComp.text = opt.text ?? "";

                if (imageComp)
                {
                    if (opt.image != null)
                    {
                        imageComp.sprite = opt.image;
                        imageComp.enabled = true;
                    }
                    else
                        imageComp.enabled = false;
                }

                btn.onClick.RemoveAllListeners();
                int capturedIndex = i;
                btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));

                // 選項語音按鈕
                if (i < optionAudioButtons.Count && optionAudioButtons[i] != null)
                {
                    var audioBtn = optionAudioButtons[i];
                    bool hasAudio = (opt != null && opt.audio != null);
                    audioBtn.gameObject.SetActive(hasAudio);
                    audioBtn.onClick.RemoveAllListeners();

                    if (hasAudio)
                    {
                        int capturedAudioIndex = i;
                        audioBtn.onClick.AddListener(() =>
                        {
                            if (audioSource.isPlaying) audioSource.Stop();
                            audioSource.PlayOneShot(stage.options[capturedAudioIndex].audio);
                        });
                    }
                }
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
                if (i < optionAudioButtons.Count && optionAudioButtons[i] != null)
                    optionAudioButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // ======================== 選項點擊 ========================
    void OnOptionClicked(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];

        ToggleOptionButtons(false);

        bool isCorrect = (index == stage.correctIndex);

        // 第一次作答才紀錄
        if (firstAttemptPending && customerManager != null)
        {
            customerManager.RegisterFirstAttempt(isCorrect);
            firstAttemptPending = false;
        }

        if (isCorrect)
        {
            StartCoroutine(NextQuestion());
        }
        else
        {
            statementText.text = "Hmm... Try again!";
            StartCoroutine(RetryAfterDelay());
        }
    }

    IEnumerator NextQuestion()
    {
        ToggleOptionButtons(true);
        yield return new WaitForSeconds(stageAdvanceDelay);
        currentStage++;
        ShowCurrentStage();
    }

    IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSeconds(wrongHintDelay);
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }

    void ToggleOptionButtons(bool interactable)
    {
        foreach (var b in optionButtons)
            if (b) b.interactable = interactable;
    }

    void ToggleAllOptions(bool show)
    {
        foreach (var btn in optionButtons) if (btn) btn.gameObject.SetActive(show);
        foreach (var ab in optionAudioButtons) if (ab) ab.gameObject.SetActive(show);
    }

    // ======================== 第一次點餐結束 ========================
    void FinishInteraction()
    {
        statementText.text = "Thank you!";
        ToggleAllOptions(false);
        if (statementAudioButton) statementAudioButton.gameObject.SetActive(false);

        if (drawer != null)
        {
            if (employee1 != null) drawer.ChangeDestination(employee1);
            if (agentForThisRoute != null) drawer.ChangeNavAgent(agentForThisRoute);
        }

        Gameflow gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            gameflow.NotifyMovingToStaff();
        }

        // ✅ 從這一刻開始到回來交餐前，提供回顧訂單的功能
        if (reviewMenuButton != null) reviewMenuButton.SetActive(true);

        StartCoroutine(DelayedSwitch());
    }

    IEnumerator DelayedSwitch()
    {
        yield return new WaitForSeconds(1f);

        // ❌ 不要關掉整個顧客，只收起對話 UI
        if (statementText != null)
            statementText.gameObject.SetActive(false);

        if (mainDialoguePanel != null)
            mainDialoguePanel.SetActive(false);
    }

    // ======================== 回來交餐 ========================
    public void BeginFinalDialogue()
    {
        returningWithFood = true;
        currentStage = 0;

        // 回來交餐時不需要再回顧 menu
        if (reviewMenuButton != null) reviewMenuButton.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);

        if (mainDialoguePanel != null) mainDialoguePanel.SetActive(true);
        if (statementText != null) statementText.gameObject.SetActive(true);

        ShowCurrentStage();
    }

    void ShowFinalThanks()
    {
        statementText.text = "Thank you!";
        ToggleAllOptions(false);
        if (statementAudioButton) statementAudioButton.gameObject.SetActive(false);

        if (completeIcon != null) completeIcon.SetActive(true);
        if (customerManager != null) customerManager.ProceedToNextCustomer();
    }

    // ======================== Review menu 開關，給 UI 按鈕用 ========================
    public void OpenReviewPanel()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(true);
    }

    public void CloseReviewPanel()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(false);
    }

    // ======================== 選項洗牌 ========================
    private void ShuffleOptionsInEachStage(List<Stage> list)
    {
        if (list == null || list.Count == 0) return;

        for (int s = 0; s < list.Count; s++)
        {
            var stage = list[s];
            if (stage == null || stage.options == null || stage.options.Count <= 1) continue;

            QAOption correctRef = stage.options[Mathf.Clamp(stage.correctIndex, 0, stage.options.Count - 1)];

            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                var tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }

            for (int j = 0; j < stage.options.Count; j++)
            {
                if (stage.options[j] == correctRef)
                {
                    stage.correctIndex = j;
                    break;
                }
            }
        }
    }
}
