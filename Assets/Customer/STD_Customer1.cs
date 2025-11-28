using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class STD_SingleCustomer : SingleCustomer
{
    // [Header("UI")]
    public List<Button> optionButtons;

    [Header("Flow")]
    public STD_Gameflow customerManager;
    public GameObject completeIcon;

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee1;
    public NavMeshAgent agentForThisRoute;

    [Header("Randomization")]
    [Tooltip("若 >= 0，使用固定亂數種子以重現相同洗牌結果。")]
    public int randomSeed = -1;

    [Header("Order Review UI")]
    [Tooltip("小的『Review menu』按鈕 GameObject")]
    public GameObject reviewMenuButton;    // 指向 Canvas/Reviewmenu
    [Tooltip("大的透明面板（menupanel） GameObject")]
    public GameObject reviewPanel;         // 指向 Canvas/menupanel
    [Header("Main Dialogue UI (顧客對話的大面板)")]
    public GameObject mainDialoguePanel;


    private int currentStage = 0;
    private bool returningWithFood = false;
    private bool firstAttemptPending = true;

    // 避免重複洗牌
    private bool optionsShuffled = false;
    private bool returnOptionsShuffled = false;

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
    public List<Stage> stages;               // 初次點餐對話
    public List<Stage> returnDialogueStages; // 回來交餐對話

    void OnEnable()
    {
        // 剛啟用顧客時：Review UI 一律關閉（圖片1 狀態）
        if (reviewMenuButton != null) reviewMenuButton.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);

        if (randomSeed >= 0) Random.InitState(randomSeed);

        if (!returningWithFood)
        {
            if (!optionsShuffled)
            {
                ShuffleOptionsInEachStage(stages);
                optionsShuffled = true;
            }
            currentStage = 0;
            ShowCurrentStage();
        }
        else
        {
            if (!returnOptionsShuffled)
            {
                ShuffleOptionsInEachStage(returnDialogueStages);
                returnOptionsShuffled = true;
            }
            ShowCurrentStage();
        }
    }

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
        statementText.text = stage.question;

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var button = optionButtons[i];
                button.gameObject.SetActive(true);

                var textComp = button.GetComponentInChildren<TextMeshProUGUI>();
                var imageComp = button.GetComponentInChildren<Image>();

                var opt = stage.options[i];
                textComp.text = opt.text ?? "";

                if (opt.image != null)
                {
                    imageComp.sprite = opt.image;
                    imageComp.enabled = true;
                }
                else
                {
                    imageComp.enabled = false;
                }

                button.onClick.RemoveAllListeners();
                int capturedIndex = i;
                button.onClick.AddListener(() => StartCoroutine(OnOptionSelected(capturedIndex)));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 只處理選項點擊、判斷對錯和流程推進
    IEnumerator OnOptionSelected(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];

        // 暫時關閉按鈕防止連點
        ToggleOptionButtons(false);

        bool isCorrect = (index == stage.correctIndex);

        // 第一次作答才記錄
        if (firstAttemptPending && customerManager != null)
        {
            customerManager.RegisterFirstAttempt(isCorrect);
            firstAttemptPending = false;
        }

        if (!isCorrect)
        {
            // 答錯，顯示 Try again!
            statementText.text = "Hmm... Try again!";
            yield return new WaitForSeconds(1.2f);
            ToggleOptionButtons(true);
            ShowCurrentStage(); // 重新顯示本階段問題
            yield break;
        }

        // 答對 → 進入下一題
        currentStage++;
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }

    void ToggleOptionButtons(bool interactable)
    {
        foreach (var b in optionButtons)
            if (b) b.interactable = interactable;
    }

    void FinishInteraction()
    {
        // 初次向顧客索取餐點流程結束，準備去找員工點餐
        statementText.text = "Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);

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

        // ✅ 從現在開始到回來交餐之前，可以回顧訂單
        if (reviewMenuButton != null) reviewMenuButton.SetActive(true);

        StartCoroutine(DelayedSwitch());
    }

    IEnumerator DelayedSwitch()
    {
        // 以前會把整個顧客關掉，現在只做一些緩衝，如果要可以在這裡多加效果
        yield return new WaitForSeconds(1f);
        if (mainDialoguePanel != null)
            mainDialoguePanel.SetActive(false);
    }

    public void BeginFinalDialogue()
    {
        // 回來交餐
        returningWithFood = true;
        currentStage = 0;

        // 回來交餐後就不需要 Review menu 了
        if (reviewMenuButton != null) reviewMenuButton.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);

        gameObject.SetActive(true);
        if (mainDialoguePanel != null)
            mainDialoguePanel.SetActive(true);
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
            customerManager.ProceedToNextCustomer();
    }

    // ============ Review menu 開關（給 UI Button 用） ============

    // 點「Review menu」按鈕時呼叫
    public void OpenReviewPanel()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(true);
    }

    // 點大面板上的 X 按鈕時呼叫
    public void CloseReviewPanel()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(false);
    }

    // ===================== 洗牌 =====================
    private void ShuffleOptionsInEachStage(List<Stage> list)
    {
        if (list == null || list.Count == 0) return;

        foreach (Stage stage in list)
        {
            if (stage.options == null || stage.options.Count <= 1) continue;

            // 記住原本正確答案
            QAOption correctOption = stage.options[stage.correctIndex];

            // Fisher–Yates 洗牌
            for (int i = stage.options.Count - 1; i > 0; i--)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                var tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }

            // 再加入一次隨機偏移
            int offset = UnityEngine.Random.Range(0, stage.options.Count);
            if (offset > 0)
            {
                var rotated = new List<QAOption>();
                rotated.AddRange(stage.options.GetRange(offset, stage.options.Count - offset));
                rotated.AddRange(stage.options.GetRange(0, offset));
                stage.options = rotated;
            }

            // 重新定位正確答案索引
            for (int j = 0; j < stage.options.Count; j++)
            {
                if (stage.options[j] == correctOption)
                {
                    stage.correctIndex = j;
                    break;
                }
            }
        }
    }
}
