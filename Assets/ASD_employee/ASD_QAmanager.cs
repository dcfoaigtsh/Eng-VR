using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 管理 ASD 模式下的問答流程
public class ASD_QAmanager : MonoBehaviour
{
    public TextMeshProUGUI statementText; // 顯示問題文字的 UI 元件
    public List<Button> optionButtons; // 顯示選項按鈕的清單

    public ASD_SingleCustomer singleCustomer; // 通知單一顧客流程的腳本參考

    [Header("Path Management")]
    public DestinationLineDrawer drawer; // 路線繪製管理器
    public Transform nextCustomer; // 下一位顧客的位置
    public UnityEngine.AI.NavMeshAgent agentForThisRoute; // 對應的導航代理

    // 問題選項資料結構
    [System.Serializable]
    public class QAOption
    {
        public string text;   // 選項文字
        public Sprite image;  // 選項圖片（可為空）
    }

    // 單一階段的資料結構
    [System.Serializable]
    public class Stage
    {
        public string question;             // 問題描述
        public List<QAOption> options;      // 該階段的選項清單
        public int correctIndex;            // 正確選項的索引
    }

    public List<Stage> stages; // 所有階段的清單

    private int currentStage = 0; // 當前階段索引

    void Start()
    {
        BindButtons(); // 綁定按鈕事件
        ShowCurrentStage(); // 顯示第一題
    }

    // 綁定所有按鈕的點擊事件
    void BindButtons()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => StartCoroutine(OnOptionSelected(idx)));
        }
    }

    // 顯示目前的題目與選項
    void ShowCurrentStage()
    {
        Stage stage = stages[currentStage];
        statementText.text = stage.question;

        bool isFinalStage = currentStage == stages.Count - 1; // 判斷是否為最後一題

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < stage.options.Count)
            {
                optionButtons[i].gameObject.SetActive(true);

                var textComp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                var imageComp = optionButtons[i].GetComponentInChildren<Image>();

                textComp.text = stage.options[i].text;
                if (!isFinalStage && stage.options[i].image != null)
                {
                    imageComp.enabled = true;
                    imageComp.sprite = stage.options[i].image;
                }
                else
                {
                    imageComp.enabled = false;
                }
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false); // 若無對應選項，隱藏按鈕
            }
        }
    }

    // 選擇選項後的處理流程
    IEnumerator OnOptionSelected(int index)
    {
        Stage stage = stages[currentStage];

        if (index == stage.correctIndex)
        {
            currentStage++;

            if (currentStage < stages.Count)
            {
                yield return new WaitForSeconds(1f);
                ShowCurrentStage(); // 顯示下一題
            }
            else
            {
                // 所有題目答完
                statementText.text = "You welcome!";
                foreach (var btn in optionButtons)
                    btn.gameObject.SetActive(false);

                yield return new WaitForSeconds(1f);

                // 切換路線指向下一位顧客
                if (drawer != null)
                {
                    if (nextCustomer != null)
                        drawer.ChangeDestination(nextCustomer);

                    if (agentForThisRoute != null)
                        drawer.ChangeNavAgent(agentForThisRoute);
                }

                gameObject.SetActive(false); // 關閉問答面板

                if (singleCustomer != null)
                    singleCustomer.BeginFinalDialogue(); // 通知流程回到顧客
            }
        }
        else
        {
            // 選錯的回饋
            statementText.text = "Hmm... Try again";
            yield return new WaitForSeconds(1f);
            statementText.text = stage.question; // 再顯示原本問題
        }
    }
}
