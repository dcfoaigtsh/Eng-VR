using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 管理 ASD 模式下的問答流程（含語音辨識）
public class ASD_QAmanager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statementText;
    public List<Button> optionAdvancedButtons;

    [Header("Speech")]
    public SpeechPopup speechPopup;  // ✅ 語音面板（含 Speak / Done / userText）

    [Header("Flow References")]
    public ASD_SingleCustomer singleCustomer;

    [Header("Path Management")]
    public DestinationLineDrawer drawer;
    public Transform nextCustomer;
    public UnityEngine.AI.NavMeshAgent agentForThisRoute;

    // 控制邏輯
    private bool firstAttemptPending = true;
    private int currentStage = 0;
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
        ShowCurrentStage();
    }

    // ======================================
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
                btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));
            }
            else
            {
                optionAdvancedButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // ======================================
    void OnOptionClicked(int index)
    {
        Stage stage = stages[currentStage];
        pendingSelectedIndex = index;

        // 鎖住選項避免連點
        ToggleOptionButtons(false);

        string sentenceToSpeak = stage.options[index].text ?? "";

        // ✅ 顯示語音面板，讓玩家說出答案
        speechPopup.ShowSentence(sentenceToSpeak, (recognized) =>
        {
            lastRecognizedText = recognized ?? "";

            bool isCorrect = (pendingSelectedIndex == stage.correctIndex);

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
        });
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
