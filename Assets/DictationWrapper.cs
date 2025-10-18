using UnityEngine;
using UnityEngine.Windows.Speech;
using System;
using System.Collections;

public class DictationWrapper : MonoBehaviour
{
#if UNITY_STANDALONE_WIN || UNITY_WSA
    private DictationRecognizer recognizer;
#endif
    public Action<string> OnPartial; // 即時文字
    public Action<string> OnFinal;   // 最終文字
    public Action<string> OnStatus;  // 顯示狀態/錯誤

    private bool running = false;
    private Coroutine autoStopCoroutine; // 自動超時用

    public void StartOnce()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA
        if (running) return;
        try
        {
            // 使用較寬鬆信心等級，讓短句也能快速辨識
            recognizer = new DictationRecognizer(ConfidenceLevel.Low);

            // 🔹 縮短開口與停頓等待時間（秒）
            recognizer.InitialSilenceTimeoutSeconds = 2f;  // 開場靜音2秒就停止
            recognizer.AutoSilenceTimeoutSeconds = 1.0f;   // 停頓1秒就結束

            // 🔹 綁定事件
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
                StopRecognition();
            };
            recognizer.DictationError += (err, hr) =>
            {
                OnStatus?.Invoke($"DictationError: {err} (0x{hr:X8})");
                StopRecognition();
            };

            recognizer.Start();
            running = true;
            OnStatus?.Invoke("🎙 Listening...");

            // 🔹 啟動自動超時機制（例如 5 秒後強制停止）
            if (autoStopCoroutine != null) StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = StartCoroutine(AutoStopAfterSeconds(5f));
        }
        catch (System.Exception ex)
        {
            OnStatus?.Invoke("此機器未啟用 Windows 語音辨識：" + ex.Message);
        }
#else
        OnStatus?.Invoke("非 Windows 平台，不支援 DictationRecognizer。");
#endif
    }

    public void StopRecognition()
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
        if (autoStopCoroutine != null)
        {
            StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = null;
        }
    }

    void OnDestroy() => StopRecognition();

    // 🔹 若超過 limit 秒仍未結束，強制停止
    private IEnumerator AutoStopAfterSeconds(float limit)
    {
        float timer = 0f;
        while (running && timer < limit)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (running)
        {
            OnStatus?.Invoke($"⏱ 超時 {limit} 秒，自動結束辨識。");
            StopRecognition();
        }
    }
}
