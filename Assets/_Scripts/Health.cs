using UnityEngine;

public class Health : MonoBehaviour {

    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private void Awake() {

        currentHealth = maxHealth;

    }

    public void TakeDamage(float amout) {

        currentHealth -= amout;
        Debug.Log(gameObject.name + " a pris " + amout + "degats . Vie restante " + currentHealth);

        if (currentHealth <= 0f) {

            Destroy(gameObject);

        }



    }

   

}
