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

        // === 新增：對應音檔 ===
        [Header("Audio Clips")]
        public AudioClip englishWordAudio;
        public AudioClip chineseWordAudio;
        public AudioClip englishSentence1Audio;
        public AudioClip englishSentence2Audio;
        public AudioClip chineseSentence1Audio;
        public AudioClip chineseSentence2Audio;
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

    [Header("Sound Icons (顯示/隱藏用)")]
    public GameObject soundBtnEng;
    public GameObject soundBtnChi;
    public GameObject soundBtnEng1;
    public GameObject soundBtnEng2;
    public GameObject soundBtnChi1;
    public GameObject soundBtnChi2;

    [Header("Sound Buttons (點擊事件用，依順序填)")]
    // 0: 英文字, 1: 中文字, 2: 英文句1, 3: 英文句2, 4: 中文句1, 5: 中文句2
    public List<Button> soundButtons = new List<Button>();

    [Header("Audio")]
    public AudioSource audioSource;

    private int currentIndex = 0;
    private bool isFlipped = false;
    private bool isReviewMode = false;

    void Start()
    {
        isReviewMode = PlayerPrefs.GetInt("IsReviewMode", 0) == 1;

        // 綁定一般按鈕
        flipButton.onClick.AddListener(OnFlipClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        previousButton.onClick.AddListener(OnPreviousClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        againButton.onClick.AddListener(OnAgainClicked);
        finishReviewButton.onClick.AddListener(OnFinishReviewClicked);

        // 綁定六個喇叭按鈕
        WireSoundButtons();

        finishReviewButton.gameObject.SetActive(false);

        currentIndex = 0;
        ShowCardFront(currentIndex);
        UpdateButtonState();
    }

    // 將 soundButtons 依序綁定事件；若沒手動填，嘗試由 GameObject 自動抓 Button
    void WireSoundButtons()
    {
        // 嘗試用圖示 GameObject 自動補齊按鈕引用（若 Inspector 尚未填好）
        if (soundButtons.Count == 0)
        {
            TryAddButton(soundBtnEng);
            TryAddButton(soundBtnChi);
            TryAddButton(soundBtnEng1);
            TryAddButton(soundBtnEng2);
            TryAddButton(soundBtnChi1);
            TryAddButton(soundBtnChi2);
        }

        for (int i = 0; i < soundButtons.Count; i++)
        {
            int idx = i; // 避免閉包
            if (soundButtons[i] != null)
            {
                soundButtons[i].onClick.RemoveAllListeners();
                soundButtons[i].onClick.AddListener(() => OnSoundButtonClicked(idx));
            }
        }

        // 基本防呆：確保音源存在
        if (audioSource == null)
        {
            // 自動建立一個 AudioSource（可選）
            var go = new GameObject("FlashcardAudioSource");
            go.transform.SetParent(this.transform);
            audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void TryAddButton(GameObject go)
    {
        if (go == null) return;
        var btn = go.GetComponent<Button>();
        if (btn != null) soundButtons.Add(btn);
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
        SceneManager.LoadScene("GameOver");
    }

    // === 重要：統一處理六個喇叭的點擊 ===
    void OnSoundButtonClicked(int index)
    {
        if (audioSource == null) return;

        var card = wordCards[currentIndex];
        AudioClip clipToPlay = null;

        switch (index)
        {
            case 0: clipToPlay = card.englishWordAudio; break;       // 英文字
            case 1: clipToPlay = card.chineseWordAudio; break;       // 中文字
            case 2: clipToPlay = card.englishSentence1Audio; break;  // 英文句1
            case 3: clipToPlay = card.englishSentence2Audio; break;  // 英文句2
            case 4: clipToPlay = card.chineseSentence1Audio; break;  // 中文句1
            case 5: clipToPlay = card.chineseSentence2Audio; break;  // 中文句2
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
        else
        {
            // 沒有對應音檔就不播放（可視需求加上提示）
            // Debug.Log("No audio clip assigned for index: " + index);
        }
    }
}
