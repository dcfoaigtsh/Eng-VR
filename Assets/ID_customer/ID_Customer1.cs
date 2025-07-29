using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;

public class ID_SingleCustomer : MonoBehaviour
{
    public TextMeshProUGUI statementText;
    public Button responseButton;
    public GameObject qaManager;
    public ID_Gameflow customerManager;
    public GameObject completeIcon;

    [Header("Navigation")]
    public DestinationLineDrawer drawer;
    public Transform employee1;
    public NavMeshAgent agentForThisRoute;

    [TextArea]
    public string orderMessage = "Please talk to clerk 1 and help me buy:\nBurger, French Fries, Chicken Nuggets, Coke";

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
                if (employee1 != null)
                    drawer.ChangeDestination(employee1);
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

    public void NotifyCustomerManager()
    {
        if (completeIcon != null)
            completeIcon.SetActive(true);
        if (customerManager != null)
            customerManager.NextCustomer();
    }
}
