using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Qué arma otorga este objeto")]
    public PlayerMovement.WeaponType weaponToEquip = PlayerMovement.WeaponType.Revolver;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            
            if (pm != null)
            {
                bool alreadyHasWeapon = false;
                if (weaponToEquip == PlayerMovement.WeaponType.Pipe && pm.hasPipe) alreadyHasWeapon = true;
                if (weaponToEquip == PlayerMovement.WeaponType.Revolver && pm.hasRevolver) alreadyHasWeapon = true;

                if (!alreadyHasWeapon)
                {
                    pm.EquipWeapon(weaponToEquip); 
                    Destroy(gameObject); 
                }
            }
        }
    }
}