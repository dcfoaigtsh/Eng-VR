using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ID_GameDescription : MonoBehaviour
{
    public GameObject InfomationBoard;
    public TextMeshProUGUI InfoContent;
    public Button NextButton, PreviousButton;
    public GameObject VisualPage;     // 第三頁圖像
    public GameObject VisualPage2;    // 第四頁圖像
    public GameObject CustomerPanel;    // ✅ 最後按 OK 時開啟的朋友面板（例如顧客）

    public List<ID_Info> Infos = new List<ID_Info>();
    public int currentInfo;

    void Awake()
    {
        Infos.Add(new ID_Info()
        {
            Content = "Welcome to 'Order Assistant'!\n\nYou are a helper.\nHelp your friend order food."
        });

        Infos.Add(new ID_Info()
        {
            Content = "How to play:\n1. Talk to your friend.\n2. Talk to the staff.\n3. Return to your friend."
        });

        Infos.Add(new ID_Info()
        {
            Content = "" // 第三頁：圖像頁
        });

        Infos.Add(new ID_Info()
        {
            Content = "" // 第四頁：圖像頁
        });

        Infos.Add(new ID_Info()
        {
            Content = "When you finish the orders,\nreview the words again.\nGood luck!"
        });
    }

    void Start()
    {
        currentInfo = 0;
        InfomationBoard.SetActive(true);
        SetupPageContent();

        // ✅ 改寫 Next 按鈕點擊邏輯
        NextButton.onClick.AddListener(() =>
        {
            if (currentInfo == Infos.Count - 1)
            {
                // 最後一頁，按下 OK：關閉說明，開啟朋友面板
                InfomationBoard.SetActive(false);
                if (CustomerPanel != null)
                    CustomerPanel.SetActive(true);
            }
            else
            {
                TurnPage(1);
            }
        });

        PreviousButton.onClick.AddListener(() => TurnPage(-1));

        UpdateButtonVisibility();
    }

    void SetupPageContent()
    {
        // 先關閉所有圖像頁，避免殘留
        if (VisualPage != null) VisualPage.SetActive(false);
        if (VisualPage2 != null) VisualPage2.SetActive(false);

        if (currentInfo == 2 && VisualPage != null)
        {
            InfoContent.text = "";
            VisualPage.SetActive(true);
        }
        else if (currentInfo == 3 && VisualPage2 != null)
        {
            InfoContent.text = "";
            VisualPage2.SetActive(true);
        }
        else
        {
            InfoContent.text = Infos[currentInfo].Content;
        }
    }

    void TurnPage(int dir)
    {
        currentInfo += dir;
        currentInfo = Mathf.Clamp(currentInfo, 0, Infos.Count - 1);
        SetupPageContent();
        UpdateButtonVisibility();
    }

    void UpdateButtonVisibility()
    {
        PreviousButton.gameObject.SetActive(currentInfo != 0);

        // ✅ 最後一頁：Next 按鈕改為 OK
        if (currentInfo == Infos.Count - 1)
        {
            NextButton.gameObject.SetActive(true);
            var text = NextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = "OK";
        }
        else
        {
            NextButton.gameObject.SetActive(true);
            var text = NextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = "Next";
        }
    }
    public void OpenInfoFromButton()
    {
        InfomationBoard.SetActive(true);
        currentInfo = 0;
        SetupPageContent();
        UpdateButtonVisibility();
    }
}


[System.Serializable]
public class ID_Info
{
    public string Title;
    public string Content;
}
