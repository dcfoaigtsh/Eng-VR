using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class ASD_SingleCustomer : SingleCustomer
{
    // [Header("UI")]
    public List<Button> optionButtons;
    public GameObject completeIcon;

    [Header("Flow References")]
    public ASD_Gameflow customerManager;
    public GameObject qaManager;      // 店員那邊的 QA Manager

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee1;
    public NavMeshAgent agentForThisRoute;

    [Header("Order Review UI")]
    [Tooltip("小的『Review menu』按鈕 GameObject")]
    public GameObject reviewMenuButton;   // 顧客頭上的 Review menu 按鈕
    [Tooltip("回顧菜單的大面板 GameObject")]
    public GameObject reviewPanel;        // 回顧菜單面板（你自訂的 UI）

    [Header("Main Dialogue UI (ASD 顧客對話主面板)")]
    [Tooltip("顧客對話的大 Panel（白板 + X 那個）")]
    public GameObject mainDialoguePanel;  // 顧客的主對話板（和 STD 那個概念一樣）

    private int currentStage = 0;
    private bool returningWithFood = false;
    private bool firstAttemptPending = true; // ✅ 控制每題第一次作答

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
    public List<Stage> stages;               // 點餐前對話
    public List<Stage> returnDialogueStages; // 回來交餐對話

    void OnEnable()
    {
        // 剛啟用顧客：關掉 Review UI，主對話板打開
        if (reviewMenuButton != null) reviewMenuButton.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);
        if (mainDialoguePanel != null) mainDialoguePanel.SetActive(true);
        if (statementText != null) statementText.gameObject.SetActive(true);

        returningWithFood = false;
        currentStage = 0;
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
                gameflow.NotifyCustomerInteractionStarted();
            }
        }

        firstAttemptPending = true;

        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;

        // ✅ 若無更多題目
        if (currentList == null || currentList.Count == 0 || currentStage >= currentList.Count)
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

                textComp.text = stage.options[i].text ?? "";

                if (stage.options[i].image != null)
                {
                    imageComp.sprite = stage.options[i].image;
                    imageComp.enabled = true;
                }
                else imageComp.enabled = false;

                btn.onClick.RemoveAllListeners();
                int capturedIndex = i;
                btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // ======================================
    void OnOptionClicked(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];

        // 關閉選項避免重複操作
        ToggleOptionButtons(false);
        bool isCorrect = (index == stage.correctIndex);

        // ✅ 第一次作答才統計
        if (firstAttemptPending && customerManager != null)
        {
            customerManager.RegisterFirstAttempt(isCorrect);
            firstAttemptPending = false;
        }

        if (isCorrect)
        {
            // ✅ 答對：不顯示提示，直接下一題
            StartCoroutine(NextQuestion());
        }
        else
        {
            // ❌ 答錯才提示
            statementText.text = "Hmm... Try again!";
            StartCoroutine(RetryAfterDelay());
        }
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

    // ======================================
    void ToggleOptionButtons(bool interactable)
    {
        foreach (var b in optionButtons)
        {
            if (b) b.interactable = interactable;
        }
    }

    // ======================================
    void FinishInteraction()
    {
        // 第一次與顧客點餐結束，準備去找員工
        statementText.text = "Thank you!";
        foreach (var btn in optionButtons) btn.gameObject.SetActive(false);

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

        // ✅ 從這一刻開始到回來交餐前，允許回顧菜單
        if (reviewMenuButton != null) reviewMenuButton.SetActive(true);

        StartCoroutine(DelayedSwitch());
        customerManager.OnDialogueWithCustomerFinished();
    }

    IEnumerator DelayedSwitch()
    {
        // 讓 "Thank you!" 停留一下
        yield return new WaitForSeconds(1f);

        // ✅ 關掉對話文字與主對話 Panel，人不消失
        if (statementText != null)
            statementText.gameObject.SetActive(false);

        if (mainDialoguePanel != null)
            mainDialoguePanel.SetActive(false);

        // ✅ 啟用店員端 QA Manager（原本就有的邏輯）
        if (qaManager != null)
            qaManager.SetActive(true);

    }

    // ✅ 店員點餐結束後，從 Gameflow 呼叫這個
    public void BeginFinalDialogue()
    {
        returningWithFood = true;
        currentStage = 0;

        // 回來交餐時不需要回顧菜單 UI
        if (reviewMenuButton != null) reviewMenuButton.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);

        // 重新打開顧客對話 Panel
        if (mainDialoguePanel != null) mainDialoguePanel.SetActive(true);
        if (statementText != null) statementText.gameObject.SetActive(true);

        ShowCurrentStage();
    }

    // ✅ 回來交餐完畢，顯示勾勾與結束畫面
    void ShowFinalThanks()
    {
        statementText.text = "Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);

        // 結束時關掉回顧 UI（保險）
        if (reviewMenuButton != null) reviewMenuButton.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);

        if (completeIcon != null)
            completeIcon.SetActive(true);

        if (customerManager != null)
            customerManager.ShowGameOverManually();
    }

    // ============ Review menu 開關（給 UI Button 用） ============
    // 點「Review menu」按鈕時呼叫
    public void OpenReviewPanel()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(true);
    }

    // 點回顧面板上的 X 按鈕時呼叫
    public void CloseReviewPanel()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(false);
    }
}
