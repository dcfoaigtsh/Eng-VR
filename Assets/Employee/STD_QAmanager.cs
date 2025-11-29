using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI; // 雖然此腳本內沒有直接使用 NavMeshAgent，但保留 namespace

public class STD_QAmanager : QAmanager
{
    // [Header("UI")]
    // public List<Button> optionAdvancedButtons;

    // [Header("Speech")] ⚠️ 已刪除
    // public SpeechPopup speechPopup;   // ⚠️ 已刪除

    [Header("Flow References")]
    public STD_SingleCustomer singleCustomer;
    public STD_Gameflow gameflow;

    [Header("Path Management")]
    public DestinationLineDrawer drawer;
    public Transform nextCustomer;
    public UnityEngine.AI.NavMeshAgent agentForThisRoute;

    [Header("Randomization")]
    [Tooltip("若 >= 0，使用固定亂數種子，讓每次啟動的選項順序一致。")]
    public int randomSeed = -1;

    private bool firstAttemptPending = true;
    private int currentStage = 0;
    private bool optionsShuffled = false;

    [System.Serializable]
    public class QAOption
    {
        public string text;
        public Sprite image;
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public List<QAOption> options;
        public int correctIndex;
    }

    public List<Stage> stages;

    void Start()
    {
        if (!optionsShuffled)
        {
            if (randomSeed >= 0) Random.InitState(randomSeed);
            ShuffleOptionsInEachStage();
            optionsShuffled = true;
        }
        ShowCurrentStage();
    }

    void ShowCurrentStage()
    {
        Gameflow gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            if (statementText.gameObject.activeInHierarchy)
            {
                gameflow.NotifyStaffInteractionStarted();
            }
        }

        firstAttemptPending = true;

        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        var stage = stages[currentStage];
        statementText.text = stage.question;

        for (int i = 0; i < optionAdvancedButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var btn = optionAdvancedButtons[i];
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                var textComp = btn.GetComponentInChildren<TextMeshProUGUI>();
                // 這行邏輯用於抓取 Image，通常 index 1 是圖標
                var imageComps = btn.GetComponentsInChildren<Image>();
                var imageComp = imageComps.Length > 1 ? imageComps[1] : null;

                if (textComp) textComp.text = stage.options[i].text ?? "";
                if (imageComp)
                {
                    if (stage.options[i].image != null)
                    {
                        imageComp.sprite = stage.options[i].image;
                        imageComp.enabled = true;
                    }
                    else imageComp.enabled = false;
                }

                btn.onClick.RemoveAllListeners();
                int captured = i;
                // 點擊後直接執行判斷邏輯
                btn.onClick.AddListener(() => OnOptionClicked(captured));
            }
            else
            {
                optionAdvancedButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 🏆 主要修改區域：移除語音面板顯示和回調等待 🏆
    void OnOptionClicked(int index)
    {
        var stage = stages[currentStage];

        // 鎖住選項，避免連點
        SetOptionsInteractable(false);

        // 立即判斷正確與否
        bool isCorrect = (index == stage.correctIndex);

        // 第一次作答統計
        if (firstAttemptPending && gameflow != null)
        {
            gameflow.RegisterFirstAttempt(isCorrect);
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
        // 答對後等待 1.2 秒
        yield return new WaitForSeconds(1.2f);
        currentStage++;
        SetOptionsInteractable(true);
        ShowCurrentStage();
    }

    IEnumerator RetryAfterDelay()
    {
        // 答錯後等待 1.2 秒
        yield return new WaitForSeconds(1.2f);
        SetOptionsInteractable(true);
        ShowCurrentStage(); // 重新顯示本階段問題
    }

    void FinishQAFlow()
    {
        statementText.text = "Clerk: You're welcome!";
        foreach (var b in optionAdvancedButtons) b.gameObject.SetActive(false);

        Gameflow gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            gameflow.NotifyReturningToCustomer();
        }

        StartCoroutine(SwitchToFinalDialogue());
    }

    IEnumerator SwitchToFinalDialogue()
    {
        yield return new WaitForSeconds(1f);
        if (drawer != null)
        {
            if (nextCustomer != null) drawer.ChangeDestination(nextCustomer);
            if (agentForThisRoute != null) drawer.ChangeNavAgent(agentForThisRoute);
        }
        gameObject.SetActive(false);
        if (gameflow != null) gameflow.NextCustomer();
    }

    void SetOptionsInteractable(bool on)
    {
        foreach (var b in optionAdvancedButtons) if (b) b.interactable = on;
    }

    // ===== 洗牌選項（保留正確索引同步） =====
    void ShuffleOptionsInEachStage()
    {
        if (stages == null || stages.Count == 0) return;
        foreach (var stage in stages)
        {
            if (stage.options == null || stage.options.Count <= 1) continue;

            var correct = stage.options[stage.correctIndex];
            // 採用 Fisher–Yates 洗牌
            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                var tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }
            // 重新定位正確答案索引
            for (int j = 0; j < stage.options.Count; j++)
            {
                if (stage.options[j] == correct) { stage.correctIndex = j; break; }
            }
        }
    }
}