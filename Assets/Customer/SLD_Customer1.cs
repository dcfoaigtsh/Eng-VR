using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class SLD_SingleCustomer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statementText;
    public Button statementAudioButton;
    public List<Button> optionButtons;
    public List<Button> optionAudioButtons;

    [Header("Flow Hooks")]
    public SLD_Gameflow customerManager;
    public GameObject completeIcon;

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee1;
    public NavMeshAgent agentForThisRoute;

    [Header("Audio")]
    public AudioSource audioSource;
    public float wrongHintDelay = 1f;
    public float stageAdvanceDelay = 0.5f;

    [Header("Randomization (optional)")]
    [Tooltip("若 >= 0，使用固定 seed 讓洗牌可重現；-1 則使用預設隨機。")]
    public int randomSeed = -1;

    private int currentStage = 0;
    private bool returningWithFood = false;
    private bool firstAttemptPending = true; // ✅ 只記錄第一次作答

    // 只洗牌一次的旗標
    private bool mainOptionsShuffled = false;
    private bool returnOptionsShuffled = false;

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

    [Header("Dialogue Data")]
    public List<Stage> stages;
    public List<Stage> returnDialogueStages;

    void Awake()
    {
        if (audioSource == null)
        {
            var go = new GameObject("SLD_OptionAudioSource");
            go.transform.SetParent(transform);
            audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void OnEnable()
    {
        // 每次啟用時（初次或回程）只洗牌一次該段的選項
        if (randomSeed >= 0) Random.InitState(randomSeed);

        if (!returningWithFood)
        {
            if (!mainOptionsShuffled)
            {
                ShuffleOptionsInEachStage(stages);
                mainOptionsShuffled = true;
            }
            // 初段回到起點時才重置 currentStage
            currentStage = 0;
        }
        else
        {
            if (!returnOptionsShuffled)
            {
                ShuffleOptionsInEachStage(returnDialogueStages);
                returnOptionsShuffled = true;
            }
            // 回程開始從 0
            currentStage = 0;
        }

        ShowCurrentStage();
    }

    void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
    }

    void ShowCurrentStage()
    {
        firstAttemptPending = true; // ✅ 每題開始時重設

        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        if (currentList == null || currentList.Count == 0)
        {
            // 沒資料直接結束
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
        if (stage == null)
        {
            // 防呆：若 stage 為 null，跳下一題
            currentStage++;
            ShowCurrentStage();
            return;
        }

        // 題幹
        if (statementText != null)
            statementText.text = stage.question ?? string.Empty;

        // 題幹音檔
        if (statementAudioButton != null)
        {
            bool hasQAudio = (stage.questionAudio != null);
            statementAudioButton.gameObject.SetActive(true);
            statementAudioButton.onClick.RemoveAllListeners();

            if (hasQAudio)
            {
                statementAudioButton.onClick.AddListener(() =>
                {
                    if (audioSource.isPlaying) audioSource.Stop();
                    audioSource.PlayOneShot(stage.questionAudio);
                });
            }
            else
            {
                // 沒音檔也可選擇隱藏
                // statementAudioButton.gameObject.SetActive(false);
            }
        }

        // 選項 UI
        int optionCount = (stage.options != null) ? stage.options.Count : 0;

        for (int i = 0; i < optionButtons.Count; i++)
        {
            bool show = (i < optionCount);
            var btn = optionButtons[i];
            if (btn != null) btn.gameObject.SetActive(show);

            if (i < optionAudioButtons.Count && optionAudioButtons[i] != null)
                optionAudioButtons[i].gameObject.SetActive(false);

            if (!show) continue;

            var opt = stage.options[i];
            // 綁文字/圖片
            var textComp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            var imageComp = btn.GetComponentInChildren<Image>(true);

            if (textComp != null) textComp.text = opt != null ? (opt.text ?? "") : "";
            if (imageComp != null)
            {
                if (opt != null && opt.image != null)
                {
                    imageComp.sprite = opt.image;
                    imageComp.enabled = true;
                }
                else
                {
                    imageComp.enabled = false;
                }
            }

            // 綁點擊事件
            btn.onClick.RemoveAllListeners();
            int captured = i;
            btn.onClick.AddListener(() => StartCoroutine(OnOptionSelected(captured)));

            // 選項音檔
            if (i < optionAudioButtons.Count)
            {
                var ab = optionAudioButtons[i];
                if (ab != null)
                {
                    bool hasAudio = (opt != null && opt.audio != null);
                    ab.gameObject.SetActive(hasAudio);
                    ab.interactable = hasAudio;
                    ab.onClick.RemoveAllListeners();

                    if (hasAudio)
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

        // 多餘的音檔按鈕關閉
        for (int i = optionCount; i < optionAudioButtons.Count; i++)
        {
            if (optionAudioButtons[i] != null)
                optionAudioButtons[i].gameObject.SetActive(false);
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
            yield return new WaitForSeconds(stageAdvanceDelay);
            ShowCurrentStage();
        }
        else
        {
            if (statementText != null)
                statementText.text = "Hmm... Try again!";
            yield return new WaitForSeconds(wrongHintDelay);
            ShowCurrentStage();
        }
    }

    void FinishInteraction()
    {
        if (statementText != null)
            statementText.text = "Thank you!";
        ToggleAllOptions(false);
        if (statementAudioButton) statementAudioButton.gameObject.SetActive(false);

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
        // 讓回程對話的選項只洗牌一次（在下次 OnEnable 觸發）
        gameObject.SetActive(true);
        ShowCurrentStage();
    }

    void ShowFinalThanks()
    {
        if (statementText != null)
            statementText.text = "Thank you!";
        ToggleAllOptions(false);
        if (statementAudioButton) statementAudioButton.gameObject.SetActive(false);

        if (completeIcon != null) completeIcon.SetActive(true);
        if (customerManager != null) customerManager.ProceedToNextCustomer();
    }

    void ToggleAllOptions(bool show)
    {
        foreach (var btn in optionButtons) if (btn) btn.gameObject.SetActive(show);
        foreach (var ab in optionAudioButtons) if (ab) ab.gameObject.SetActive(show);
    }

    // ======================= 核心：只洗選項、維持題目順序 =======================
    private void ShuffleOptionsInEachStage(List<Stage> list)
    {
        if (list == null || list.Count == 0) return;

        // 用於固定 seed 的情況：避免不同段改變全域亂數序列，可局部產生 index
        for (int s = 0; s < list.Count; s++)
        {
            var stage = list[s];
            if (stage == null || stage.options == null || stage.options.Count <= 1) continue;

            // 記住原本正確選項（以參照判斷）
            QAOption correctRef = stage.options[stage.correctIndex];

            // Fisher–Yates 洗牌
            for (int i = 0; i < stage.options.Count; i++)
            {
                int r = Random.Range(i, stage.options.Count);
                var tmp = stage.options[i];
                stage.options[i] = stage.options[r];
                stage.options[r] = tmp;
            }

            // 更新 correctIndex
            for (int j = 0; j < stage.options.Count; j++)
            {
                if (stage.options[j] == correctRef)
                {
                    stage.correctIndex = j;
                    break;
                }
            }
        }
    }
}
