using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SLD_Gameflow : MonoBehaviour
{
    [Header("所有顧客物件（依序）")]
    public List<GameObject> customerList;

    [Header("結束畫面 UI")]
    public SLD_GameOverUI gameOverUI;

    private int currentCustomerIndex = 0;
    private bool waitingForDelivery = false;

    // ✅ 答題統計
    private int totalQuestions = 0;
    private int correctAnswers = 0;

    // ✅ 計時器
    private float startTime;
    private float endTime;

    void Start()
    {
        startTime = Time.time; // ✅ 開始計時

        if (customerList != null && customerList.Count > 0)
        {
            ActivateCustomer(0);
        }
        else
        {
            Debug.LogWarning("⚠ 顧客清單為空！");
        }
    }

    void ActivateCustomer(int index)
    {
        for (int i = 0; i < customerList.Count; i++)
        {
            customerList[i].SetActive(i == index);
        }

        Debug.Log($"🧍 顧客 {index + 1} 出現！");
        waitingForDelivery = false;
    }

    // ✅ 給 QA Manager 回報每題結果（僅第一次作答）
    public void RegisterFirstAttempt(bool isCorrect)
    {
        totalQuestions++;
        if (isCorrect) correctAnswers++;

        Debug.Log($"📊 記錄作答結果：目前 {correctAnswers}/{totalQuestions}");
    }

    public void NextCustomer()
    {
        if (waitingForDelivery || currentCustomerIndex >= customerList.Count)
        {
            Debug.LogWarning("⚠ 無法回到顧客交餐階段，可能已完成或索引錯誤");
            return;
        }

        waitingForDelivery = true;
        Debug.Log($"✅ 顧客 {currentCustomerIndex + 1} 完成點餐，準備交餐");

        customerList[currentCustomerIndex].SetActive(true);

        var customer = customerList[currentCustomerIndex].GetComponent<SLD_SingleCustomer>();
        if (customer != null)
        {
            customer.BeginFinalDialogue();
        }
        else
        {
            Debug.LogWarning("⚠ 無法取得 SLD_SingleCustomer 元件");
        }
    }

    public void ProceedToNextCustomer()
    {
        customerList[currentCustomerIndex].SetActive(false);
        currentCustomerIndex++;

        if (currentCustomerIndex < customerList.Count)
        {
            ActivateCustomer(currentCustomerIndex);
        }
        else
        {
            Debug.Log("所有顧客互動完畢！");
            ShowGameOverManually();
        }
    }

    public void ShowGameOverManually()
    {
        // ✅ 統計資料
        float accuracyPercent = GetAccuracyPercent();
        float timeSpent = Time.timeSinceLevelLoad;

        PlayerPrefs.SetFloat("Accuracy", accuracyPercent);
        PlayerPrefs.SetFloat("TimeSpent", timeSpent);
        PlayerPrefs.Save();

        if (gameOverUI != null)
        {
            Debug.Log("顯示 Game Over 畫面");
            gameOverUI.ShowGameOver();
        }
    }

    // ✅ 計算正確率（百分比）
    private float GetAccuracyPercent()
    {
        if (totalQuestions == 0) return 100f;
        return ((float)correctAnswers / totalQuestions) * 100f;
    }
}
