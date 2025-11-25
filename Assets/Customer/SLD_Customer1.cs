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
    public List<Button> optionButtons;
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
        public AudioClip audio; // 👈 保留音檔
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public AudioClip questionAudio; // 👈 保留音檔
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
            gameflow.NotifyCustomerInteractionStarted();
        }

        firstAttemptPending = true;
        // pendingSelectedIndex = -1; ⚠️ 已刪除
        // lastRecognizedText = ""; ⚠️ 已刪除

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

        // 🧩 防呆：選項為空或索引錯誤
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

        // 題幹音檔播放 (保留)
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

        // 顯示選項（含圖片 + 單獨語音）
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
                // 點擊選項後直接判斷
                btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));

                // 單獨播放選項語音 (保留)
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

    // 🏆 主要修改區域：移除語音面板呼叫和回調等待 🏆
    void OnOptionClicked(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];
        
        ToggleOptionButtons(false);

        // 立即判斷是否正確
        bool isCorrect = (index == stage.correctIndex);

        // ✅ 第一次作答才統計
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
        // 先顯示選項，再等待延遲時間
        ToggleOptionButtons(true);
        yield return new WaitForSeconds(stageAdvanceDelay);
        currentStage++;
        ShowCurrentStage();
    }

    IEnumerator RetryAfterDelay()
    {
        // 答錯後等待延遲時間
        yield return new WaitForSeconds(wrongHintDelay);
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }

    void ToggleOptionButtons(bool interactable)
    {
        foreach (var b in optionButtons)
            if (b) b.interactable = interactable;
    }

    // ======================== 結束流程 ========================
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

        StartCoroutine(DelayedSwitch());
    }

    IEnumerator DelayedSwitch()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }

    public void BeginFinalDialogue()
    {
        returningWithFood = true;
        gameObject.SetActive(true);
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

    void ToggleAllOptions(bool show)
    {
        foreach (var btn in optionButtons) if (btn) btn.gameObject.SetActive(show);
        foreach (var ab in optionAudioButtons) if (ab) ab.gameObject.SetActive(show);
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