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

        // 單字卡內容
        public string englishWord;
        public string chineseWord;

        // 句子卡內容（可選）
        public string englishSentence1;
        public string englishSentence2;
        public string chineseSentence1;
        public string chineseSentence2;

        public bool isSentenceMode;
    }

    public List<WordCard> wordCards;

    [Header("UI 元件")]
    public Image imageDisplay;

    // 單字模式 UI
    public TextMeshProUGUI wordEnglish;
    public TextMeshProUGUI wordChinese;

    // 句子模式 UI
    public TextMeshProUGUI sentenceEng1;
    public TextMeshProUGUI sentenceEng2;
    public TextMeshProUGUI sentenceChi1;
    public TextMeshProUGUI sentenceChi2;

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
        isFlipped = false;

        if (card.isSentenceMode)
        {
            // 顯示句子英文，隱藏中文
            wordEnglish.gameObject.SetActive(false);
            wordChinese.gameObject.SetActive(false);

            sentenceEng1.gameObject.SetActive(true);
            sentenceEng2.gameObject.SetActive(true);
            sentenceChi1.gameObject.SetActive(true);  // 這邊要顯示出來，但內容清空
            sentenceChi2.gameObject.SetActive(true);

            sentenceEng1.text = card.englishSentence1;
            sentenceEng2.text = card.englishSentence2;
            sentenceChi1.text = ""; // 清空中文
            sentenceChi2.text = "";
        }
        else
        {
            // 顯示單字模式
            wordEnglish.gameObject.SetActive(true);
            wordChinese.gameObject.SetActive(true);

            sentenceEng1.gameObject.SetActive(false);
            sentenceEng2.gameObject.SetActive(false);
            sentenceChi1.gameObject.SetActive(false);
            sentenceChi2.gameObject.SetActive(false);

            wordEnglish.text = "";
            wordChinese.text = "";
        }

        UpdateButtonState();
    }


    void ShowCardBack()
    {
        var card = wordCards[currentIndex];
        isFlipped = true;

        if (card.isSentenceMode)
        {
            sentenceEng1.gameObject.SetActive(false);
            sentenceEng2.gameObject.SetActive(false);
            sentenceChi1.gameObject.SetActive(true);
            sentenceChi2.gameObject.SetActive(true);

            sentenceChi1.text = card.chineseSentence1;
            sentenceChi2.text = card.chineseSentence2;
        }
        else
        {
            wordEnglish.text = card.englishWord;
            wordChinese.text = card.chineseWord;
        }

        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        flipButton.gameObject.SetActive(!isFlipped);
        previousButton.gameObject.SetActive(isFlipped && currentIndex > 0);
        nextButton.gameObject.SetActive(isFlipped && currentIndex < wordCards.Count - 1);

        bool isLast = isFlipped && currentIndex == wordCards.Count - 1;
        startGameButton.gameObject.SetActive(isLast);
        againButton.gameObject.SetActive(isLast);
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
        SceneManager.LoadScene("ASDmode");
    }

    void OnAgainClicked()
    {
        currentIndex = 0;
        ShowCardFront(currentIndex);
    }
}
