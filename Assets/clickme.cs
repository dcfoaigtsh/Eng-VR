using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DescriptionController : MonoBehaviour
{
    [Header("說明群組（通常指 Game description/Quiz1）")]
    public GameObject quizGroup;

    [Header("要一起打開的 Panel / 文字頁（例如 Quiz Page 1）")]
    public GameObject quizPage1;   // 指到 Game description/Quiz1/Quiz Page 1

    [Header("關閉按鈕 X（可選，通常會跟著一起開）")]
    public GameObject closeButton; // 指到 Game description/Quiz1/X

    public void OpenDescription()
    {
        // ★ 1. 先把整組開起來
        if (quizGroup != null)
            quizGroup.SetActive(true);

        // ★ 2. 再確保主要的說明頁面有被打開
        if (quizPage1 != null)
            quizPage1.SetActive(true);

        // ★ 3. X 按鈕也順便打開（避免之前被關掉）
        if (closeButton != null)
            closeButton.SetActive(true);

        Debug.Log("[Description] OpenDescription");
    }

    public void CloseDescription()
    {
        // 關掉整組就好（裡面的 Page1、X 都會一起隱藏）
        if (quizGroup != null)
            quizGroup.SetActive(false);

        Debug.Log("[Description] CloseDescription");
    }
}
