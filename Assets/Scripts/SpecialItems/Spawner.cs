using UnityEngine;

public class Spawner : MonoBehaviour {
    public RockDrop rockDrop;

    [Header("Lily Pad Spawning")]
    public LilyPad lilyPadPrefab;
    public float lilyPadSpawnChance = 0.7f;
    public int minBouncesBetweenSpawns = 4;

    [Header("Positioning")]
    /// Area offscreen to the right where special items are spawned.
    public Rect spawnArea;

    private int bouncesSinceLastSpawn;

    void Awake() {
        if (rockDrop == null)
            rockDrop = Object.FindAnyObjectByType<RockDrop>();
    }

    void OnEnable()
    {
        rockDrop.Bounced += OnRockBounced;
    }

    void OnDisable()
    {
        rockDrop.Bounced -= OnRockBounced;
    }

    void Start() {
        bouncesSinceLastSpawn = 0;
    }

    private void OnRockBounced(Vector3 _) {
        bouncesSinceLastSpawn++;
        if (bouncesSinceLastSpawn >= minBouncesBetweenSpawns) {
            if (Random.value < lilyPadSpawnChance) {
                Vector3 spawnPosition = lilyPadPrefab.transform.position;
                spawnPosition.x = Random.Range(spawnArea.xMin, spawnArea.xMax);
                spawnPosition.z = Random.Range(spawnArea.yMin, spawnArea.yMax);

                Instantiate(lilyPadPrefab, spawnPosition, lilyPadPrefab.transform.rotation);
            }
            bouncesSinceLastSpawn = 0;
        }
    }
}
