using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

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

    private int currentStage = 0;
    private bool firstAttemptPending = true;
    private bool optionsShuffled = false;

    [System.Serializable]
    public class QAOption
    {
        public string text;
        public Sprite image;
        public AudioClip audio; // 👈 保留音檔
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public AudioClip questionAudio; // 👈 保留音檔
        public List<QAOption> options;
        public int correctIndex;
    }

    public List<Stage> stages;

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
        if (!optionsShuffled)
        {
            if (randomSeed >= 0) Random.InitState(randomSeed);
            ShuffleOptionsInEachStage();
            optionsShuffled = true;
        }

        ShowCurrentStage();
    }

    // ====================== 顯示題目 ======================
    void ShowCurrentStage()
    {
        firstAttemptPending = true;
        // pendingSelectedIndex = -1; ⚠️ 已刪除
        // lastRecognizedText = ""; ⚠️ 已刪除

        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        // === 題目語音 (保留) ===
        if (statementAudioButton != null)
        {
            bool hasAudio = (stage.questionAudio != null);
            statementAudioButton.gameObject.SetActive(hasAudio);
            statementAudioButton.onClick.RemoveAllListeners();

            if (hasAudio)
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
                var btn = optionAdvancedButtons[i];
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                var textComp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                var imageComps = btn.GetComponentsInChildren<Image>(true);
                var imageComp = imageComps.Length > 1 ? imageComps[1] : null;

                var opt = stage.options[i];
                if (textComp != null) textComp.text = opt.text ?? "";

                if (imageComp != null)
                {
                    if (opt.image != null)
                    {
                        imageComp.sprite = opt.image;
                        imageComp.enabled = true;
                        textComp.alignment = TextAlignmentOptions.Top;
                    }
                    else
                    {
                        imageComp.enabled = false;
                        textComp.alignment = TextAlignmentOptions.Midline;
                    }
                }

                // 綁定選項點擊
                btn.onClick.RemoveAllListeners();
                int capturedIndex = i;
                // 點擊後直接判斷
                btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));

                // === 選項語音喇叭 (保留) ===
                if (i < optionAudioButtons.Count)
                {
                    var ab = optionAudioButtons[i];
                    if (ab != null)
                    {
                        bool hasOptAudio = (opt.audio != null);
                        ab.gameObject.SetActive(hasOptAudio);
                        ab.onClick.RemoveAllListeners();

                        if (hasOptAudio)
                        {
                            ab.onClick.AddListener(() =>
                            {
                                if (audioSource.isPlaying) audioSource.Stop();
                                audioSource.PlayOneShot(opt.audio);
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

    // 🏆 主要修改區域：移除語音面板呼叫和回調等待 🏆
    void OnOptionClicked(int index)
    {
        Stage stage = stages[currentStage];
        
        ToggleOptionButtons(false);

        // 立即判斷是否正確
        bool isCorrect = (index == stage.correctIndex);

        // ✅ 第一次作答才紀錄
        if (firstAttemptPending && gameflow != null)
        {
            gameflow.RegisterFirstAttempt(isCorrect);
            firstAttemptPending = false;
        }

        // ✅ 判斷正確與否
        if (isCorrect)
        {
            StartCoroutine(NextQuestion());
        }
        else
        {
            statementText.text = "Clerk: Hmm... Try again!";
            StartCoroutine(RetryAfterDelay());
        }
    }

    IEnumerator NextQuestion()
    {
        yield return new WaitForSeconds(correctDelay);
        currentStage++;
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }

    IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSeconds(wrongDelay);
        ToggleOptionButtons(true);
        ShowCurrentStage();
    }

    void ToggleOptionButtons(bool interactable)
    {
        foreach (var b in optionAdvancedButtons)
            if (b) b.interactable = interactable;
    }

    // ====================== 結束階段 ======================
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

    // ====================== 選項洗牌 ======================
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