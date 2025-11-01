using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public TextMeshProUGUI coinText;

    public void UpdateCoinUI(int coin)
    {
        coinText.text = coin.ToString();
    }
}
