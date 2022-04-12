using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private ParticleSystem ShootingSystem;
    [SerializeField] private Transform BulletSpawnPoint;
    [SerializeField] private GameObject ImpactParticleSystemPrefab;
    [SerializeField] private GameObject BulletTrailPrefab;
    [SerializeField] private float ShootDelay = 0.1f;
    [SerializeField] private float Speed = 100;
    [SerializeField] private LayerMask Mask;
    [SerializeField] private bool BouncingBullets;
    [SerializeField] private float BounceDistance = 10f;

    private float LastShootTime;

    public void Shoot()
    {
        if (LastShootTime + ShootDelay < Time.time)
        {
            ShootingSystem.Play();
            StartCoroutine(SpawnTrail(transform.forward));
            LastShootTime = Time.time;
        }
    }

    private IEnumerator SpawnTrail(Vector3 direction)
    {
        GameObject bulletTrail = Lean.Pool.LeanPool.Spawn(BulletTrailPrefab, BulletSpawnPoint.position, Quaternion.identity);
        TrailRenderer trail = bulletTrail.GetComponent<TrailRenderer>();
        if (trail != null) 
        {
            Vector3 hitPoint = direction * 100;
            Vector3 hitNormal = Vector3.zero;
            bool impact = Physics.Raycast(BulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue, Mask);
            if (impact) {
                hitPoint = hit.point;
                hitNormal = hit.normal;
            }

            yield return StartCoroutine(HitAndBounce(trail, direction, hitPoint, hitNormal, BounceDistance, impact));
    
            Lean.Pool.LeanPool.Despawn(bulletTrail, trail.time);
        }
        else
        {
            Debug.Log("FAILED: to create trail renderer");
        }
    }

    private IEnumerator HitAndBounce(TrailRenderer Trail, Vector3 trailDirection, Vector3 HitPoint, Vector3 HitNormal, float BounceDistance, bool MadeImpact)
    {
        Vector3 startPosition = Trail.transform.position;
        float distance = Vector3.Distance(Trail.transform.position, HitPoint);
        float startingDistance = distance;

        while (distance > 0)
        {
            Trail.transform.position = Vector3.Lerp(startPosition, HitPoint, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * Speed;

            yield return null;
        }

        Trail.transform.position = HitPoint;

        if (MadeImpact)
        {
            GameObject impactParticleSystem = Lean.Pool.LeanPool.Spawn(ImpactParticleSystemPrefab, HitPoint, Quaternion.LookRotation(HitNormal));
            if (impactParticleSystem != null)
            {
                Lean.Pool.LeanPool.Despawn(impactParticleSystem, 0.5f);

                if (BouncingBullets && BounceDistance > 0)
                {
                    Vector3 bounceDirection = Vector3.Reflect(trailDirection, HitNormal).normalized;
                    Vector3 newHitPoint = bounceDirection * BounceDistance;
                    Vector3 newHitNormal = Vector3.zero;
                    bool impact = Physics.Raycast(HitPoint, bounceDirection, out RaycastHit hit, BounceDistance, Mask);
                    if (impact) 
                    {
                        newHitPoint = hit.point;
                        newHitNormal = hit.normal;
                        BounceDistance -= Vector3.Distance(newHitPoint, HitPoint);
                    }

                    yield return StartCoroutine(HitAndBounce(Trail, bounceDirection, newHitPoint, newHitNormal, BounceDistance, impact));
                }
            }
            else
            {
                Debug.Log("FAILED: to create particle system");
            }
        }
    }
}
