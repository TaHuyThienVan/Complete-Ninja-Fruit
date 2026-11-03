using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bomb : MonoBehaviour
{
    const string Player_String = "Player";
    const float RotationForce = 20f;
    Rigidbody body;
    bool moves;
    bool timed;
    float fuse;
    bool warningPlayed;
    
    
    private void Start()
    {
        transform.rotation = Random.rotation;
        body = GetComponent<Rigidbody>();

    }
    
    private void Update()
    {
        transform.Rotate(Vector3.right, RotationForce * Time.deltaTime);
        if (moves && body != null) body.AddForce(Vector3.right * Mathf.Sin(Time.time * 2f) * 3f, ForceMode.Acceleration);
        if (!timed) return;
        fuse -= Time.deltaTime;
        if (!warningPlayed && fuse <= 1f)
        {
            warningPlayed = true;
            if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayBombWarning();
        }
        if (fuse <= 0f)
        {
            Blade blade = FindAnyObjectByType<Blade>();
            bool nearby = blade != null && Vector3.Distance(blade.transform.position, transform.position) < 2.5f;
            Explode(nearby);
        }
    }

    public void Configure(bool moving, bool timedFuse, float fuseSeconds)
    {
        moves = moving;
        timed = timedFuse;
        fuse = fuseSeconds;
        warningPlayed = false;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag(Player_String))
        {
            
            Explode(true);

        }
    }

    void Explode(bool penalize)
    {
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayBomb();
        if (SpawnEffect.instance != null) SpawnEffect.instance.SpawnEffectBoom(transform.position, Quaternion.identity);
        if (penalize && GameManager.instance != null) GameManager.instance.Explode();
        Destroy(gameObject);
    }
}
