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

    [Header("Speech")]
    public SpeechPopup speechPopup;   // ✅ 跟顧客一樣用的語音面板（內含 Speak / Done）

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

    // 暫存本題被點擊的選項索引與最後辨識文字（若你想記錄可用）
    private int pendingSelectedIndex = -1;
    private string lastRecognizedText = "";

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
        firstAttemptPending = true;
        pendingSelectedIndex = -1;
        lastRecognizedText = "";

        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        var stage = stages[currentStage];
        statementText.text = "Clerk: " + stage.question;

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
                btn.onClick.AddListener(() => OnOptionClicked(captured));
            }
            else
            {
                optionAdvancedButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnOptionClicked(int index)
    {
        var stage = stages[currentStage];
        pendingSelectedIndex = index;

        // 點了選項後：開啟語音面板，顯示要念的句子（就用該選項文字）
        string sentenceToSpeak = stage.options[index].text ?? "";

        // 鎖住選項，避免語音期間亂點
        SetOptionsInteractable(false);

        // 顧客同款：ShowSentence(句子, Done回呼)
        speechPopup.ShowSentence(sentenceToSpeak, (recognized) =>
        {
            lastRecognizedText = recognized ?? "";

            // ✅ 玩家按下 Done，此時才判斷正確與否
            bool isCorrect = (pendingSelectedIndex == stage.correctIndex);

            // 第一次作答統計
            if (firstAttemptPending && gameflow != null)
            {
                gameflow.RegisterFirstAttempt(isCorrect);
                firstAttemptPending = false;
            }

            if (isCorrect)
            {
                statementText.text = "Clerk: Great job!";
                StartCoroutine(NextQuestion());
            }
            else
            {
                statementText.text = "Clerk: Hmm... Try again!";
                StartCoroutine(RetryAfterDelay());
            }
        });
    }

    IEnumerator NextQuestion()
    {
        yield return new WaitForSeconds(1.2f);
        currentStage++;
        SetOptionsInteractable(true);
        ShowCurrentStage();
    }

    IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSeconds(1.2f);
        SetOptionsInteractable(true);
        ShowCurrentStage();
    }

    void FinishQAFlow()
    {
        statementText.text = "Clerk: You're welcome!";
        foreach (var b in optionAdvancedButtons) b.gameObject.SetActive(false);
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
            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                var tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }
            for (int j = 0; j < stage.options.Count; j++)
            {
                if (stage.options[j] == correct) { stage.correctIndex = j; break; }
            }
        }
    }
}
