using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class SpeechPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;      // 整個對話框容器
    public TMP_Text targetText;   // 上行：要念的句子
    public TMP_Text userText;     // 下行：玩家語音結果
    public Button speakButton;    // 右側：Speak
    public Button doneButton;     // 右側：Done（新增）

    [Header("Recognizer")]
    public DictationWrapper dictation;

    private Action<string> onFinishedOnce; // 語音完成後回呼
    private bool busy = false;             // 是否正在錄音中
    private bool finalized = false;        // 防止重複完成
    private Coroutine autoHideCoroutine;   // 自動關閉計時

    void Awake()
    {
        panel.SetActive(false);

        speakButton.onClick.RemoveAllListeners();
        speakButton.onClick.AddListener(OnSpeakClicked);

        if (doneButton != null)
        {
            doneButton.onClick.RemoveAllListeners();
            doneButton.onClick.AddListener(OnDoneClicked);
        }

        // 綁定辨識事件
        dictation.OnPartial = (txt) =>
        {
            if (userText) userText.text = txt;
        };
        dictation.OnFinal = (txt) =>
        {
            if (userText) userText.text = txt;
            Finish(txt);
        };
        dictation.OnStatus = (msg) =>
        {
            Debug.Log(msg);
        };
    }

    public void Show(string target, Action<string> onFinished)
    {
        targetText.text = target;
        userText.text = "";
        onFinishedOnce = onFinished;

        panel.SetActive(true);
        finalized = false;
        busy = false;

        // UI 狀態
        SetButtonsInteractable(speak: true, done: false);
    }

    public void Hide()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        panel.SetActive(false);
        dictation.StopRecognition();
        busy = false;
        finalized = false;
    }

    void OnSpeakClicked()
    {
        if (busy) return;

        busy = true;
        userText.text = "…listening…";

        SetButtonsInteractable(speak: false, done: true);
        dictation.StartOnce(); // 開始一次；完成或錯誤時自動停止
    }

    void OnDoneClicked()
    {
        if (finalized) return;
        finalized = true;

        // 取目前畫面上的文字當作最後結果（可能是 DictationResult 或假設文字）
        string finalNow = userText ? userText.text : string.Empty;

        // 停止辨識，但「不要」關掉面板；讓玩家看到自己的文字
        dictation.StopRecognition();

        // 直接進到 Finish（會呼叫 onFinishedOnce 回到 QAmanager）
        Finish(finalNow);
    }

    void Finish(string finalText)
    {
        if (finalized == false)
            finalized = true; // 若是 Dictation 自動完成

        busy = false;

        // 保持面板顯示，讓玩家看到自己最後的文字（由外部決定何時 Hide）
        SetButtonsInteractable(speak: true, done: false);

        // 回呼給外部（例如 STD_SingleCustomer）
        onFinishedOnce?.Invoke(finalText);
        onFinishedOnce = null;
    }

    // 🔹 新增：由外部呼叫，在錄音完成後延遲幾秒自動關閉
    public void AutoHideAfterSeconds(float delay)
    {
        if (autoHideCoroutine != null)
            StopCoroutine(autoHideCoroutine);
        autoHideCoroutine = StartCoroutine(AutoHide(delay));
    }

    private IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
    }

    void SetButtonsInteractable(bool speak, bool done)
    {
        if (speakButton) speakButton.interactable = speak;
        if (doneButton) doneButton.interactable = done;
    }
}
