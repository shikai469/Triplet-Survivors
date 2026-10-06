using UnityEngine;

[System.Serializable]
public class LootItem
{
    public GameObject itemPrefab;  // The prefab of the item to drop
    [Range(0f, 1f)]
    public float dropChance;      // Chance (0-1) to drop this item
}

public class LootDropSystem : MonoBehaviour
{
    [Header("Loot Settings")]
    public LootItem[] lootTable;  // Array of loot items with their drop chances

    public void DropLoot()
    {
        if (lootTable == null || lootTable.Length == 0) return;

        float totalChance = 0;

        // Calculate the total drop chance
        foreach (var lootItem in lootTable)
        {
            totalChance += lootItem.dropChance;
        }

        // Generate a random number between 0 and the total chance
        float randomValue = Random.Range(0f, totalChance);

        float cumulativeChance = 0;

        // Determine which item to drop
        foreach (var lootItem in lootTable)
        {
            cumulativeChance += lootItem.dropChance;

            if (randomValue <= cumulativeChance)
            {
                // Spawn the item at the enemy's position
                Instantiate(lootItem.itemPrefab, transform.position, Quaternion.identity);
                return;  // Ensure only one item is dropped
            }
        }
    }
}