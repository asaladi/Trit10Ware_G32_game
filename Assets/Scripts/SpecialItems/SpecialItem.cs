using UnityEngine;

public class Player {}

/// <summary>
/// Base class for power-ups and obstables.
/// </summary>
public abstract class SpecialItem : MonoBehaviour {
    public RockDrop rockDrop;
    public float despawnXLimit = -30f;
    static public float baseScrollingSpeed = 2f;

    void Awake() {
        if (rockDrop == null)
            rockDrop = Object.FindAnyObjectByType<RockDrop>();
    }

    void Update() {
        if (transform.position.x < despawnXLimit) {
            Despawn();
        } else {
            Vector3 position = transform.position;
            position.x -= baseScrollingSpeed * rockDrop.speedMultiplier * Time.deltaTime;
            transform.position = position;
        }
    }

    /// <summary>
    /// Trigger an effect.
    /// </summary>
    public abstract void Trigger();

    public void Despawn() {
        Destroy(gameObject);
    }
}
