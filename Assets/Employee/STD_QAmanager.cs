using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class STD_QAmanager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statementText;
    public List<Button> optionAdvancedButtons;

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

    // ✅ 本題是否尚未回報「第一次作答」
    private bool firstAttemptPending = true;

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
    private int currentStage = 0;

    // ✅ 防止重複洗牌
    private bool optionsShuffled = false;

    void Start()
    {
        // ✅ 僅在開始時洗牌所有題目的選項（題目順序不動）
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
        firstAttemptPending = true; // 每題重新開放第一次作答

        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        for (int i = 0; i < optionAdvancedButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var button = optionAdvancedButtons[i];
                button.gameObject.SetActive(true);

                var textComp = button.GetComponentInChildren<TextMeshProUGUI>();
                var imageComps = button.GetComponentsInChildren<Image>();
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
                    {
                        imageComp.enabled = false;
                    }
                }

                button.onClick.RemoveAllListeners();
                int capturedIndex = i;
                button.onClick.AddListener(() => StartCoroutine(OnOptionSelected(capturedIndex)));
            }
            else
            {
                optionAdvancedButtons[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator OnOptionSelected(int index)
    {
        Stage stage = stages[currentStage];

        // ✅ 第一次作答才回報 Gameflow
        if (firstAttemptPending && gameflow != null)
        {
            bool isCorrectFirstTry = (index == stage.correctIndex);
            gameflow.RegisterFirstAttempt(isCorrectFirstTry);
            firstAttemptPending = false; // 鎖定：只記錄一次
        }

        if (index == stage.correctIndex)
        {
            currentStage++;

            if (currentStage >= stages.Count)
            {
                yield return new WaitForSeconds(1f);
                FinishQAFlow();
            }
            else
            {
                yield return new WaitForSeconds(1f);
                ShowCurrentStage();
            }
        }
        else
        {
            statementText.text = "Clerk: Hmm... Try again";
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
        }
    }

    void FinishQAFlow()
    {
        statementText.text = "Clerk: You're welcome!";
        foreach (var btn in optionAdvancedButtons)
            btn.gameObject.SetActive(false);

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

        if (gameflow != null)
        {
            Debug.Log("呼叫 Gameflow 切換到交餐流程！");
            gameflow.NextCustomer();
        }
    }

    // ====================== 核心：隨機打亂選項 ======================
    private void ShuffleOptionsInEachStage()
    {
        if (stages == null || stages.Count == 0) return;

        foreach (Stage stage in stages)
        {
            if (stage.options == null || stage.options.Count <= 1) continue;

            // 保存原本的正確選項
            QAOption correctOption = stage.options[stage.correctIndex];

            // Fisher–Yates 洗牌演算法
            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                QAOption temp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = temp;
            }

            // 找回正確選項的新索引
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
