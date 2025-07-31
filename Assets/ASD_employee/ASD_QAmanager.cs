using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ASD_QAmanager : MonoBehaviour
{
    public TextMeshProUGUI statementText;
    public List<Button> optionButtons;
    public Button restartButton;

    public ASD_SingleCustomer singleCustomer;

    [Header("Path Management")]
    public DestinationLineDrawer drawer;
    public Transform nextCustomer;
    public UnityEngine.AI.NavMeshAgent agentForThisRoute;

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

    private int currentStage = 0;
    private bool isCorrect = false;

    void Start()
    {
        restartButton.gameObject.SetActive(false);
        BindButtons();
        ShowCurrentStage();
    }

    void BindButtons()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => StartCoroutine(OnOptionSelected(idx)));
        }

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartQuiz);
    }

    void ShowCurrentStage()
    {
        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                var textComp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                var imageComp = optionButtons[i].GetComponentInChildren<Image>();

                textComp.text = stage.options[i].text;
                if (stage.options[i].image != null)
                    imageComp.sprite = stage.options[i].image;
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator OnOptionSelected(int index)
    {
        Stage stage = stages[currentStage];

        if (index == stage.correctIndex)
        {
            currentStage++;

            if (currentStage < stages.Count)
            {
                yield return new WaitForSeconds(1f);
                ShowCurrentStage();
            }
            else
            {
                statementText.text = "Thank you!";
                foreach (var btn in optionButtons)
                    btn.gameObject.SetActive(false);

                yield return new WaitForSeconds(1f);

                // ✅ 切換導航路線到下一位顧客
                if (drawer != null)
                {
                    if (nextCustomer != null)
                        drawer.ChangeDestination(nextCustomer);

                    if (agentForThisRoute != null)
                        drawer.ChangeNavAgent(agentForThisRoute);
                }

                gameObject.SetActive(false);

                if (singleCustomer != null)
                    singleCustomer.NotifyCustomerManager();
            }
        }
        else
        {
            statementText.text = "Hmm... Try again";
            yield return new WaitForSeconds(1f);
            statementText.text = stage.question;
        }
    }

    void RestartQuiz()
    {
        currentStage = 0;
        restartButton.gameObject.SetActive(false);
        ShowCurrentStage();
    }
}
