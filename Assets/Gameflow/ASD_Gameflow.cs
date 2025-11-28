using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASD_Gameflow : Gameflow
{
    [Header("只使用第1位顧客")]
    public List<GameObject> customerList;

    [Header("結束畫面 UI")]
    public ASD_GameOverUI gameOverUI;
    private bool hasCompleted = false;

    [Header("進度條控制")]
    public IDProgressBar progressBar;

    // 統計用
    public int totalQuestions = 0;
    public int correctFirstTry = 0;

    void Start()
    {
        if (customerList != null && customerList.Count > 0)
        {
            ActivateCustomer(0);
            progressBar?.SetProgress(0);   // 👈 進度：開始（顧客對話）
        }
        else
        {
            Debug.LogWarning("⚠ 顧客清單為空！");
        }
    }

    // 顧客1出現
    void ActivateCustomer(int index)
    {
        for (int i = 0; i < customerList.Count; i++)
        {
            if (i == index)
                NotifyMovingToCustomer();
            
            customerList[i].SetActive(i == index);
        }

        Debug.Log("顧客 1 出現！");
    }

    // 顧客第一次作答統計
    public void RegisterFirstAttempt(bool correct)
    {
        totalQuestions++;
        if (correct) correctFirstTry++;
    }

    public float GetAccuracyPercent()
    {
        if (totalQuestions == 0) return 0f;
        return (float)correctFirstTry / totalQuestions * 100f;
    }

    // ★★★★★ 新增：顧客對話結束後由 SingleCustomer 呼叫
    public void OnDialogueWithCustomerFinished()
    {
        Debug.Log("🗨 與顧客對話結束，準備去找店員");
        progressBar?.SetProgress(1);   // 👈 進度 1：走向店員
    }

    // QA Manager 呼叫：店員點餐完成
    public void NextCustomer()
    {
        if (hasCompleted) return;

        hasCompleted = true;

        Debug.Log("🏁 店員點餐完成，要回去找顧客交餐");

        // 顧客進入「回來交餐對話」
        var customer = customerList[0].GetComponent<ASD_SingleCustomer>();
        if (customer != null)
            customer.BeginFinalDialogue();

        progressBar?.SetProgress(2);   // 👈 進度 2：回去交餐
    }

    // 全部完成 → 進入結束畫面
    public void ShowGameOverManually()
    {
        NotifyCompleted();

        float accuracyPercent = GetAccuracyPercent();
        float timeSpent = Time.timeSinceLevelLoad;

        PlayerPrefs.SetFloat("Accuracy", accuracyPercent);
        PlayerPrefs.SetFloat("TimeSpent", timeSpent);
        PlayerPrefs.Save();

        if (gameOverUI != null)
        {
            Debug.Log("🎉 顯示結束畫面！");
            gameOverUI.ShowGameOver();

            progressBar?.SetProgress(3);  // 👈 進度 3：任務完成
        }
    }
}
