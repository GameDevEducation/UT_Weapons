using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BaseProjectile : MonoBehaviour
{
    public enum EProjectilePhysics
    {
        Engine,
        Custom
    }

    [SerializeField] protected EProjectilePhysics PhysicsMode = EProjectilePhysics.Engine;
    [SerializeField] protected float MaxLifeTime = 30f;

    protected Rigidbody LinkedRB;
    protected Vector3 LaunchDirection;
    protected float LaunchSpeed;
    protected float LifeTimeRemaining = -1f;

    void Awake()
    {
        LinkedRB = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (LifeTimeRemaining > 0f)
        {
            LifeTimeRemaining -= Time.deltaTime;
            if (LifeTimeRemaining <= 0f)
            {
                Debug.Log($"Projectile despawned due to lifetime limit reached");
                Destroy(gameObject);
                return;
            }

            if (PhysicsMode == EProjectilePhysics.Custom)
                Tick_ProjectileMovement(Time.deltaTime);
        }
    }

    protected virtual void Tick_ProjectileMovement(float InDeltaTime)
    {
        LinkedRB.MovePosition(LinkedRB.position + LaunchDirection * LaunchSpeed * InDeltaTime);
    }

    public void Launch(Vector3 InFireDirection, float InProjectileLaunchSpeed)
    {
        OnLaunch(InFireDirection, InProjectileLaunchSpeed);
    }

    protected virtual void OnLaunch(Vector3 InFireDirection, float InProjectileLaunchSpeed)
    {
        LifeTimeRemaining = MaxLifeTime;

        LaunchDirection = InFireDirection;
        LaunchSpeed = InProjectileLaunchSpeed;

        if (PhysicsMode == EProjectilePhysics.Engine)
            LinkedRB.AddForce(LaunchDirection * LaunchSpeed, ForceMode.VelocityChange);
    }

    void OnCollisionEnter(Collision InCollision)
    {
        OnHit(InCollision);
    }

    protected virtual void OnHit(Collision InCollision)
    {
        Debug.Log($"Projectile Hit: {InCollision.gameObject.name} at {InCollision.contacts[0].point}");
    }
}
