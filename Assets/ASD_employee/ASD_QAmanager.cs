using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 管理 ASD 模式下的問答流程
public class ASD_QAmanager : MonoBehaviour
{
    public TextMeshProUGUI statementText; // 顯示問題文字的 UI 元件
    public List<Button> optionAdvancedButtons; // 使用新版按鈕（含圖片）

    public ASD_SingleCustomer singleCustomer; // 通知顧客流程
    [Header("Path Management")]
    public DestinationLineDrawer drawer;         // 路線繪製
    public Transform nextCustomer;               // 下一位顧客的目的地
    public UnityEngine.AI.NavMeshAgent agentForThisRoute; // 導航代理

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

    // 顯示目前的題目與選項
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

    // 回答選項時的處理
    IEnumerator OnOptionSelected(int index)
    {
        Stage stage = stages[currentStage];

        if (index == stage.correctIndex)
        {
            currentStage++;
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
        }
        else
        {
            statementText.text = "Hmm... Try again";
            yield return new WaitForSeconds(1f);
            ShowCurrentStage();
        }
    }

    // 所有題目完成時
    void FinishQAFlow()
    {
        statementText.text = "You're welcome!";
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

        if (singleCustomer != null)
            singleCustomer.BeginFinalDialogue();
    }
}
