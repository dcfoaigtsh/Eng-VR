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

    public Button flipButton;
    public Button nextButton;
    public Button previousButton;
    public Button startGameButton;
    public Button againButton;

    private int currentIndex = 0;
    private bool isFlipped = false;

    void Start()
    {
        currentIndex = 0;
        ShowCardFront(currentIndex);

        // 按鈕事件綁定
        flipButton.onClick.AddListener(OnFlipClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        previousButton.onClick.AddListener(OnPreviousClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        againButton.onClick.AddListener(OnAgainClicked);

        UpdateButtonState();
    }

    void ShowCardFront(int index)
    {
        var card = wordCards[index];
        imageDisplay.sprite = card.image;
        wordEnglish.text = "";
        wordChinese.text = "";
        isFlipped = false;

        UpdateButtonState();
    }

    void ShowCardBack()
    {
        var card = wordCards[currentIndex];
        wordEnglish.text = card.englishWord;
        wordChinese.text = card.chineseWord;
        isFlipped = true;

        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        flipButton.gameObject.SetActive(!isFlipped);
        previousButton.gameObject.SetActive(isFlipped && currentIndex > 0);
        nextButton.gameObject.SetActive(isFlipped && currentIndex < wordCards.Count - 1);
        startGameButton.gameObject.SetActive(isFlipped && currentIndex == wordCards.Count - 1);
        againButton.gameObject.SetActive(isFlipped && currentIndex == wordCards.Count - 1);
    }

    void OnFlipClicked()
    {
        ShowCardBack();
    }

    void OnNextClicked()
    {
        if (currentIndex < wordCards.Count - 1)
        {
            currentIndex++;
            ShowCardFront(currentIndex);
        }
    }

    void OnPreviousClicked()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowCardFront(currentIndex);
        }
    }

    void OnStartGameClicked()
    {
        SceneManager.LoadScene("ASDmode");  // 改成你的遊戲主場景名稱
    }

    void OnAgainClicked()
    {
        currentIndex = 0;
        ShowCardFront(currentIndex);
    }
}
