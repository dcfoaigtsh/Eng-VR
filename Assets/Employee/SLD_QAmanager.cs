using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SLD_QAmanager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statementText;
    public Button statementAudioButton;
    public List<Button> optionAdvancedButtons;
    public List<Button> optionAudioButtons;

    [Header("Flow References")]
    public SLD_SingleCustomer singleCustomer;
    public SLD_Gameflow gameflow;

    [Header("Path Management")]
    public DestinationLineDrawer drawer;
    public Transform nextCustomer;
    public UnityEngine.AI.NavMeshAgent agentForThisRoute;

    [Header("Audio")]
    public AudioSource audioSource;
    public float correctDelay = 1f;
    public float wrongDelay = 1f;

    [System.Serializable]
    public class QAOption
    {
        public string text;
        public Sprite image;
        public AudioClip audio;
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public AudioClip questionAudio;
        public List<QAOption> options;
        public int correctIndex;
    }

    public List<Stage> stages;
    private int currentStage = 0;

    // ✅ 新增：本題是否尚未回報第一次作答
    private bool firstAttemptPending = true;

    void Awake()
    {
        if (audioSource == null)
        {
            GameObject go = new GameObject("SLD_QA_AudioSource");
            go.transform.SetParent(transform);
            audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        ShowCurrentStage();
    }

    void ShowCurrentStage()
    {
        // ✅ 每題開始時，允許記錄「第一次作答」
        firstAttemptPending = true;

        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        // （題目喇叭與圖片邏輯略）

        for (int i = 0; i < optionAdvancedButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var button = optionAdvancedButtons[i];
                button.gameObject.SetActive(true);

                var textComp = button.GetComponentInChildren<TextMeshProUGUI>(true);
                var imageComps = button.GetComponentsInChildren<Image>(true);
                var imageComp = imageComps.Length > 1 ? imageComps[1] : null;

                if (textComp != null) textComp.text = stage.options[i].text ?? "";

                if (imageComp != null)
                {
                    if (stage.options[i].image != null)
                    {
                        imageComp.sprite = stage.options[i].image;
                        imageComp.enabled = true;
                        textComp.alignment = TextAlignmentOptions.Top;
                    }
                    else
                    {
                        imageComp.enabled = false;
                        textComp.alignment = TextAlignmentOptions.Midline;
                    }
                }

                button.onClick.RemoveAllListeners();
                int capturedIndex = i;
                button.onClick.AddListener(() => StartCoroutine(OnOptionSelected(capturedIndex)));
            }
            else
            {
                optionAdvancedButtons[i].gameObject.SetActive(false);
                if (i < optionAudioButtons.Count && optionAudioButtons[i] != null)
                    optionAudioButtons[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator OnOptionSelected(int index)
    {
        Stage stage = stages[currentStage];

        // ✅ 只在本題「第一次」作答時回報到 Gameflow
        if (firstAttemptPending && gameflow != null)
        {
            bool isCorrectFirstTry = (index == stage.correctIndex);
            gameflow.RegisterFirstAttempt(isCorrectFirstTry);
            firstAttemptPending = false; // 鎖住，不再重複記數
        }

        if (index == stage.correctIndex)
        {
            currentStage++;
            yield return new WaitForSeconds(correctDelay);
            ShowCurrentStage();
        }
        else
        {
            statementText.text = "Clerk: Hmm... Try again";
            yield return new WaitForSeconds(wrongDelay);
            ShowCurrentStage();
        }
    }

    void FinishQAFlow()
    {
        statementText.text = "Clerk: You're welcome!";
        foreach (var btn in optionAdvancedButtons) btn.gameObject.SetActive(false);
        foreach (var ab in optionAudioButtons) if (ab) ab.gameObject.SetActive(false);
        if (statementAudioButton) statementAudioButton.gameObject.SetActive(false);

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

        if (gameflow != null)
        {
            Debug.Log("呼叫 Gameflow 切換到交餐流程！");
            gameflow.NextCustomer();
        }
    }
}
