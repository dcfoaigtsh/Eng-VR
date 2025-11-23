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

    [Header("遊戲說明元件")]
    public STD_GameDescription gameDescription; // 遊戲說明元件參考

    [Header("玩家狀態")]
    public PlayerState currentPlayerState = PlayerState.ReadingDescription;

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
            currentPlayerState = newState;
            OnPlayerStateChanged?.Invoke(newState);
            Debug.Log($"🎮 玩家狀態變更: {newState}");
        }
    }

    void Start()
    {
        // 初始狀態為閱讀遊戲說明
        SetPlayerState(PlayerState.ReadingDescription);
        
        if (customerList != null && customerList.Count > 0)
        {
            ActivateCustomer(0);
        }
        else
        {
            Debug.LogWarning("⚠ 顧客清單為空！");
        }

        // 尋找遊戲說明元件
        if (gameDescription == null)
        {
            gameDescription = FindObjectOfType<STD_GameDescription>();
        }
    }

    void Update()
    {
        // 每幀檢查遊戲說明狀態
        CheckGameDescriptionState();
    }

    void CheckGameDescriptionState()
    {
        if (gameDescription != null && gameDescription.InfomationBoard != null)
        {
            bool isDescriptionOpen = gameDescription.InfomationBoard.activeInHierarchy;
            int currentPage = gameDescription.currentInfo;

            if (isDescriptionOpen)
            {
                // 遊戲說明打開
                SetPlayerState(PlayerState.ReadingDescription);
            }
            else
            {
                if (currentPlayerState == PlayerState.ReadingDescription)
                {
                    SetPlayerState(PlayerState.MovingToCustomer);
                }
            }
        }
        else
        {
            // 如果沒有遊戲說明元件，直接設為 MovingToCustomer
            if (currentPlayerState == PlayerState.ReadingDescription)
            {
                SetPlayerState(PlayerState.MovingToCustomer);
            }
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
        
        // 顧客出現時，如果不在閱讀說明狀態，設為 MovingToCustomer
        if (currentPlayerState != PlayerState.ReadingDescription)
        {
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
    public void NotifyCustomerInteractionStarted()
    {
        SetPlayerState(PlayerState.TalkingToCustomer);
        Debug.Log("顧客互動開始，狀態設為 TalkingToCustomer");
    }

    public void NotifyStaffInteractionStarted()
    {
        SetPlayerState(PlayerState.OrderingAtStaff);
        Debug.Log("員工互動開始，狀態設為 OrderingAtStaff");
    }

    public void NotifyMovingToStaff()
    {
        SetPlayerState(PlayerState.MovingToStaff);
        Debug.Log("移動到員工，狀態設為 MovingToStaff");
    }

    public void NotifyMovingToCustomer()
    {
        SetPlayerState(PlayerState.MovingToCustomer);
        Debug.Log("移動到顧客，狀態設為 MovingToCustomer");
    }

    public void NotifyReturningToCustomer()
    {
        SetPlayerState(PlayerState.ReturningToCustomer);
        Debug.Log("返回顧客，狀態設為 ReturningToCustomer");
    }
}