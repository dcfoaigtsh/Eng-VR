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
        public string englishSentence1;
        public string englishSentence2;
        public string chineseSentence1;
        public string chineseSentence2;
        public bool isSentenceMode;
    }

    public List<WordCard> wordCards;

    [Header("UI 元件")]
    public Image imageDisplay;
    public TextMeshProUGUI wordEnglish;
    public TextMeshProUGUI wordChinese;
    public TextMeshProUGUI sentenceEng1;
    public TextMeshProUGUI sentenceEng2;
    public TextMeshProUGUI sentenceChi1;
    public TextMeshProUGUI sentenceChi2;

    public Button flipButton;
    public Button nextButton;
    public Button previousButton;
    public Button startGameButton;
    public Button againButton;
    public Button finishReviewButton;

    [Header("Sound Icons")]
    public GameObject soundBtnEng;
    public GameObject soundBtnChi;
    public GameObject soundBtnEng1;
    public GameObject soundBtnEng2;
    public GameObject soundBtnChi1;
    public GameObject soundBtnChi2;

    private int currentIndex = 0;
    private bool isFlipped = false;
    private bool isReviewMode = false;

    void Start()
    {
        isReviewMode = PlayerPrefs.GetInt("IsReviewMode", 0) == 1;

        currentIndex = 0;
        ShowCardFront(currentIndex);

        flipButton.onClick.AddListener(OnFlipClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        previousButton.onClick.AddListener(OnPreviousClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        againButton.onClick.AddListener(OnAgainClicked);
        finishReviewButton.onClick.AddListener(OnFinishReviewClicked);

        finishReviewButton.gameObject.SetActive(false);
        UpdateButtonState();
    }

    void ShowCardFront(int index)
    {
        var card = wordCards[index];
        imageDisplay.sprite = card.image;
        isFlipped = false;

        if (card.isSentenceMode)
        {
            wordEnglish.gameObject.SetActive(false);
            wordChinese.gameObject.SetActive(false);

            sentenceEng1.gameObject.SetActive(true);
            sentenceEng2.gameObject.SetActive(true);
            sentenceChi1.gameObject.SetActive(true);
            sentenceChi2.gameObject.SetActive(true);

            sentenceEng1.text = card.englishSentence1;
            sentenceEng2.text = card.englishSentence2;
            sentenceChi1.text = "";
            sentenceChi2.text = "";

            soundBtnEng1.SetActive(!string.IsNullOrWhiteSpace(card.englishSentence1));
            soundBtnEng2.SetActive(!string.IsNullOrWhiteSpace(card.englishSentence2));
            soundBtnChi1.SetActive(false);
            soundBtnChi2.SetActive(false);

            soundBtnEng.SetActive(false);
            soundBtnChi.SetActive(false);
        }
        else
        {
            wordEnglish.gameObject.SetActive(true);
            wordChinese.gameObject.SetActive(true);

            sentenceEng1.gameObject.SetActive(false);
            sentenceEng2.gameObject.SetActive(false);
            sentenceChi1.gameObject.SetActive(false);
            sentenceChi2.gameObject.SetActive(false);

            wordEnglish.text = "";
            wordChinese.text = "";

            soundBtnEng.SetActive(false);
            soundBtnChi.SetActive(false);
            soundBtnEng1.SetActive(false);
            soundBtnEng2.SetActive(false);
            soundBtnChi1.SetActive(false);
            soundBtnChi2.SetActive(false);
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

            soundBtnChi1.SetActive(!string.IsNullOrWhiteSpace(card.chineseSentence1));
            soundBtnChi2.SetActive(!string.IsNullOrWhiteSpace(card.chineseSentence2));

            soundBtnEng1.SetActive(false);
            soundBtnEng2.SetActive(false);
            soundBtnEng.SetActive(false);
            soundBtnChi.SetActive(false);
        }
        else
        {
            wordEnglish.text = card.englishWord;
            wordChinese.text = card.chineseWord;

            soundBtnEng.SetActive(!string.IsNullOrWhiteSpace(card.englishWord));
            soundBtnChi.SetActive(!string.IsNullOrWhiteSpace(card.chineseWord));

            soundBtnEng1.SetActive(false);
            soundBtnEng2.SetActive(false);
            soundBtnChi1.SetActive(false);
            soundBtnChi2.SetActive(false);
        }

        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        flipButton.gameObject.SetActive(!isFlipped);
        previousButton.gameObject.SetActive(isFlipped && currentIndex > 0);
        nextButton.gameObject.SetActive(isFlipped && currentIndex < wordCards.Count - 1);

        bool isLast = isFlipped && currentIndex == wordCards.Count - 1;

        if (isReviewMode)
        {
            finishReviewButton.gameObject.SetActive(isLast);
            startGameButton.gameObject.SetActive(false);
            againButton.gameObject.SetActive(false);
        }
        else
        {
            startGameButton.gameObject.SetActive(isLast);
            againButton.gameObject.SetActive(isLast);
            finishReviewButton.gameObject.SetActive(false);
        }
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
        switch (ModeManager.Instance.currentMode)
        {
            case LearningMode.Standard:
                SceneManager.LoadScene("Standard");
                break;
            case LearningMode.ASD:
                SceneManager.LoadScene("ASDmode");
                break;
            case LearningMode.ID:
                SceneManager.LoadScene("IDmode");
                break;
            case LearningMode.SLD:
                SceneManager.LoadScene("SLDmode");
                break;
            default:
                Debug.LogWarning("⚠ 無法識別的學習模式");
                break;
        }
    }

    void OnAgainClicked()
    {
        currentIndex = 0;
        ShowCardFront(currentIndex);
    }

    void OnFinishReviewClicked()
    {
        PlayerPrefs.SetInt("IsReviewMode", 0);
        SceneManager.LoadScene("MainMenu");
    }
}
