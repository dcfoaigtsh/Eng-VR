using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class STD_SingleCustomer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statementText;
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

    private int currentStage = 0;
    private bool returningWithFood = false;
    private bool firstAttemptPending = true;

    // ✅ 避免重複洗牌
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
        firstAttemptPending = true;

        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;

        if (currentList == null || currentList.Count == 0)
        {
            if (!returningWithFood)
                FinishInteraction();
            else
                ShowFinalThanks();
            return;
        }

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

    IEnumerator OnOptionSelected(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];

        // ✅ 第一次作答才記錄
        if (firstAttemptPending && customerManager != null)
        {
            bool isCorrectFirstTry = (index == stage.correctIndex);
            customerManager.RegisterFirstAttempt(isCorrectFirstTry);
            firstAttemptPending = false;
        }

        if (index == stage.correctIndex)
        {
            currentStage++;
            yield return new WaitForSeconds(0.5f);
            ShowCurrentStage();
        }
        else
        {
            statementText.text = "Hmm... Try again!";
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
        }
    }

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

    // ===================== ✅ 核心：隨機打亂每題選項 =====================
    private void ShuffleOptionsInEachStage(List<Stage> list)
    {
        if (list == null || list.Count == 0) return;

        foreach (Stage stage in list)
        {
            if (stage.options == null || stage.options.Count <= 1) continue;

            // 記住原本正確選項
            QAOption correctOption = stage.options[stage.correctIndex];

            // Fisher–Yates 洗牌
            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                QAOption tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }

            // 重新找回正確選項的新索引
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
