using UnityEngine;

public class LootDrop : MonoBehaviour
{
    [Header("Configuración de Loot")]
    public GameObject weaponPickupPrefab; 
    public float dropHeight = 0.5f;       
    public void DropWeapon()
    {
        if (weaponPickupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, dropHeight, 0);
            
            Instantiate(weaponPickupPrefab, spawnPosition, Quaternion.Euler(90, 0, 0));
        }
    }
}