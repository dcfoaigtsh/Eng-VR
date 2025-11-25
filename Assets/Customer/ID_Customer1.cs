using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class ID_SingleCustomer : SingleCustomer
{
    // [Header("UI")]
    public List<Button> optionButtons;
    public GameObject completeIcon;

    [Header("Flow References")]
    public ID_Gameflow customerManager;
    public GameObject qaManager;
    public GameObject employeePanel;

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee1;
    public NavMeshAgent agentForThisRoute;

    private int currentStage = 0;
    private bool returningWithFood = false;
    private bool firstAttemptPending = true;  // ✅ 控制第一次作答


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

    [Header("Dialogue Data")]
    public List<Stage> stages;               // 點餐前對話
    public List<Stage> returnDialogueStages; // 回來交餐對話

    void OnEnable()
    {
        currentStage = 0;
        ShowCurrentStage();
    }

    // ==========================================================
    void ShowCurrentStage()
    {
        Gameflow gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            gameflow.NotifyCustomerInteractionStarted();
        }
        
        firstAttemptPending = true;

        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;

        if (currentStage >= currentList.Count)
        {
            if (!returningWithFood)
                FinishInteraction();
            else
                ShowFinalThanks();
            return;
        }

        Stage stage = currentList[currentStage];
        statementText.text = stage.question;

        // 顯示選項（圖片 + 文字）
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var btn = optionButtons[i];
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                var textComp = btn.GetComponentInChildren<TextMeshProUGUI>();
                var imageComp = btn.GetComponentInChildren<Image>();

                if (textComp) textComp.text = stage.options[i].text ?? "";

                if (imageComp)
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
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 🏆 主要修改區域：移除語音面板呼叫和回調等待 🏆
    void OnOptionClicked(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];
        
        // 鎖住按鈕避免多次點擊
        ToggleOptionButtons(false);

        // 立即判斷是否正確
        bool isCorrect = (index == stage.correctIndex);

        
        // ✅ 第一次作答統計
        if (firstAttemptPending && customerManager != null)
        {
            customerManager.RegisterFirstAttempt(isCorrect);
            firstAttemptPending = false;
        }

        if (isCorrect)
        {
            // 答對 → 不顯示提示，直接下一題
            StartCoroutine(NextQuestion());
        }
        else
        {
            // 答錯 → 顯示提示
            statementText.text = "Hmm... Try again!";
            StartCoroutine(RetryAfterDelay());
        }
    }

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
        foreach (var b in optionButtons)
            if (b) b.interactable = interactable;
    }

    // ==========================================================
    void FinishInteraction()
    {
        statementText.text = "Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);

        if (drawer != null)
        {
            if (employee1 != null)
                drawer.ChangeDestination(employee1);
            if (agentForThisRoute != null)
                drawer.ChangeNavAgent(agentForThisRoute);
        }

        if (customerManager != null)
            customerManager.OnDialogueWithCustomerFinished();

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
        if (qaManager != null)
            qaManager.SetActive(true);
        if (employeePanel != null)
            employeePanel.SetActive(true);
    }

    // ==========================================================
    public void BeginFinalDialogue()
    {
        returningWithFood = true;
        currentStage = 0;
        gameObject.SetActive(true);
        ShowCurrentStage();
    }

    void ShowFinalThanks()
    {
        statementText.text = "Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);

        if (completeIcon != null)
            completeIcon.SetActive(true);

        if (customerManager != null)
            customerManager.ShowGameOverManually();
    }
}