using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            PlayerInventory inventory = collision.gameObject.GetComponent<PlayerInventory>();
            inventory.AddCoins(1);

            Destroy(gameObject);

        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        
    }
}
