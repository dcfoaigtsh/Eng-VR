using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using System.Threading.Tasks;

public class TA_Central : MonoBehaviour
{
    public ChatGPT chatGPT;
    public string myAPIKey;
    public string myOrganization;
    public string message;
    void Awake()
    {
        chatGPT.SetConfig(myAPIKey, myOrganization);
    }
    void Update()
    {

    }
}

