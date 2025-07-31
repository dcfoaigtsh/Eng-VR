using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class FlashcardManager : MonoBehaviour
{
    [System.Serializable]
    public class WordCard
    {
        public Sprite image;
        public string englishWord;
        public string chineseWord;
    }

    public List<WordCard> wordCards;

    [Header("UI 元件")]
    public Image imageDisplay;
    public TextMeshProUGUI wordEnglish;
    public TextMeshProUGUI wordChinese;
    public Button nextButton;
    public Button previousButton;           // ✅ 加入上一張按鈕
    public Button startGameButton;

    private int currentIndex = 0;

    void Start()
    {
        currentIndex = 0;
        ShowCard(currentIndex);

        previousButton.gameObject.SetActive(false);  // 開始時隱藏
        startGameButton.gameObject.SetActive(false); // 最後一張才顯示

        nextButton.onClick.AddListener(OnNextClicked);
        previousButton.onClick.AddListener(OnPreviousClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    void ShowCard(int index)
    {
        if (index < 0 || index >= wordCards.Count) return;

        var card = wordCards[index];
        imageDisplay.sprite = card.image;
        wordEnglish.text = card.englishWord;
        wordChinese.text = card.chineseWord;

        // 控制按鈕狀態
        previousButton.gameObject.SetActive(index > 0);
        nextButton.gameObject.SetActive(index < wordCards.Count - 1);
        startGameButton.gameObject.SetActive(index == wordCards.Count - 1);
    }

    void OnNextClicked()
    {
        if (currentIndex < wordCards.Count - 1)
        {
            currentIndex++;
            ShowCard(currentIndex);
        }
    }

    void OnPreviousClicked()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowCard(currentIndex);
        }
    }

    void OnStartGameClicked()
    {
        SceneManager.LoadScene("ASDmode"); // ✅ 請確認這是你遊戲主場景的正確名稱
    }
}
