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

    [Header("Speech UI")]
    public SpeechPopup speechPopup;   // ✅ 語音對話框（上句/下句/Speak）

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

    IEnumerator OnOptionSelected(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];

        // 第一次作答才記錄（保持你的統計機制）
        if (firstAttemptPending && customerManager != null)
        {
            bool isCorrectFirstTry = (index == stage.correctIndex);
            customerManager.RegisterFirstAttempt(isCorrectFirstTry);
            firstAttemptPending = false;
        }

        // ✅ 判定正確與否
        bool choiceIsCorrect = (index == stage.correctIndex);
        string targetSentence = stage.options[index].text ?? "";

        // ❌ 錯誤選項：不開語音，直接提示
        if (!choiceIsCorrect)
        {
            statementText.text = "Hmm... Try again!";
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
            yield break;
        }

        // ✅ 正確選項：開始語音流程
        ToggleOptionButtons(false);

        bool finished = false;
        speechPopup.Show(targetSentence, (spokenFinal) =>
        {
            finished = true;
        });

        // 等待玩家完成錄音
        while (!finished) yield return null;

        // 等待顯示文字 3 秒
        yield return new WaitForSeconds(3f);
        speechPopup.Hide();

        // 進入下一題
        currentStage++;
        ToggleOptionButtons(true);
        yield return new WaitForSeconds(0.3f);
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

            QAOption correctOption = stage.options[stage.correctIndex];

            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                QAOption tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }

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
