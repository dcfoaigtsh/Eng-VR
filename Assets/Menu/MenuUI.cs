using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuUI : MonoBehaviour
{
    public void OnSelectMode(int modeIndex)
    {
        if (modeIndex == 0)
        {
            ModeManager.Instance.SetMode(modeIndex);
            SceneManager.LoadScene("WordLearning");  // 改成你遊戲場景的名稱
        }
        else if (modeIndex == 1)
        {
            ModeManager.Instance.SetMode(modeIndex);
            SceneManager.LoadScene("WordLearning");  // 改成你遊戲場景的名稱
        }
        else if (modeIndex == 2)
        {
            ModeManager.Instance.SetMode(modeIndex);
            SceneManager.LoadScene("WordLearning");  // 改成你遊戲場景的名稱
        }
        else if (modeIndex == 3)
        {
            ModeManager.Instance.SetMode(modeIndex);
            SceneManager.LoadScene("WordLearning");  // 改成你遊戲場景的名稱
        }
        else
        {
            Debug.LogError("無效的模式索引：" + modeIndex);
        }
    }
}
