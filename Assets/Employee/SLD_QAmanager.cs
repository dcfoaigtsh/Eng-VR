using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SLD_QAmanager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statementText;
    public Button statementAudioButton;          // ✅ 題目喇叭
    public List<Button> optionAdvancedButtons;   // 主選項（文字＋圖片）
    public List<Button> optionAudioButtons;      // ✅ 選項喇叭（Button）

    [Header("Flow References")]
    public SLD_SingleCustomer singleCustomer;
    public SLD_Gameflow gameflow;

    [Header("Path Management")]
    public DestinationLineDrawer drawer;
    public Transform nextCustomer;
    public UnityEngine.AI.NavMeshAgent agentForThisRoute;

    [Header("Audio")]
    public AudioSource audioSource;              // ✅ 共用音源
    public float correctDelay = 1f;
    public float wrongDelay = 1f;

    // ---------- 資料結構 ----------
    [System.Serializable]
    public class QAOption
    {
        public string text;
        public Sprite image;
        public AudioClip audio; // ✅ 選項音檔
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public AudioClip questionAudio; // ✅ 題目音檔
        public List<QAOption> options;
        public int correctIndex;
    }

    public List<Stage> stages;
    private int currentStage = 0;

    // ---------- 初始化 ----------
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

    // ---------- 顯示題目 ----------
    void ShowCurrentStage()
    {
        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        // ✅ 題目喇叭設定
        if (statementAudioButton != null)
        {
            bool hasAudio = (stage.questionAudio != null);
            statementAudioButton.gameObject.SetActive(true);
            statementAudioButton.interactable = hasAudio;

            var img = statementAudioButton.GetComponent<Image>();
            if (img != null)
                img.color = hasAudio ? Color.white : new Color(1f, 1f, 1f, 0.4f);

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

        // ✅ 選項顯示與喇叭設定
        for (int i = 0; i < optionAdvancedButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var button = optionAdvancedButtons[i];
                button.gameObject.SetActive(true);

                var textComp = button.GetComponentInChildren<TextMeshProUGUI>(true);
                var imageComps = button.GetComponentsInChildren<Image>(true);
                var imageComp = imageComps.Length > 1 ? imageComps[1] : null;

                if (textComp != null)
                    textComp.text = stage.options[i].text ?? "";

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

                // ✅ 綁定主選項答題事件
                button.onClick.RemoveAllListeners();
                int capturedIndex = i;
                button.onClick.AddListener(() => StartCoroutine(OnOptionSelected(capturedIndex)));

                // ✅ 喇叭播放音檔
                if (i < optionAudioButtons.Count && optionAudioButtons[i] != null)
                {
                    var audioBtn = optionAudioButtons[i];
                    bool hasOptAudio = (stage.options[i].audio != null);

                    audioBtn.gameObject.SetActive(true);
                    audioBtn.interactable = hasOptAudio;

                    var abImg = audioBtn.GetComponent<Image>();
                    if (abImg != null)
                        abImg.color = hasOptAudio ? Color.white : new Color(1f, 1f, 1f, 0.4f);

                    audioBtn.onClick.RemoveAllListeners();
                    if (hasOptAudio)
                    {
                        AudioClip clip = stage.options[i].audio;
                        audioBtn.onClick.AddListener(() =>
                        {
                            if (audioSource.isPlaying) audioSource.Stop();
                            audioSource.PlayOneShot(clip);
                        });
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

        // 關閉多餘喇叭
        for (int i = stage.options.Count; i < optionAudioButtons.Count; i++)
        {
            if (optionAudioButtons[i] != null)
                optionAudioButtons[i].gameObject.SetActive(false);
        }
    }

    // ---------- 答題邏輯 ----------
    IEnumerator OnOptionSelected(int index)
    {
        Stage stage = stages[currentStage];

        if (index == stage.correctIndex)
        {
            currentStage++;
            yield return new WaitForSeconds(correctDelay);
            ShowCurrentStage();
        }
        else
        {
            statementText.text = "Employee: Hmm... Try again";
            yield return new WaitForSeconds(wrongDelay);
            ShowCurrentStage();
        }
    }

    // ---------- 結束互動 ----------
    void FinishQAFlow()
    {
        statementText.text = "Employee: You're welcome!";
        foreach (var btn in optionAdvancedButtons)
            btn.gameObject.SetActive(false);
        foreach (var ab in optionAudioButtons)
            if (ab) ab.gameObject.SetActive(false);
        if (statementAudioButton) statementAudioButton.gameObject.SetActive(false);

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
}
