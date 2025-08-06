using UnityEngine;
using UnityEngine.UI;

public class IDProgressBar : MonoBehaviour
{
    public Image progressImage;       // 拖入 UI Image 元件
    public Sprite[] progressSprites;  // 依序放入 3 張進度圖

    public void SetProgress(int step)
    {
        if (step >= 0 && step < progressSprites.Length)
        {
            progressImage.sprite = progressSprites[step];
        }
    }
}
