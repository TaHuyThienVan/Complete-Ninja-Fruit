using UnityEngine;
using System.Collections;
public class Spawner : Singleton<Spawner>
{
    private Collider spawnArea;

    [SerializeField] GameObject[] fruitPrefabs;
    [SerializeField] GameObject boomPrefab;

    [SerializeField] float minSpawndelay = 0.25f;
    [SerializeField] float maxSpawndelay = 1f;

    [SerializeField] float minAngle = -15f;
    [SerializeField] float maxAngle = 15f;

    [SerializeField] float minForce = 18f;
    [SerializeField] float maxForce = 22f;

    [SerializeField] float maxLifeTime = 5f;
    [Range(0,1f)] float boomchance = 0.05f;
    int stage = 1;
    bool sequenceActive;
    bool sequenceFruitInFlight;
    bool lastSpawnWasBomb;
    float nextEventTime;
    

    protected override void Awake()
    {
        base.Awake();
        this.LoadCompnents();
    }
    protected override void LoadCompnents()
    {
        base.LoadCompnents();
        this.spawnArea = GetComponent<Collider>();

    }
    private void OnEnable() // khi script duoc bat
    {
        StartCoroutine(Spawn());
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Spawn()
    {
        nextEventTime = Time.time + Random.Range(16f, 23f);
        yield return new WaitForSeconds(1.2f);

        while (enabled && fruitPrefabs != null && fruitPrefabs.Length > 0 && spawnArea != null)
        {
            GameManager game = GameManager.instance;
            if (sequenceActive && sequenceFruitInFlight)
            {
                yield return new WaitForSeconds(0.15f);
                continue;
            }
            if (sequenceActive && game.SequenceActive)
            {
                int step = game.SequenceStep;
                int[] order = { 0, 2, 4 };
                if (step >= 0 && step < order.Length && fruitPrefabs.Length > order[step])
                {
                    SpawnOne(fruitPrefabs[order[step]], 1f, false, FruitSpecialType.Normal, step);
                    sequenceFruitInFlight = true;
                }
                else EndSequenceChallenge();
            }
            else
            {
                if (sequenceActive) EndSequenceChallenge();
                bool isBomb = game.CanSpawnBomb && !lastSpawnWasBomb && Random.value < Mathf.Min(0.14f, boomchance + (stage - 1) * 0.012f);
                GameObject prefab = isBomb && boomPrefab != null ? boomPrefab : fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];
                lastSpawnWasBomb = isBomb;
                SpawnOne(prefab, 1f, isBomb, isBomb ? FruitSpecialType.Normal : ChooseSpecial());

                if (stage >= 2 && Time.time >= nextEventTime)
                {
                    game.StartFruitRush(3f);
                    SpawnOne(fruitPrefabs[Random.Range(0, fruitPrefabs.Length)], 1.15f, false, ChooseSpecial());
                    SpawnOne(fruitPrefabs[Random.Range(0, fruitPrefabs.Length)], 1.15f, false, ChooseSpecial());
                    nextEventTime = Time.time + Random.Range(18f, 26f);
                }
            }

            float difficulty = Mathf.Clamp01((stage - 1) / 8f);
            float delay = Random.Range(minSpawndelay, maxSpawndelay) * Mathf.Lerp(1f, 0.65f, difficulty) * game.SpawnIntervalMultiplier;
            yield return new WaitForSeconds(Mathf.Max(0.2f, delay));
        }
    }

    public void ResetRun()
    {
        foreach (Fruit fruit in FindObjectsByType<Fruit>())
            if (fruit.transform.root != transform.root) Destroy(fruit.transform.root.gameObject);
        foreach (Bomb bomb in FindObjectsByType<Bomb>())
            if (bomb.transform.root != transform.root) Destroy(bomb.transform.root.gameObject);
        stage = 1;
        sequenceActive = false;
        sequenceFruitInFlight = false;
        lastSpawnWasBomb = false;
        nextEventTime = Time.time + Random.Range(16f, 23f);
    }

    public void SetStage(int value) => stage = Mathf.Max(1, value);

    public void BeginSequenceChallenge()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length < 5) return;
        sequenceActive = true;
        sequenceFruitInFlight = false;
    }

    public void SequenceStepReady() => sequenceFruitInFlight = false;

    public void EndSequenceChallenge()
    {
        sequenceActive = false;
        sequenceFruitInFlight = false;
    }

    public void SpawnBossFruit()
    {
        GameObject prefab = null;
        foreach (GameObject fruit in fruitPrefabs)
            if (fruit.name.IndexOf("Watermelon", System.StringComparison.OrdinalIgnoreCase) >= 0) { prefab = fruit; break; }
        if (prefab == null && fruitPrefabs.Length > 0) prefab = fruitPrefabs[0];
        if (prefab == null || spawnArea == null) return;
        Vector3 position = new Vector3(0f, spawnArea.bounds.min.y, spawnArea.bounds.center.z);
        GameObject boss = Instantiate(prefab, position, Quaternion.identity);
        boss.transform.localScale *= 1.65f;
        Fruit fruitComponent = boss.GetComponentInChildren<Fruit>();
        if (fruitComponent != null) fruitComponent.ConfigureSpecial(FruitSpecialType.Boss, 5);
        Destroy(boss, maxLifeTime + 8f);
        Rigidbody body = boss.GetComponentInChildren<Rigidbody>();
        if (body != null) body.AddForce(Vector3.up * Random.Range(minForce, maxForce) * 1.1f, ForceMode.Impulse);
    }

    FruitSpecialType ChooseSpecial()
    {
        float roll = Random.value;
        if (roll < 0.025f) return FruitSpecialType.Golden;
        if (roll < 0.05f) return FruitSpecialType.Frozen;
        if (roll < 0.075f) return FruitSpecialType.Bonus;
        if (roll < 0.095f) return FruitSpecialType.Fake;
        return FruitSpecialType.Normal;
    }

    void SpawnOne(GameObject prefab, float forceMultiplier, bool isBomb, FruitSpecialType special, int challengeStep = -1)
    {
        Vector3 position = new Vector3(
            Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
            Random.Range(spawnArea.bounds.min.y, spawnArea.bounds.max.y),
            Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z));
        Quaternion rotation = Quaternion.Euler(0f, 0f, Random.Range(minAngle, maxAngle));
        GameObject spawned = Instantiate(prefab, position, rotation);
        Destroy(spawned, maxLifeTime);
        float force = Random.Range(minForce, maxForce) * forceMultiplier * (1f + Mathf.Min(stage - 1, 8) * 0.035f);
        Rigidbody body = spawned.GetComponentInChildren<Rigidbody>();
        if (body != null) body.AddForce(spawned.transform.up * force, ForceMode.Impulse);
        if (isBomb)
        {
            Bomb bomb = spawned.GetComponentInChildren<Bomb>();
            if (bomb != null)
            {
                int pattern = Random.Range(0, 3);
                bomb.Configure(pattern == 1, pattern == 2, Random.Range(4f, 6f));
            }
        }
        else
        {
            Fruit fruit = spawned.GetComponentInChildren<Fruit>();
            if (fruit != null)
            {
                fruit.ConfigureSpecial(special);
                fruit.SetChallengeStep(challengeStep);
            }
        }
    }
}
