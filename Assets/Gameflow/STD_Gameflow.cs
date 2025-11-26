using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STD_Gameflow : Gameflow
{
    [Header("所有顧客物件（依序）")]
    public List<GameObject> customerList;

    [Header("結束畫面 UI")]
    public STD_GameOverUI gameOverUI;

    private int currentCustomerIndex = 0;
    private bool waitingForDelivery = false;

    // 【新增統計變數與方法】！
    public int totalQuestions = 0;       // 總題數（每題只算第一次作答）
    public int correctFirstTry = 0;      // 第一次答對的題數

    public void RegisterFirstAttempt(bool correct)
    {
        totalQuestions++;
        if (correct) correctFirstTry++;
        Debug.Log($"[STD統計] 回報作答：{(correct ? "✔" : "✘")}（正確/總題數 = {correctFirstTry}/{totalQuestions}）");

    }

    public float GetAccuracyPercent()
    {
        if (totalQuestions == 0) return 0f;
        return (float)correctFirstTry / totalQuestions * 100f;
    }
    // 【到這裡結束】

    void Start()
    {
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
            if (i == index) NotifyMovingToCustomer();
            customerList[i].SetActive(i == index);
        }

        Debug.Log($"🧍 顧客 {index + 1} 出現！");
        waitingForDelivery = false;
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

        var customer = customerList[currentCustomerIndex].GetComponent<STD_SingleCustomer>();
        if (customer != null)
        {
            customer.BeginFinalDialogue();
        }
        else
        {
            Debug.LogWarning("⚠ 無法取得 STD_SingleCustomer 元件");
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
        NotifyCompleted();
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
}