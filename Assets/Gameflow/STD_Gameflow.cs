using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STD_Gameflow : MonoBehaviour
{
    [Header("所有顧客物件（依序）")]
    public List<GameObject> customerList;  // 拖入顧客物件（順序很重要）

    [Header("結束畫面 UI")]
    public STD_GameOverUI gameOverUI;      // 拖入 Game Over 面板腳本

    private int currentCustomerIndex = 0;
    private bool waitingForDelivery = false;

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

    // 只啟用目前的顧客，其他全部隱藏
    void ActivateCustomer(int index)
    {
        for (int i = 0; i < customerList.Count; i++)
        {
            customerList[i].SetActive(i == index);
        }

        Debug.Log($"🧍 顧客 {index + 1} 出現！");
        waitingForDelivery = false;
    }

    /// <summary>
    /// 提供給 QA Manager 呼叫：完成點餐後執行（顧客回來交餐）
    /// </summary>
    public void NextCustomer()
    {
        if (waitingForDelivery || currentCustomerIndex >= customerList.Count)
        {
            Debug.LogWarning("⚠ 無法回到顧客交餐階段，可能已完成或索引錯誤");
            return;
        }

        waitingForDelivery = true;

        Debug.Log($"✅ 顧客 {currentCustomerIndex + 1} 完成點餐，準備交餐");

        // 呼叫目前顧客的交餐流程
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

    /// <summary>
    /// 顧客交餐完畢後呼叫，切換到下一位顧客或結束
    /// </summary>
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
            Debug.Log("🎉 所有顧客互動完畢！");
            ShowGameOverManually();
        }
    }

    /// <summary>
    /// 顯示結束畫面（由最後一位顧客呼叫）
    /// </summary>
    public void ShowGameOverManually()
    {
        if (gameOverUI != null)
        {
            Debug.Log("🎬 顯示 Game Over 畫面");
            gameOverUI.ShowGameOver();
        }
    }
}
