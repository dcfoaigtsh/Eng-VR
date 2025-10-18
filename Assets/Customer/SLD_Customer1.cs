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

    private int currentStage = 0;
    private bool returningWithFood = false;
    private bool firstAttemptPending = true; // ✅ 加入這個旗標！

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
        if (!returningWithFood)
            currentStage = 0;

        ShowCurrentStage();
    }

    void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
    }

    void ShowCurrentStage()
    {
        firstAttemptPending = true; // ✅ 每題開始時重設旗標！

        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        if (currentStage >= currentList.Count)
        {
            if (!returningWithFood) FinishInteraction();
            else ShowFinalThanks();
            return;
        }

        Stage stage = currentList[currentStage];
        statementText.text = stage.question;

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
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            bool show = (i < stage.options.Count);
            optionButtons[i].gameObject.SetActive(show);
            if (i < optionAudioButtons.Count) optionAudioButtons[i].gameObject.SetActive(false);
            if (!show) continue;

            var opt = stage.options[i];
            var textComp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            var imageComp = optionButtons[i].GetComponentInChildren<Image>(true);

            if (textComp != null) textComp.text = opt.text ?? "";
            if (imageComp != null)
            {
                if (opt.image != null)
                {
                    imageComp.sprite = opt.image;
                    imageComp.enabled = true;
                }
                else imageComp.enabled = false;
            }

            optionButtons[i].onClick.RemoveAllListeners();
            int captured = i;
            optionButtons[i].onClick.AddListener(() => StartCoroutine(OnOptionSelected(captured)));

            if (i < optionAudioButtons.Count)
            {
                bool hasAudio = (opt.audio != null);
                optionAudioButtons[i].gameObject.SetActive(hasAudio);
                optionAudioButtons[i].interactable = hasAudio;

                optionAudioButtons[i].onClick.RemoveAllListeners();
                if (hasAudio)
                {
                    optionAudioButtons[i].onClick.AddListener(() =>
                    {
                        if (audioSource.isPlaying) audioSource.Stop();
                        audioSource.PlayOneShot(opt.audio);
                    });
                }
            }
        }

        for (int i = stage.options.Count; i < optionAudioButtons.Count; i++)
        {
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
            statementText.text = "Hmm... Try again!";
            yield return new WaitForSeconds(wrongHintDelay);
            ShowCurrentStage();
        }
    }

    void FinishInteraction()
    {
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
        currentStage = 0;
        gameObject.SetActive(true);
        ShowCurrentStage();
    }

    void ShowFinalThanks()
    {
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
}
