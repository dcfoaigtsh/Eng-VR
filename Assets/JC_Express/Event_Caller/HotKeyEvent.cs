using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HotKeyEvent : MonoBehaviour
{
    public UnityEvent hotKeyEvent;
    public KeyCode hotKey = KeyCode.F; // 預設熱鍵為F鍵
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(hotKey))
        {
            hotKeyEvent.Invoke(); // 當按下熱鍵時，觸發事件
        }   
    }
}
