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
    public IDProgressBar progressBar;  // 進度條控制元件

    void Start()
    {
        if (customerList != null && customerList.Count > 0)
        {
            ActivateCustomer(0);
            progressBar?.SetProgress(0);  // 🎯 進入遊戲階段（初始）
        }
        else
        {
            Debug.LogWarning("⚠ 顧客清單為空！");
        }
    }

    // 顧客出現
    void ActivateCustomer(int index)
    {
        for (int i = 0; i < customerList.Count; i++)
        {
            customerList[i].SetActive(i == index);
        }

        Debug.Log("顧客 1 出現！");
    }

    // 👉 QAManager 在玩家與顧客對話完呼叫
    public void OnDialogueWithCustomerFinished()
    {
        Debug.Log("🗨 與顧客對話結束，進入點餐階段");
        progressBar?.SetProgress(1); // 🎯 第二階段：顧客對話完成
    }

    // 👉 QAManager 在玩家點餐完成後呼叫
    public void OnOrderFinished()
    {
        if (hasCompleted) return;

        hasCompleted = true;

        Debug.Log("✅ 點餐完成，準備進入交餐流程");

        var customer = customerList[0].GetComponent<ID_SingleCustomer>();
        if (customer != null)
            customer.BeginFinalDialogue();

        progressBar?.SetProgress(2); // 🎯 第三階段：完成點餐
    }

    public void ShowGameOverManually()
    {
        if (gameOverUI != null)
        {
            Debug.Log("🎉 顯示結束畫面！");
            gameOverUI.ShowGameOver();
            progressBar?.SetProgress(3); // 🎯 最終階段：顯示 GameOver
        }
    }
}
