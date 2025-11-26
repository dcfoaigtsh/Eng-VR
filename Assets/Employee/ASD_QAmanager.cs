using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

// 管理 ASD 模式下的問答流程（已移除語音辨識）
public class ASD_QAmanager : QAmanager
{
    // [Header("UI")]
    public List<Button> optionAdvancedButtons;

    [Header("Flow References")]
    public ASD_SingleCustomer singleCustomer;

    [Header("Path Management")]
    public DestinationLineDrawer drawer;
    public Transform nextCustomer;
    public UnityEngine.AI.NavMeshAgent agentForThisRoute;

    // 控制邏輯
    private bool firstAttemptPending = true;
    private int currentStage = 0;

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
        ShowCurrentStage();
    }

    // ======================================
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

        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        // 顯示選項（圖片＋文字）
        for (int i = 0; i < optionAdvancedButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var btn = optionAdvancedButtons[i];
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                var textComp = btn.GetComponentInChildren<TextMeshProUGUI>();
                var imageComps = btn.GetComponentsInChildren<Image>();
                var imageComp = imageComps.Length > 1 ? imageComps[1] : null;

                if (textComp != null)
                    textComp.text = stage.options[i].text ?? "";

                if (imageComp != null)
                {
                    if (stage.options[i].image != null)
                    {
                        imageComp.sprite = stage.options[i].image;
                        imageComp.enabled = true;
                    }
                    else
                        imageComp.enabled = false;
                }

                btn.onClick.RemoveAllListeners();
                int capturedIndex = i;
                // 點擊後直接執行判斷邏輯
                btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));
            }
            else
            {
                optionAdvancedButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 🏆 主要修改區域：移除語音面板呼叫和回調等待 🏆
    void OnOptionClicked(int index)
    {
        Stage stage = stages[currentStage];
        
        // 鎖住選項避免連點
        ToggleOptionButtons(false);

        // 立即判斷是否正確
        bool isCorrect = (index == stage.correctIndex);

        // ✅ 第一次作答才統計
        if (firstAttemptPending && singleCustomer != null && singleCustomer.customerManager != null)
        {
            singleCustomer.customerManager.RegisterFirstAttempt(isCorrect);
            firstAttemptPending = false;
        }

        if (isCorrect)
        {
            // 答對：不顯示提示，直接下一題
            StartCoroutine(NextQuestion());
        }
        else
        {
            // 答錯：顯示提示並重試
            statementText.text = "Hmm... Try again!";
            StartCoroutine(RetryAfterDelay());
        }
        
        // ⚠️ 原本的 speechPopup.ShowSentence 呼叫已移除
    }

    // ======================================
    IEnumerator NextQuestion()
    {
        yield return new WaitForSeconds(1f);
        currentStage++;
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }

    IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }

    void ToggleOptionButtons(bool interactable)
    {
        foreach (var b in optionAdvancedButtons)
        {
            if (b) b.interactable = interactable;
        }
    }

    // ======================================
    void FinishQAFlow()
    {
        statementText.text = "You're welcome!";
        foreach (var btn in optionAdvancedButtons)
            btn.gameObject.SetActive(false);

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
            if (nextCustomer != null)
                drawer.ChangeDestination(nextCustomer);
            if (agentForThisRoute != null)
                drawer.ChangeNavAgent(agentForThisRoute);
        }

        gameObject.SetActive(false);

        if (singleCustomer != null)
            singleCustomer.BeginFinalDialogue();
    }
}