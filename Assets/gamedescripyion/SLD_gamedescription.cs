using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SLD_GameDescription : GameDescription
{
    public GameObject InfomationBoard;
    public Button CloseButton, NextButton, PreviousButton;
    public GameObject VisualPage;   // 圖像頁 1
    public GameObject VisualPage2;  // 圖像頁 2

    public List<SLD_Info> Infos = new List<SLD_Info>();

    // ----------------- 初始化文字內容 -----------------
    void Awake()
    {
        Infos.Add(new SLD_Info()
        {
            Content = "Welcome to 'Order Assistant'!\n\nYou are a helper.\nYou will help your friend order food."
        });

        Infos.Add(new SLD_Info()
        {
            Content = "How to play:\n1. Click the ! talk to your friend.\n2. Talk to the staff.\n3. Return to your friend with food."
        });

        Infos.Add(new SLD_Info()
        {
            Content = "" // 圖像頁
        });

        Infos.Add(new SLD_Info()
        {
            Content = "" // 圖像頁
        });

        Infos.Add(new SLD_Info()
        {
            Content = "Once you finish taking orders, your task is completed.\nThen, review the words again.\nGood luck!"
        });
    }

    // ----------------- 綁定按鈕事件（只跑一次） -----------------
    void Start()
    {
        NextButton.onClick.AddListener(() => TurnPage(1));
        PreviousButton.onClick.AddListener(() => TurnPage(-1));
        CloseButton.onClick.AddListener(() => InfomationBoard.SetActive(false));
    }

    // ----------------- 每次打開說明時，回到第一頁 -----------------
    void OnEnable()
    {
        currentInfo = 0;                 // 回到第 0 頁
        InfomationBoard.SetActive(true); // 確保 Panel 有打開

        SetupPageContent();
        UpdateButtonVisibility();
    }

    // ----------------- 設定目前頁面的內容 -----------------
    void SetupPageContent()
    {
        Gameflow gameflow = FindObjectOfType<Gameflow>();
        if (gameflow != null)
        {
            gameflow.NotifyReadingDescription();
        }

        // 先關掉所有圖像頁，避免殘留
        if (VisualPage != null) VisualPage.SetActive(false);
        if (VisualPage2 != null) VisualPage2.SetActive(false);

        // 第 2、3 頁為圖像頁
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

    // ----------------- 換頁 -----------------
    void TurnPage(int dir)
    {
        currentInfo += dir;
        currentInfo = Mathf.Clamp(currentInfo, 0, Infos.Count - 1);

        SetupPageContent();
        UpdateButtonVisibility();
    }

    // ----------------- 上一頁 / 下一頁 / 關閉 按鈕顯示邏輯 -----------------
    void UpdateButtonVisibility()
    {
        PreviousButton.gameObject.SetActive(currentInfo != 0);
        NextButton.gameObject.SetActive(currentInfo != Infos.Count - 1);
        CloseButton.gameObject.SetActive(currentInfo == Infos.Count - 1);
    }
}

[System.Serializable]
public class SLD_Info
{
    public string Title;
    public string Content;
}
