using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    ReadingDescription,     // 正在閱讀遊戲說明
    MovingToCustomer,       // 正在走向顧客
    TalkingToCustomer,      // 正在與顧客對話
    ReviewingOrder,         // 正在回顧訂單
    MovingToStaff,          // 正在走向員工
    OrderingAtStaff,        // 正在與員工點餐
    ReturningToCustomer,    // 正在返回顧客
    Completed               // 已完成點餐任務
}

public class Gameflow : MonoBehaviour
{
    [Header("玩家狀態")]
    public PlayerState currentPlayerState = PlayerState.ReadingDescription;
    public PlayerState previousPlayerState = PlayerState.ReadingDescription;

    // 事件系統，用於通知狀態變化
    public event System.Action<PlayerState> OnPlayerStateChanged;

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

    // 公共方法供子類別或其他腳本呼叫來更新狀態
    public void NotifyReadingDescription() => SetPlayerState(PlayerState.ReadingDescription);
    public void NotifyCustomerInteractionStarted() => SetPlayerState(PlayerState.TalkingToCustomer);
    public void NotifyStaffInteractionStarted() => SetPlayerState(PlayerState.OrderingAtStaff);
    public void NotifyReviewingOrder() => SetPlayerState(PlayerState.ReviewingOrder);
    public void NotifyMovingToStaff() => SetPlayerState(PlayerState.MovingToStaff);
    public void NotifyMovingToCustomer() => SetPlayerState(PlayerState.MovingToCustomer);
    public void NotifyReturningToCustomer() => SetPlayerState(PlayerState.ReturningToCustomer);
    public void NotifyRestorePreviousState() => SetPlayerState(previousPlayerState);
    public void NotifyCompleted() => SetPlayerState(PlayerState.Completed);
}