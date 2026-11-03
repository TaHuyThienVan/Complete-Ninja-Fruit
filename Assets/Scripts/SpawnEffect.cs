using UnityEngine;

// Manager chuyên lo hi?u ?ng
public class SpawnEffect : Singleton<SpawnEffect>
{
    [SerializeField] private GameObject boomEffect;
    public void SpawnEffectBoom(Vector3 position, Quaternion rotation)
    {
        GameObject effect = Instantiate(boomEffect, new Vector3(position.x, position.y, 0), rotation);
        Destroy(effect, 2f);
    }

}
