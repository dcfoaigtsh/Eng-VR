using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STD_Gameflow : MonoBehaviour
{
    [Header("所有顧客物件（依序）")]
    public List<GameObject> customerList;

    [Header("結束畫面 UI")]
    public STD_GameOverUI gameOverUI;

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

    void ActivateCustomer(int index)
    {
        for (int i = 0; i < customerList.Count; i++)
        {
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

        customerList[currentCustomerIndex].SetActive(true); // ✅ 確保顧客顯示

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
        if (gameOverUI != null)
        {
            Debug.Log("顯示 Game Over 畫面");
            gameOverUI.ShowGameOver();
        }
    }
}
