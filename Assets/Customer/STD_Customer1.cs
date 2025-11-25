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

    // [Header("Speech UI")]

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
    public List<Stage> stages; // 初次點餐對話
    public List<Stage> returnDialogueStages; // 回來交餐對話

    void OnEnable()
    {
        if (randomSeed >= 0) Random.InitState(randomSeed);

        // ⚠️ 移除 speechPopup.ClosePanel(); 介面清理邏輯

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
            gameflow.NotifyCustomerInteractionStarted();
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

    // 此協程現在只處理選項點擊、判斷對錯和流程推進
    IEnumerator OnOptionSelected(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];

        // 暫時關閉按鈕防止連點
        ToggleOptionButtons(false);
        
        // 直接判斷是否正確
        bool isCorrect = (index == stage.correctIndex);

        // 第一次作答才記錄
        if (firstAttemptPending && customerManager != null)
        {
            customerManager.RegisterFirstAttempt(isCorrect);
            firstAttemptPending = false;
        }

        if (!isCorrect)
        {
            // ❌ 答錯，顯示 Try again!
            statementText.text = "Hmm... Try again!";
            yield return new WaitForSeconds(1.2f);
            ToggleOptionButtons(true);
            ShowCurrentStage(); // 重新顯示本階段問題
            yield break;
        }

        // ✅ 答對 → 進入下一題
        
        currentStage++;
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }
    
    void ToggleOptionButtons(bool interactable)
    {
        foreach (var b in optionButtons) if (b) b.interactable = interactable;
    }

    void FinishInteraction()
    {
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
            customerManager.ProceedToNextCustomer();
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

            // 🔹 用 Fisher–Yates 洗牌
            for (int i = stage.options.Count - 1; i > 0; i--)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                var tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }

            // 🔹 再加入一次隨機偏移（增加變化性）
            int offset = UnityEngine.Random.Range(0, stage.options.Count);
            if (offset > 0)
            {
                var rotated = new List<QAOption>();
                rotated.AddRange(stage.options.GetRange(offset, stage.options.Count - offset));
                rotated.AddRange(stage.options.GetRange(0, offset));
                stage.options = rotated;
            }

            // 🔹 重新定位正確答案索引
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