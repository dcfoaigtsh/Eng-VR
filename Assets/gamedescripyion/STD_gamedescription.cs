using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class STD_GameDescription : MonoBehaviour
{
    public GameObject InfomationBoard;
    public TextMeshProUGUI InfoContent;
    public Button CloseButton, NextButton, PreviousButton;
    public GameObject VisualPage; // 新增的圖像說明頁
    public GameObject VisualPage2; // 新增的圖像說明頁

    public List<STD_Info> Infos = new List<STD_Info>();  
    public int currentInfo;

    void Awake()
    {
        Infos.Add(new STD_Info()
        {
            Content = "Welcome to 'Order Assistant'!\n\nYou are a helper. \nYou will help your friend order food."
        });

        Infos.Add(new STD_Info()
        {
            Content = "How to play: \n1. Click the ! talk to your friend.\n2. Talk to the staff \n3. Return to your friend with food."
        });

        Infos.Add(new STD_Info()
        {
            Content = "" // 圖像頁，不需文字
        });

        Infos.Add(new STD_Info()
        {
            Content = "" // 圖像頁，不需文字
        });

        Infos.Add(new STD_Info()
        {
            Content = "Once you finish taking orders, your task is completed.\nThen, review the words again. \n Good luck!"
        });
    }

    void Start()
    {
        currentInfo = 0;
        InfomationBoard.SetActive(true);
        SetupPageContent();

        NextButton.onClick.AddListener(() => TurnPage(1));
        PreviousButton.onClick.AddListener(() => TurnPage(-1));
        CloseButton.onClick.AddListener(() => InfomationBoard.SetActive(false));

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
        NextButton.gameObject.SetActive(currentInfo != Infos.Count - 1);
        CloseButton.gameObject.SetActive(currentInfo == Infos.Count - 1);
    }
}

[System.Serializable]
public class STD_Info
{
    public string Title;
    public string Content;
}
