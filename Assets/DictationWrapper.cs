using UnityEngine;
using UnityEngine.Windows.Speech;
using System;

public class DictationWrapper : MonoBehaviour
{
#if UNITY_STANDALONE_WIN || UNITY_WSA
    private DictationRecognizer recognizer;
#endif
    public Action<string> OnPartial; // 即時文字
    public Action<string> OnFinal;   // 最終文字
    public Action<string> OnStatus;  // 顯示狀態/錯誤

    private bool running = false;

    // 🔹 開始語音辨識（由 SpeechPopup 呼叫）
    public void StartListening(Action<string> onFinalResult)
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA
        if (running) return;

        try
        {
            recognizer = new DictationRecognizer(ConfidenceLevel.High);

            // 🔸 延長等待時間，讓使用者可以慢慢說
            recognizer.InitialSilenceTimeoutSeconds = 5f;  // 開場最多等 5 秒才放棄
            recognizer.AutoSilenceTimeoutSeconds = 3f;    // 停頓 3 秒才結束

            // 🔸 綁定事件
            OnFinal = onFinalResult;

            recognizer.DictationHypothesis += (txt) =>
            {
                OnPartial?.Invoke(txt); // 即時顯示
            };

            recognizer.DictationResult += (txt, conf) =>
            {
                OnPartial?.Invoke(txt);
                OnFinal?.Invoke(txt);   // 最終結果
            };

            recognizer.DictationComplete += (cause) =>
            {
                OnStatus?.Invoke($"DictationComplete: {cause}");
                running = false;
            };

            recognizer.DictationError += (err, hr) =>
            {
                OnStatus?.Invoke($"DictationError: {err} (0x{hr:X8})");
                running = false;
            };

            recognizer.Start();
            running = true;
            OnStatus?.Invoke("Listening...");
        }
        catch (System.Exception ex)
        {
            OnStatus?.Invoke("此機器未啟用 Windows 語音辨識：" + ex.Message);
        }
#else
        OnStatus?.Invoke("非 Windows 平台，不支援 DictationRecognizer。");
#endif
    }

    // 🔹 停止辨識（按 Done 時呼叫）
    public void StopListening()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA
        if (recognizer != null)
        {
            if (recognizer.Status == SpeechSystemStatus.Running)
                recognizer.Stop();
            recognizer.Dispose();
            recognizer = null;
        }
#endif
        running = false;
    }

    void OnDestroy() => StopListening();
}
