using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class ID_SingleCustomer : MonoBehaviour
{
    public TextMeshProUGUI statementText;   
    public List<Button> optionButtons;
    public GameObject completeIcon;
    public ID_Gameflow customerManager;
    public GameObject qaManager;
    public GameObject employeePanel;

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
    }

    [System.Serializable]
    public class Stage
    {
        public string question;
        public List<QAOption> options;
        public int correctIndex;
    }

    public List<Stage> stages;               // 點餐前對話
    public List<Stage> returnDialogueStages; // 回來交餐對話

    void OnEnable()
    {
        currentStage = 0;
        ShowCurrentStage();
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

                textComp.text = stage.options[i].text;

                if (stage.options[i].image != null)
                {
                    imageComp.sprite = stage.options[i].image;
                    imageComp.enabled = true;
                }
                else
                {
                    imageComp.enabled = false;
                }

                optionButtons[i].onClick.RemoveAllListeners();
                int capturedIndex = i;
                optionButtons[i].onClick.AddListener(() => StartCoroutine(OnOptionSelected(capturedIndex)));
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

        if (index == stage.correctIndex)
        {
            currentStage++;
            yield return new WaitForSeconds(0.5f);
            ShowCurrentStage();
        }
        else
        {
            statementText.text = "Hmm... Try again!";
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
        }
    }

    void FinishInteraction()
    {
        statementText.text = "Friend: Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);

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
        if (qaManager != null)
            qaManager.SetActive(true);
        
        if (employeePanel != null)
            employeePanel.SetActive(true);
    }

    // ✅ 店員點餐結束後，從 Gameflow 呼叫這個
    public void BeginFinalDialogue()
    {
        returningWithFood = true;
        currentStage = 0;
        gameObject.SetActive(true);
        ShowCurrentStage();
    }

    // ✅ 回來交餐完畢，顯示勾勾與結束畫面
    void ShowFinalThanks()
    {
        statementText.text = "Friend: Thank you!";
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);

        if (completeIcon != null)
            completeIcon.SetActive(true);

        if (customerManager != null)
            customerManager.ShowGameOverManually();
    }
}
