using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    public enum EFireMode
    {
        Single,
        Burst,
        Continuous
    }

    public enum EProjectileType
    {
        HitScan,
        Projectile
    }

    protected enum EState
    {
        ReadyToFire,
        Firing,
        CoolingDown
    }

    [Header("Common")]
    [SerializeField] protected EFireMode FireMode = EFireMode.Single;
    [SerializeField] protected Transform MuzzleTransform;
    [SerializeField] protected float MaxWeaponRange = 50f;
    [SerializeField] protected float EndOfFireCooldown = 0f;

    [Header("Projectile Settings")]
    [SerializeField] protected EProjectileType ProjectileType = EProjectileType.HitScan;
    [SerializeField] protected float ProjectileLaunchSpeed = 20.0f;
    [SerializeField] protected GameObject ProjectilePrefab;

    [Header("Hitscan Settings")]
    [SerializeField] protected LayerMask HitScanLayerMask = ~0;

    [Header("Burst Fire Settings")]
    [SerializeField] protected int BurstSize = 3;
    [SerializeField] protected float BurstInterval = 0.25f;
    [SerializeField] protected float ErrorEscalationPerShot = 1.5f;

    [Header("Continuous Fire Settings")]
    [SerializeField] protected float MaxContinuousDuration = 5.0f;

    [Header("Accuracy")]
    [SerializeField] protected float MaxHorizontalAngleError = 1f;
    [SerializeField] protected float MaxVerticalAngleError = 1f;

    protected EState CurrentState = EState.ReadyToFire;

    protected float EndOfCooldownTimeRemaining = -1f;

    protected float NextBurstCooldownTimeRemaining = -1f;
    protected int BurstShotsRemaining = -1;
    protected float CurrentErrorEscalation = 1.0f;

    protected float ContinuousFireTimeRemaining = -1f;

    // Update is called once per frame
    void Update()
    {
        if (CurrentState == EState.Firing)
            Tick_Firing(Time.deltaTime);
        else if (CurrentState == EState.CoolingDown)
            Tick_Cooldown(Time.deltaTime);
    }

    public void DEBUG_StartFiring()
    {
        RequestStartFiring();
    }

    public void DEBUG_StopFiring()
    {
        RequestStopFiring();
    }

    public bool RequestStartFiring()
    {
        if (CurrentState != EState.ReadyToFire)
            return false;

        return OnStartFiring();
    }

    public bool RequestStopFiring()
    {
        if (CurrentState == EState.Firing)
            return OnStopFiring();

        return CurrentState == EState.CoolingDown;
    }

    protected virtual bool OnStartFiring()
    {
        CurrentState = EState.Firing;

        if (FireMode == EFireMode.Single)
            return OnStartFiring_Single();
        else if (FireMode == EFireMode.Burst)
            return OnStartFiring_Burst();
        else if (FireMode == EFireMode.Continuous)
            return OnStartFiring_Continuous();

        return false;
    }

    protected virtual bool OnStopFiring()
    {
        CurrentState = EState.CoolingDown;

        EndOfCooldownTimeRemaining = EndOfFireCooldown;
        NextBurstCooldownTimeRemaining = -1f;
        ContinuousFireTimeRemaining = -1f;
        CurrentErrorEscalation = 1.0f;
        BurstShotsRemaining = -1;

        return true;
    }

    protected virtual bool OnStartFiring_Single()
    {
        OnPerformSingleFire();

        OnStopFiring();

        return true;
    }

    protected virtual bool OnStartFiring_Burst()
    {
        OnPerformSingleFire();

        NextBurstCooldownTimeRemaining = BurstInterval;
        BurstShotsRemaining = BurstSize - 1;

        if (BurstShotsRemaining <= 0)
            OnStopFiring();

        return true;
    }

    protected virtual bool OnStartFiring_Continuous()
    {
        ContinuousFireTimeRemaining = MaxContinuousDuration;

        OnStartContinuousFireEffects();

        return true;
    }

    protected virtual void OnPerformSingleFire()
    {
        if (ProjectileType == EProjectileType.HitScan)
            OnPerformFire_HitScan();
        else
            OnPerformFire_Projectile();
    }

    protected virtual void OnStartContinuousFireEffects()
    {
        OnPerformFire_HitScan();
    }

    protected virtual void OnStopContinuousFireEffects()
    {

    }

    protected virtual void OnPerformFire_HitScan()
    {
        float HError = Random.Range(-MaxHorizontalAngleError * CurrentErrorEscalation,
                                     MaxHorizontalAngleError * CurrentErrorEscalation);
        float VError = Random.Range(-MaxVerticalAngleError * CurrentErrorEscalation,
                                     MaxVerticalAngleError * CurrentErrorEscalation);

        Vector3 FireDirection = Quaternion.Euler(VError, HError, 0f) * MuzzleTransform.forward;

        RaycastHit HitInfo;
        if (Physics.Raycast(MuzzleTransform.position, FireDirection,
                            out HitInfo, MaxWeaponRange, HitScanLayerMask,
                            QueryTriggerInteraction.Ignore))
        {
            OnHit(HitInfo);
        }
    }

    protected virtual void OnPerformFire_Projectile()
    {
        float HError = Random.Range(-MaxHorizontalAngleError * CurrentErrorEscalation,
                                     MaxHorizontalAngleError * CurrentErrorEscalation);
        float VError = Random.Range(-MaxVerticalAngleError * CurrentErrorEscalation,
                                     MaxVerticalAngleError * CurrentErrorEscalation);

        Vector3 FireDirection = Quaternion.Euler(VError, HError, 0f) * MuzzleTransform.forward;

        GameObject ProjectileGO = GameObject.Instantiate(ProjectilePrefab, MuzzleTransform.position, MuzzleTransform.rotation);

        ProjectileGO.GetComponent<BaseProjectile>().Launch(FireDirection, ProjectileLaunchSpeed);
    }

    protected virtual void OnHit(RaycastHit InHitInfo)
    {
        Debug.Log($"HitScan Hit: {InHitInfo.collider.gameObject.name} at {InHitInfo.point}");
    }

    protected void Tick_Firing(float InDeltaTime)
    {
        if (FireMode == EFireMode.Burst)
        {
            NextBurstCooldownTimeRemaining -= InDeltaTime;

            if (NextBurstCooldownTimeRemaining <= 0)
            {
                CurrentErrorEscalation = CurrentErrorEscalation * ErrorEscalationPerShot;

                OnPerformSingleFire();

                NextBurstCooldownTimeRemaining = BurstInterval;
                --BurstShotsRemaining;

                if (BurstShotsRemaining <= 0)
                    OnStopFiring();
            }
        }
        else if (FireMode == EFireMode.Continuous)
        {
            ContinuousFireTimeRemaining -= InDeltaTime;

            if (ContinuousFireTimeRemaining <= 0)
            {
                OnStopContinuousFireEffects();

                OnStopFiring();
            }
            else if (ProjectileType == EProjectileType.HitScan)
                OnPerformFire_HitScan();
        }
    }

    protected void Tick_Cooldown(float InDeltaTime)
    {
        if (CurrentState != EState.CoolingDown)
            return;

        EndOfCooldownTimeRemaining -= InDeltaTime;
        if (EndOfCooldownTimeRemaining <= 0f)
        {
            CurrentState = EState.ReadyToFire;
            EndOfCooldownTimeRemaining = -1f;
        }
    }
}
