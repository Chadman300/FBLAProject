using MoreMountains.Feedbacks;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class GunController : MonoBehaviour
{
    public bool isEquipt = false;

    [Header("Feedbacks Parameters")]
    [SerializeField] private MMF_Player shootFeedback;

    [Header("Gun Parameters")]
    [SerializeField] private Rigidbody gunRb;
    public Rigidbody playerRb;
    [SerializeField] private Vector2 damage;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private float maxRaycastDistance = float.MaxValue;
    [SerializeField] private float maxSpreadTime = 10f;
    [SerializeField] private Vector2 recoilForce = new Vector2(10f, 15f);
    [SerializeField] private Vector2 playerRecoilForce = new Vector2(10f, 15f);
    [SerializeField] private float hitForce = 10f;
    public bool isRightHand = true;
    [Space]
    public GunShootType currentShootType = GunShootType.FullAuto;
    public GunShootType[] availableShootTypes = { GunShootType.FullAuto, GunShootType.SemiAuto };

    [Header("Bullet Config")]
    [SerializeField] private bool isHitScan = true;
    [SerializeField] private Transform bulletSpawn;

    [Header("Ammo Config")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo = 30;

    /*
    [Header("Non Hitscan Parameters")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletVelocity = 250f;
    */

    [Header("Trail Renderer Proppertys")]
    // Proppertys for trail renderer
    [SerializeField] private Material trailMaterial;
    [SerializeField] private AnimationCurve trailWidthCurve;
    [SerializeField] private float trailDuration = 0.5f;
    [SerializeField] private float trailMinVertexDistance = 0.1f;
    [SerializeField] private Gradient trailColor;
    [SerializeField] private bool trailEmmiting = true;
    [SerializeField] private bool trailShadowCasting = false;

    [Header("Trail Movement")]
    // Sim speed is how quickky the trail moves, miss dis is how far it gose after you miss
    [SerializeField] private float trailMissDistance = 100f;
    [SerializeField] private float trailSimulationSpeed = 100f;

    private ObjectPool<TrailRenderer> trailPool;
    private float lastShootTime;
    private float stopShootingTime;
    private float initialClickTime;

    void Awake()
    {
        trailPool = new ObjectPool<TrailRenderer>(CreateTrail);
        gunRb = GetComponent<Rigidbody>();
        lastShootTime = 0; // in editor this will not be propperly reset, in build it's fine
        //currentAmmo = maxAmmo;
    }

    private void Update()
    {
        if(currentShootType == GunShootType.SemiAuto)
        {
            if (Input.GetKeyDown(isRightHand ? KeyCode.Mouse1 : KeyCode.Mouse0))
            {
                TryToShoot();
            }
        }
        else if (currentShootType == GunShootType.FullAuto)
        {
            if (Input.GetKey(isRightHand ? KeyCode.Mouse1 : KeyCode.Mouse0))
            {
                TryToShoot();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentAmmo = maxAmmo;
        }
    }

    public void TryToShoot()
    {
        if (!isEquipt)
            return;

        if (Time.time > fireRate + lastShootTime)
        {
            if (currentAmmo == 0)
            {
                //AudioConfig.PlayOutOfAmmoClip(shootingAudioSource);
                return;
            }

            lastShootTime = Time.time;
            //shootSystem.Play();
            //AudioConfig.PlayShootingClip(shootingAudioSource, AmmoConfig.CurrentClipAmmo == 1);

            //recoil camera add forces
            gunRb.AddForce(-bulletSpawn.forward * Random.Range(recoilForce.x, recoilForce.y), ForceMode.Impulse);
            playerRb.AddForce(-playerRb.transform.forward * Random.Range(playerRecoilForce.x, playerRecoilForce.y), ForceMode.Impulse);

            currentAmmo--;

            // play shell eject
            shootFeedback?.PlayFeedbacks();

            /*
            if (TrailConfig.isShellEjecting && TrailConfig.ShellModel != null)
            {
                activeMonoBehavior.StartCoroutine(PlayShellEject());
            }
            */

            if (isHitScan)
            {
                DoHitScanShoot();
            }
            else
            {
                //DoProjectileShoot(shootDirection);
            }
        }
    }

    private void DoHitScanShoot()
    {
        //shootDirection.Normalize();
        if (Physics.Raycast(
            bulletSpawn.position,
            bulletSpawn.forward,
            out RaycastHit hit,
            maxRaycastDistance,
            hitMask
            ))
        {
            StartCoroutine(
                PlayTrail(
                    bulletSpawn.position,
                    hit.point,
                    hit
                    ));

            BulletCollision(hit);
        }
        else
        {
            StartCoroutine(
                PlayTrail(
                    bulletSpawn.position,
                    bulletSpawn.position + (bulletSpawn.forward * trailMissDistance),
                    new RaycastHit()
                    ));
        }
    }

    private void BulletCollision(RaycastHit hit)
    {
        if(hit.transform.gameObject.TryGetComponent<EnemyController>(out EnemyController enemyController))
        {
            enemyController.ApplyDamage(Random.Range(damage.x, damage.y));
            enemyController.rb.AddForce(hitForce * bulletSpawn.transform.forward, ForceMode.Impulse);
        }
    }

    private IEnumerator DeleyedDisableTrail(TrailRenderer trail)
    {
        yield return new WaitForSeconds(trailDuration);
        yield return null;
        trail.emitting = false;
        trail.gameObject.SetActive(false);
        trailPool.Release(trail);
    }

    private IEnumerator PlayTrail(Vector3 startPoint, Vector3 endPoint, RaycastHit hit)
    {
        TrailRenderer instance = trailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = startPoint;
        //instance.gameObject.layer = TrailConfig.weaponLayer;

        yield return null; // avoid position carry-over from last frame if resued

        instance.emitting = trailEmmiting;

        float distance = Vector3.Distance(startPoint, endPoint);
        float remainingDistance = distance;
        while (remainingDistance > 0)
        {
            instance.transform.position = Vector3.Lerp(
                startPoint,
                endPoint,
                Mathf.Clamp01(1 - (remainingDistance / distance))
                );
            remainingDistance -= trailSimulationSpeed * Time.deltaTime;

            yield return null;
        }

        instance.transform.position = endPoint;

        // Surface Manager
        /*
        if (hit.collider != null)
        {
            HandleBulletImpact(distance, endPoint, hit.normal, hit.collider);
        }
        */

        yield return new WaitForSeconds(trailDuration);
        yield return null;
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        trailPool.Release(instance);
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();

        // stuff from the TrailConfigScriptableObj
        trail.colorGradient = trailColor;
        trail.material = trailMaterial;
        trail.widthCurve = trailWidthCurve;
        trail.time = trailDuration;
        trail.minVertexDistance = trailMinVertexDistance;

        trail.emitting = false; // >may want to play with this later (add some smoke effects maybe?)<
        trail.shadowCastingMode = trailShadowCasting ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }
}
