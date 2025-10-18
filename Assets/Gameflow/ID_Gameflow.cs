using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ID_Gameflow : MonoBehaviour
{
    [Header("只使用第1位顧客")]
    public List<GameObject> customerList;

    [Header("結束畫面 UI")]
    public ID_GameOverUI gameOverUI;
    private bool hasCompleted = false;

    [Header("進度條控制")]
    public IDProgressBar progressBar;

    // ✅ 統計欄位
    public int totalQuestions = 0;
    public int correctFirstTry = 0;

    void Start()
    {
        if (customerList != null && customerList.Count > 0)
        {
            ActivateCustomer(0);
            progressBar?.SetProgress(0);
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

        Debug.Log("顧客 1 出現！");
    }

    // ✅ 提供 QA manager 呼叫：回報第一次作答結果
    public void RegisterFirstAttempt(bool correct)
    {
        totalQuestions++;
        if (correct) correctFirstTry++;
        Debug.Log($"[ID統計] 第一次作答：{(correct ? "✔" : "✘")}，目前 {correctFirstTry}/{totalQuestions}");
    }

    // ✅ 提供 GameOver 顯示用
    public float GetAccuracyPercent()
    {
        if (totalQuestions == 0) return 0f;
        return (float)correctFirstTry / totalQuestions * 100f;
    }

    public void OnDialogueWithCustomerFinished()
    {
        Debug.Log("🗨 與顧客對話結束，進入點餐階段");
        progressBar?.SetProgress(1);
    }

    public void OnOrderFinished()
    {
        if (hasCompleted) return;

        hasCompleted = true;

        Debug.Log("✅ 點餐完成，準備進入交餐流程");

        var customer = customerList[0].GetComponent<ID_SingleCustomer>();
        if (customer != null)
            customer.BeginFinalDialogue();

        progressBar?.SetProgress(2);
    }

    public void ShowGameOverManually()
    {
        // ✅ 寫入 PlayerPrefs
        float accuracyPercent = GetAccuracyPercent();
        float timeSpent = Time.timeSinceLevelLoad;

        PlayerPrefs.SetFloat("Accuracy", accuracyPercent);
        PlayerPrefs.SetFloat("TimeSpent", timeSpent);
        PlayerPrefs.Save();

        if (gameOverUI != null)
        {
            Debug.Log("🎉 顯示結束畫面！");
            gameOverUI.ShowGameOver();
            progressBar?.SetProgress(3);
        }
    }
}
