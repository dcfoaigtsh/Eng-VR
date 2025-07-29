using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ASD_SingleCustomer2: MonoBehaviour
{
    public TextMeshProUGUI statementText;
    public Button responseButton;
    public GameObject qaManager;
    public ASD_Gameflow customerManager;
    public GameObject completeIcon;

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee2;
    public UnityEngine.AI.NavMeshAgent agentForThisRoute;
    

    [TextArea]
    public string orderMessage = "Please talk to clerk 2 and help me buy:\nHot Dog, Salad, Onion Rings, Sprite";

    void OnEnable()
    {
        ShowGreeting();
    }

    void ShowGreeting()
    {
        statementText.text = "Can you help me buy some food?";
        responseButton.GetComponentInChildren<TextMeshProUGUI>().text = "Yes";
        responseButton.onClick.RemoveAllListeners();
        responseButton.onClick.AddListener(ShowOrder);
    }

    void ShowOrder()
    {
        statementText.text = orderMessage;
        responseButton.GetComponentInChildren<TextMeshProUGUI>().text = "OK";
        responseButton.onClick.RemoveAllListeners();
         responseButton.onClick.AddListener(() => {
            if (drawer != null)
            {
                if (employee2 != null)
                    drawer.ChangeDestination(employee2);
                if (agentForThisRoute != null)
                    drawer.ChangeNavAgent(agentForThisRoute);
            }

            StartQA();
        });
    }

    void StartQA()
    {
        gameObject.SetActive(false);
        qaManager.SetActive(true);
    }

    // 呼叫此函數讓下一位顧客開始
    public void NotifyCustomerManager()
    {
        if (completeIcon != null)
            completeIcon.SetActive(true);
        if (customerManager != null)
            customerManager.NextCustomer();
    }
}
