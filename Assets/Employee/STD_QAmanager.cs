using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class STD_QAmanager : MonoBehaviour
{
    public TextMeshProUGUI statementText;
    public List<Button> optionAdvancedButtons;

    public STD_SingleCustomer singleCustomer;
    public STD_Gameflow gameflow;

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

    void Start()
    {
        ShowCurrentStage();
    }

    void ShowCurrentStage()
    {
        if (currentStage >= stages.Count)
        {
            FinishQAFlow();
            return;
        }

        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        for (int i = 0; i < optionAdvancedButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                var button = optionAdvancedButtons[i];
                button.gameObject.SetActive(true);

                var textComp = button.GetComponentInChildren<TextMeshProUGUI>();
                var imageComps = button.GetComponentsInChildren<Image>();
                var imageComp = imageComps.Length > 1 ? imageComps[1] : null;

                if (textComp != null)
                    textComp.text = stage.options[i].text;

                if (stage.options[i].image != null && imageComp != null)
                {
                    imageComp.sprite = stage.options[i].image;
                    imageComp.enabled = true;
                }
                else if (imageComp != null)
                {
                    imageComp.enabled = false;
                }

                button.onClick.RemoveAllListeners();
                int capturedIndex = i;
                button.onClick.AddListener(() => StartCoroutine(OnOptionSelected(capturedIndex)));
            }
            else
            {
                optionAdvancedButtons[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator OnOptionSelected(int index)
    {
        Stage stage = stages[currentStage];

        if (index == stage.correctIndex)
        {
            currentStage++;

            if (currentStage >= stages.Count)
            {
                yield return new WaitForSeconds(1f);
                FinishQAFlow();
            }
            else
            {
                yield return new WaitForSeconds(1f);
                ShowCurrentStage();
            }
        }
        else
        {
            statementText.text = "Employee:Hmm... Try again";
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
        }
    }

    void FinishQAFlow()
    {
        statementText.text = "Employee:You're welcome!";
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

        if (gameflow != null)
        {
            Debug.Log("呼叫 Gameflow 切換到交餐流程！");
            gameflow.NextCustomer(); // ✅ 呼叫顧客進入交餐對話
        }
    }
}
