using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    ReadingDescription,     // 正在閱讀遊戲說明
    MovingToCustomer,       // 正在走向顧客
    TalkingToCustomer,      // 正在與顧客對話
    MovingToStaff,          // 正在走向員工
    OrderingAtStaff,        // 正在與員工點餐
    ReturningToCustomer,    // 正在返回顧客
    Completed               // 已完成點餐任務
}

public class STD_Gameflow : MonoBehaviour
{
    [Header("所有顧客物件（依序）")]
    public List<GameObject> customerList;

    [Header("結束畫面 UI")]
    public STD_GameOverUI gameOverUI;

    [Header("玩家狀態")]
    public PlayerState currentPlayerState = PlayerState.ReadingDescription;
    public PlayerState previousPlayerState = PlayerState.ReadingDescription;

    private int currentCustomerIndex = 0;
    private bool waitingForDelivery = false;

    // 【新增統計變數與方法】！
    public int totalQuestions = 0;       // 總題數（每題只算第一次作答）
    public int correctFirstTry = 0;      // 第一次答對的題數

    // 事件系統，用於通知狀態變化
    public System.Action<PlayerState> OnPlayerStateChanged;

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

    // 設置玩家狀態
    public void SetPlayerState(PlayerState newState)
    {
        if (currentPlayerState != newState)
        {
            previousPlayerState = currentPlayerState;
            currentPlayerState = newState;
            OnPlayerStateChanged?.Invoke(newState);
            Debug.Log($"玩家狀態變更: {newState}");
        }
    }

    void Start()
    {
        SetPlayerState(PlayerState.ReadingDescription);
        
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
        
        if(currentPlayerState != PlayerState.TalkingToCustomer) {
            SetPlayerState(PlayerState.MovingToCustomer);
        }
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
            SetPlayerState(PlayerState.Completed);
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

    // 公共方法供其他腳本呼叫來更新狀態
    public void NotifyReadingDescription()
    {
        SetPlayerState(PlayerState.ReadingDescription);
    }

    public void NotifyCustomerInteractionStarted()
    {
        SetPlayerState(PlayerState.TalkingToCustomer);
    }

    public void NotifyStaffInteractionStarted()
    {
        SetPlayerState(PlayerState.OrderingAtStaff);
    }

    public void NotifyMovingToStaff()
    {
        SetPlayerState(PlayerState.MovingToStaff);
    }

    public void NotifyMovingToCustomer()
    {
        SetPlayerState(PlayerState.MovingToCustomer);
    }

    public void NotifyReturningToCustomer()
    {
        SetPlayerState(PlayerState.ReturningToCustomer);
    }

    public void NotifyRestorePreviousState()
    {
        SetPlayerState(previousPlayerState);
    }
}