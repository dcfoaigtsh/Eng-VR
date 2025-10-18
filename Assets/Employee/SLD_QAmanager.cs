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

    [Header("Randomization")]
    [Tooltip("若 >= 0，使用固定亂數種子，讓每次啟動的選項順序一致。")]
    public int randomSeed = -1;

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
    private bool firstAttemptPending = true;

    // ✅ 防止重複洗牌
    private bool optionsShuffled = false;

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
        firstAttemptPending = true;

        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        // === 題目音檔 ===
        if (statementAudioButton != null)
        {
            bool hasQAudio = (stage.questionAudio != null);
            statementAudioButton.gameObject.SetActive(hasQAudio);
            statementAudioButton.onClick.RemoveAllListeners();

            if (hasQAudio)
            {
                statementAudioButton.onClick.AddListener(() =>
                {
                    if (audioSource.isPlaying) audioSource.Stop();
                    audioSource.PlayOneShot(stage.questionAudio);
                });
            }
        }

        // === 顯示選項 ===
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

                // === 若選項有音檔，顯示小喇叭 ===
                if (i < optionAudioButtons.Count)
                {
                    var audioBtn = optionAudioButtons[i];
                    if (audioBtn != null)
                    {
                        bool hasAudio = (stage.options[i].audio != null);
                        audioBtn.gameObject.SetActive(hasAudio);
                        audioBtn.onClick.RemoveAllListeners();
                        if (hasAudio)
                        {
                            audioBtn.onClick.AddListener(() =>
                            {
                                if (audioSource.isPlaying) audioSource.Stop();
                                audioSource.PlayOneShot(stage.options[i].audio);
                            });
                        }
                    }
                }
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

        // ✅ 第一次作答才記錄
        if (firstAttemptPending && gameflow != null)
        {
            bool isCorrectFirstTry = (index == stage.correctIndex);
            gameflow.RegisterFirstAttempt(isCorrectFirstTry);
            firstAttemptPending = false;
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

    // ====================== 核心：隨機打亂選項 ======================
    private void ShuffleOptionsInEachStage()
    {
        if (stages == null || stages.Count == 0) return;

        foreach (Stage stage in stages)
        {
            if (stage.options == null || stage.options.Count <= 1) continue;

            QAOption correctOption = stage.options[stage.correctIndex];

            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                var temp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = temp;
            }

            // 找回正確答案的新位置
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
