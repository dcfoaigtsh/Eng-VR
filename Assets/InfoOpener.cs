using UnityEngine;

public class InfoOpener : MonoBehaviour
{
    public GameObject gameDescriptionObject; // 拖入 ID_GameDescription 腳本所在物件

    public void OpenDescription()
    {
        var desc = gameDescriptionObject.GetComponent<ID_GameDescription>();
        if (desc != null)
        {
            desc.OpenInfoFromButton(); // 呼叫開啟面板的函式
        }
    }
}
