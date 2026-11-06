using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FruitSpecialType { Normal, Golden, Frozen, Bonus, Fake, Boss }

public class Fruit : VanMonoBeha
{
    const string Player_String = "Player";
    public GameObject whole;
    public GameObject sliced;
    [SerializeField] GameObject JuiceEffectPrefabs;

    Rigidbody fruitRigidbody;
    Collider fruitCollider;

    [SerializeField] ParticleSystem juiceParitcleSystem;

    [SerializeField] int point = 1;
    bool isSliced;
    bool missed;
    bool hasEnteredPlayfield;
    FruitSpecialType specialType;
    int challengeStep = -1;
    int bossHitsRemaining;
    float lastBossHitTime = -1f;

    public int ChallengeStep => challengeStep;

    public void ConfigureSpecial(FruitSpecialType type, int bossHits = 0)
    {
        specialType = type;
        bossHitsRemaining = bossHits;
        if (type == FruitSpecialType.Fake)
        {
            Vector3 size = fruitCollider != null ? fruitCollider.bounds.size : Vector3.zero;
            float largestDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            transform.localScale *= Mathf.Min(0.78f, largestDimension > 0.01f ? 1.6f / largestDimension : 0.35f);
        }
        if (type == FruitSpecialType.Normal || whole == null) return;
        Color tint = type == FruitSpecialType.Golden ? new Color(1f, 0.78f, 0.2f) :
                     type == FruitSpecialType.Frozen ? new Color(0.45f, 0.9f, 1f) :
                     type == FruitSpecialType.Bonus ? new Color(0.6f, 1f, 0.45f) :
                     type == FruitSpecialType.Fake ? new Color(0.92f, 0.22f, 0.86f) :
                     new Color(1f, 0.45f, 0.18f);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        ApplyTint(whole, tint, block);
        if (type == FruitSpecialType.Fake && sliced != null) ApplyTint(sliced, tint, block);
    }

    static void ApplyTint(GameObject target, Color tint, MaterialPropertyBlock block)
    {
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
        }
    }

    public void SetChallengeStep(int step) => challengeStep = step;
    protected override void Awake()
    {
        base.Awake();
        this.LoadCompnents();

    }
    protected override void LoadCompnents()
    {
        base.LoadCompnents();
        this.LoadCollider();
        this.LoadRigidbody();
        this.LoadjuiceParticlyeSystem();

    }
    
    void LoadCollider()
    {
        if (this.fruitCollider != null) return;
        this.fruitCollider = GetComponent<Collider>();

    }
    void LoadRigidbody()
    {
        if (this.fruitRigidbody != null) return;
        this.fruitRigidbody = GetComponent<Rigidbody>();
    }
    void LoadjuiceParticlyeSystem()
    {
        if (this.juiceParitcleSystem != null) return;
        this.juiceParitcleSystem = GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        if (transform.position.y >= GameManager.instance.MissLineY) hasEnteredPlayfield = true;
        if (!isSliced && !missed && hasEnteredPlayfield && whole.activeSelf && GameManager.instance.IsGameRunning && transform.position.y < GameManager.instance.MissLineY)
        {
            missed = true;
            if (specialType == FruitSpecialType.Boss) GameManager.instance.BossEscaped();
            GameManager.instance.MissedFruit();
            Destroy(gameObject);
        }
    }



    internal void TrySlice(Vector3 direction, Vector3 position, float force)
    {
        if (isSliced || !GameManager.instance.IsGameRunning || GameManager.instance.IsPaused) return;
        if (specialType == FruitSpecialType.Boss && bossHitsRemaining > 0 && Time.time - lastBossHitTime < 0.25f) return;
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlaySlice();
        if (specialType == FruitSpecialType.Boss && bossHitsRemaining > 0)
        {
            lastBossHitTime = Time.time;
            bossHitsRemaining--;
            GameManager.instance.RegisterBossHit();
            PlayJuiceEffect();
            if (bossHitsRemaining > 0) return;
            GameManager.instance.BossDefeated();
        }
        isSliced = true;
        GameManager.instance.OnSpecialFruit(specialType);
        if (specialType == FruitSpecialType.Fake) GameManager.instance.HitFakeFruit();
        else
        {
            int points = specialType == FruitSpecialType.Golden ? point * 3 :
                         specialType == FruitSpecialType.Bonus ? point + 9 : point;
            GameManager.instance.IncreaseScore(points);
            if (specialType == FruitSpecialType.Frozen) GameManager.instance.ApplyFreeze(2f);
        }
        GameManager.instance.RegisterSequenceSlice(challengeStep);
        this.whole.SetActive(false);
        this.sliced.SetActive(true);
        PlayJuiceEffect();
        

        

        fruitCollider.enabled = false; // tranh viec cat 2 lan

        float angle = Mathf.Atan2(direction.y,direction.x) * Mathf.Rad2Deg; // xoay fruit
        this.sliced.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Rigidbody[] slices = sliced.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody slice in slices)
        {
            slice.linearVelocity = fruitRigidbody.linearVelocity;
            slice.AddForceAtPosition(direction * force, position,ForceMode.Impulse);
        }
    }

    void PlayJuiceEffect()
    {
        if (JuiceEffectPrefabs != null) Destroy(Instantiate(JuiceEffectPrefabs, transform.position, Quaternion.identity), 2f);
        if (juiceParitcleSystem != null) juiceParitcleSystem.Play();
    }
    
    private void OnTriggerEnter(Collider collision)
    {
        if(collision.CompareTag(Player_String))
        {
            Blade blade = collision.GetComponent<Blade>();
            if (blade != null && blade.IsPathClear()) TrySlice(blade.direction, blade.transform.position, blade.sliceForce);
        }
    }
}
