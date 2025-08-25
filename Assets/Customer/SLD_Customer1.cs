using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class SLD_SingleCustomer : MonoBehaviour
{
    public TextMeshProUGUI statementText;
    public List<Button> optionButtons;
    public List<GameObject> optionAudioIcons; // ✅ 對應喇叭圖示

    public SLD_Gameflow customerManager;
    public GameObject completeIcon;

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee1;
    public NavMeshAgent agentForThisRoute;

    private int currentStage = 0;
    private bool returningWithFood = false;

    [System.Serializable]
    public class QAOption
    {
        public string text;
        public Sprite image;
        public AudioClip audio; // ✅ 新增音效欄位
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public List<QAOption> options;
        public int correctIndex;
    }

    public List<Stage> stages;
    public List<Stage> returnDialogueStages;

    void OnEnable()
    {
        if (returningWithFood)
        {
            ShowCurrentStage();
        }
        else
        {
            currentStage = 0;
            ShowCurrentStage();
        }
    }

    void ShowCurrentStage()
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;

        if (currentStage >= currentList.Count)
        {
            if (!returningWithFood)
            {
                FinishInteraction();
            }
            else
            {
                ShowFinalThanks();
            }
            return;
        }

        Stage stage = currentList[currentStage];
        statementText.text = stage.question;

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                optionButtons[i].gameObject.SetActive(true);

                var textComp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                var imageComp = optionButtons[i].GetComponentInChildren<Image>();

                string optionText = stage.options[i].text;
                textComp.text = optionText;

                if (stage.options[i].image != null)
                {
                    imageComp.sprite = stage.options[i].image;
                    imageComp.enabled = true;
                }
                else
                {
                    imageComp.enabled = false;
                }

                // ✅ 根據文字是否為空，決定是否顯示音效圖示
                if (i < optionAudioIcons.Count)
                {
                    bool hasText = !string.IsNullOrWhiteSpace(optionText);
                    optionAudioIcons[i].SetActive(hasText);
                }

                if (stage.options[i].audio != null)
                {
                    var audioSource = optionAudioIcons[i].GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = optionAudioIcons[i].gameObject.AddComponent<AudioSource>();
                    }
                    audioSource.clip = stage.options[i].audio;

                    // 移除舊的監聽器，避免重複添加
                    var audioButton = optionAudioIcons[i].GetComponent<Button>();
                    if (audioButton == null)
                    {
                        audioButton = optionAudioIcons[i].gameObject.AddComponent<Button>();
                    }
                    audioButton.onClick.RemoveAllListeners();
                    audioButton.onClick.AddListener(() => audioSource.Play());
                }
                else
                {
                    var audioSource = optionAudioIcons[i].GetComponent<AudioSource>();
                    audioSource.clip = null;
                    optionAudioIcons[i].GetComponent<Button>().onClick.RemoveAllListeners();
                }

                optionButtons[i].onClick.RemoveAllListeners();
                int capturedIndex = i;
                optionButtons[i].onClick.AddListener(() => StartCoroutine(OnOptionSelected(capturedIndex)));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
                if (i < optionAudioIcons.Count)
                    optionAudioIcons[i].SetActive(false);
            }
        }
    }

    IEnumerator OnOptionSelected(int index)
    {
        List<Stage> currentList = returningWithFood ? returnDialogueStages : stages;
        Stage stage = currentList[currentStage];

        if (index == stage.correctIndex)
        {
            currentStage++;
            yield return new WaitForSeconds(0.5f);
            ShowCurrentStage();
        }
        else
        {
            statementText.text = "Friend:mm... Try again!";
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
        }
    }

    void FinishInteraction()
    {
        statementText.text = "Friend:Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);
        foreach (var icon in optionAudioIcons)
            icon.SetActive(false);

        if (drawer != null)
        {
            if (employee1 != null)
                drawer.ChangeDestination(employee1);
            if (agentForThisRoute != null)
                drawer.ChangeNavAgent(agentForThisRoute);
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
        statementText.text = "Friend:Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);
        foreach (var icon in optionAudioIcons)
            icon.SetActive(false);

        if (completeIcon != null)
            completeIcon.SetActive(true);
        if (customerManager != null)
            customerManager.ProceedToNextCustomer();
    }
}
