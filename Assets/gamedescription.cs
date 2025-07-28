using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameDescription : MonoBehaviour
{
    public GameObject InfomationBoard;
    public TextMeshProUGUI InfoContent;
    public Button CloseButton, NextButton, PreviousButton;
    public GameObject VisualPage; // 新增的圖像說明頁

    public List<Info> Infos = new List<Info>();  
    public int currentInfo;

    void Awake()
    {
        Infos.Add(new Info()
        {
            Content = "Welcome to 'Order Assistant'!\n\nYou are a new helper. Your job is to take orders from customers. Read carefully and choose the right food!"
        });

        Infos.Add(new Info()
        {
            Content = "1. When a customer comes, click to talk.\n2. Follow the steps and find the right clerk.\n3. Each customer wants a main dish, side dish, snack, and drink."
        });

        Infos.Add(new Info()
        {
            Content = "" // 圖像頁，不需文字
        });

        Infos.Add(new Info()
        {
            Content = "Help every customer finish their order!\n\nWhen all orders are done, you’ll see your results. Good luck!"
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
        if (currentInfo == 2 && VisualPage != null)
        {
            InfoContent.text = "";
            VisualPage.SetActive(true);
        }
        else
        {
            InfoContent.text = Infos[currentInfo].Content;
            if (VisualPage != null)
                VisualPage.SetActive(false);
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
public class Info
{
    public string Title;
    public string Content;
}
