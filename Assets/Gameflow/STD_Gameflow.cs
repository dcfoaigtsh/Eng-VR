using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STD_Gameflow : MonoBehaviour
{
    [Header("List of all customer GameObjects in order")]
    public List<GameObject> customerList;  // 拖入所有顧客物件

    [Header("結束畫面 UI")]
    public STD_GameOverUI gameOverUI;  // 拖入 Game Over 面板腳本

    private int currentCustomerIndex = 0;

    void Start()
    {
        ActivateCustomer(currentCustomerIndex);
    }

    // 啟用指定顧客，其餘關閉
    void ActivateCustomer(int index)
    {
        for (int i = 0; i < customerList.Count; i++)
        {
            customerList[i].SetActive(i == index);
        }

        Debug.Log($"顧客 {index + 1} 出現！");
    }

    // 提供給 QAManager 呼叫，切換下一位顧客
    public void NextCustomer()
    {
        if (currentCustomerIndex >= customerList.Count)
        {
            Debug.Log("所有顧客都完成點餐！（已超出索引）");
            return;
        }

        customerList[currentCustomerIndex].SetActive(false);
        currentCustomerIndex++;

        if (currentCustomerIndex < customerList.Count)
        {
            ActivateCustomer(currentCustomerIndex);
        }
        else
        {
            Debug.Log("✅ 所有顧客都完成點餐！");
    
            if (gameOverUI != null)
            {
                Debug.Log(" 顯示結束面板！");
                gameOverUI.ShowGameOver(); // <== 這一行！！
            }
        }
    }
}
