using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public CoinManager coinManager;
    int coins = 0;

    public void AddCoins(int amount)
    {
        coins += amount;
        coinManager.UpdateCoinUI(coins);
    }
}
