using TMPro;
using UnityEngine;

public class Money : MonoBehaviour
{
    public TMP_Text moneyText;

    public void Start()
    {
        moneyText = GetComponent<TMP_Text>();
    }

    public void Update()
    {
        try
        {
            moneyText.text = $"Money: {Globals.cash}";
        }
        catch
        {
            moneyText.text = $"Money: 0";
        }
    }
}