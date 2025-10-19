using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class SpeechPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;      // 整個語音練習面板
    public TMP_Text targetText;   // 上行：要念的句子
    public TMP_Text userText;     // 下行：辨識結果
    public Button speakButton;    // 「Speak」按鈕
    public Button doneButton;     // 「Done」按鈕

    [Header("Recognizer")]
    public DictationWrapper dictation;

    private bool busy = false;
    private string recognizedResult = "";

    // ✅ 外部可設定：Done 按下後的回呼（例如檢查是否正確）
    private Action<string> onSpeechFinished;

    void Awake()
    {
        panel.SetActive(false);
        speakButton.onClick.AddListener(OnSpeak);
        doneButton.onClick.AddListener(OnDone);
    }

    // 🔹 顯示面板 + 傳入要念的句子 & 結束後回呼
    public void ShowSentence(string sentence, Action<string> callback)
    {
        targetText.text = sentence;
        userText.text = "";
        recognizedResult = "";
        onSpeechFinished = callback;
        panel.SetActive(true);
    }

    // 🔹 點擊「Speak」
    void OnSpeak()
    {
        if (busy) return;
        busy = true;
        userText.text = "Listening...";
        dictation.StartListening(OnRecognized);
    }

    // 🔹 收到辨識結果
    void OnRecognized(string recognizedText)
    {
        busy = false;
        recognizedResult = recognizedText;
        userText.text = recognizedText;
    }

    // 🔹 點擊「Done」→ 關閉並通知外部
    void OnDone()
    {
        dictation.StopListening();
        panel.SetActive(false);
        busy = false;

        // ✅ 通知外部（例如進行正確性判斷）
        if (onSpeechFinished != null)
            onSpeechFinished.Invoke(recognizedResult);
    }

    public void ClosePanel()
    {
        dictation.StopListening();
        panel.SetActive(false);
        busy = false;
    }
}
