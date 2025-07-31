using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASD_Gameflow : MonoBehaviour
{
    [Header("只使用第1位顧客")]
    public List<GameObject> customerList;  // 拖入顧客物件（只使用 index 0）

    [Header("結束畫面 UI")]
    public ASD_GameOverUI gameOverUI;      // 拖入 Game Over 面板腳本

    private bool hasStarted = false;
    private bool hasCompleted = false;

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

    // 啟用顧客1（其實只有一位）
    void ActivateCustomer(int index)
    {
        for (int i = 0; i < customerList.Count; i++)
        {
            customerList[i].SetActive(i == index);
        }

        Debug.Log("顧客 1 出現！");
        hasStarted = true;
    }

    // 提供給 QA Manager 呼叫：完成點餐後執行
    public void NextCustomer()
    {
        if (hasCompleted) return;

        hasCompleted = true;

        Debug.Log("✅ 顧客1完成店員互動，準備回來找他交餐點");

        // 👉 呼叫顧客1的回傳流程（交餐）
        var customer = customerList[0].GetComponent<ASD_SingleCustomer>();
        if (customer != null)
            customer.BeginFinalDialogue(); // ✅ 新增的 public 方法

        // ✅ 不顯示結束畫面，等到顧客結束對話才叫 ShowGameOver
    }

    public void ShowGameOverManually()
{
    if (gameOverUI != null)
    {
        Debug.Log("🎉 顯示結束畫面！");
        gameOverUI.ShowGameOver();
    }
}
}
