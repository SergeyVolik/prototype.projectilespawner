using Prototype.ECS.Baking;
using Sirenix.OdinInspector;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Prototype.ProjectileSpawner
{

    [Flags]
    public enum SpawnProjectileMoveType : byte
    {
        Forward = 1 << 1,
        Angle = 1 << 2
    }

    public enum SpawnDelayType
    {
        Semi,
        Auto,
        Burst
    }


    public struct BurstSpawnC : IComponentData, IEnableableComponent
    {
        public float delays;
        public int produceTimes;
        public int currentProduceCount;
        public Entity cooldownDelayE;
    }

    public struct SemiSpawnC : IComponentData, IEnableableComponent { }

    public struct AutoSpawnC : IComponentData, IEnableableComponent { }

    public struct ProjectileForwardModeC : IComponentData, IEnableableComponent
    {
        public int projectiles;
        public float projectileOffset;
    }
    public struct ExecuteProjectileSpawn : IComponentData, IEnableableComponent { }

    public struct ProjectileAngleModeC : IComponentData, IEnableableComponent
    {
        public float angleOffset;
        public int projectiles;
    }

    public struct ProjectileSpawnerC : IComponentData, IEnableableComponent
    {
        public Entity projectilePrefab;
        public float delayBetweenShots;
        public int projectiles;
        public Entity projectileSpawnPoint;
        public float speed;
        public int damage;

        public float projectileLifetime;
        public SpawnProjectileMoveType spawnType;
        public float angleOffset;
        public float projectileOffset;
        public bool twoDimMode;
    }

}

namespace Prototype.ProjectileSpawner
{

    [DisallowMultipleComponent]
    public class ProjectileSpawnerAuthoring : MonoBehaviour
    {
        private const string hasAngleFlag = "@(this.spawnMoveType & SpawnProjectileMoveType.Angle) == SpawnProjectileMoveType.Angle";
        private const string hasForwardFlag = "@(this.spawnMoveType & SpawnProjectileMoveType.Forward) == SpawnProjectileMoveType.Forward";
        private const string hasBurstMode = "@this.delayType == SpawnDelayType.Burst";
        public Transform spawnPoint;
        public GameObject projectilePrefab;
        public SpawnProjectileMoveType spawnMoveType;
        public SpawnDelayType delayType;

        public float projectileLifetime;
        public float delayBetweenShots;
        public float projectileSpeed;
        public bool twoDimMode;
        public int damage;


        [Min(1)]
        public int projectiles = 1;

        [BoxGroup("Angle")]
        [ShowIf(hasAngleFlag)]
        public float angleOffset = 1;

        [BoxGroup("Forward")]
        [ShowIf(hasForwardFlag)]
        public float projectileOffset = 0.1f;

        [BoxGroup("Burst")]
        [ShowIf(hasBurstMode)]
        public float burstSpawnDelays = 0.1f;
        [BoxGroup("Burst")]
        [ShowIf(hasBurstMode)]
        public int burstSpawns = 2;

        public bool ativeAtStart = false;

        void OnEnable() { }

        class Baker : BakerForEnabledComponent<ProjectileSpawnerAuthoring>
        {
            public override void BakeIfEnabled(ProjectileSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new ProjectileSpawnerC
                {
                    delayBetweenShots = authoring.delayBetweenShots,
                    speed = authoring.projectileSpeed,
                    projectileSpawnPoint = GetEntity(authoring.spawnPoint, TransformUsageFlags.Dynamic),
                    projectilePrefab = GetEntity(authoring.projectilePrefab, TransformUsageFlags.Dynamic),
                    projectiles = authoring.projectiles,
                    spawnType = authoring.spawnMoveType,
                    angleOffset = authoring.angleOffset,
                    projectileOffset = authoring.projectileOffset,
                    twoDimMode = authoring.twoDimMode,
                    damage = authoring.damage,
                    projectileLifetime = authoring.projectileLifetime,
                });
                SetComponentEnabled<ProjectileSpawnerC>(entity, authoring.ativeAtStart);

                AddComponent<ExecuteProjectileSpawn>(entity);
                SetComponentEnabled<ExecuteProjectileSpawn>(entity, false);

                AddComponent<ProjectileAngleModeC>(entity, new ProjectileAngleModeC
                {
                    angleOffset = authoring.angleOffset,
                    projectiles = authoring.projectiles,
                });
                SetComponentEnabled<ProjectileAngleModeC>(entity, (authoring.spawnMoveType & SpawnProjectileMoveType.Angle) == SpawnProjectileMoveType.Angle);
                AddComponent<ProjectileForwardModeC>(entity, new ProjectileForwardModeC
                {
                    projectiles = authoring.projectiles,
                    projectileOffset = authoring.projectileOffset
                });
                SetComponentEnabled<ProjectileForwardModeC>(entity, (authoring.spawnMoveType & SpawnProjectileMoveType.Forward) == SpawnProjectileMoveType.Forward);


                AddComponent<AutoSpawnC>(entity);
                SetComponentEnabled<AutoSpawnC>(entity, false);

                var burstCooldownE = CreateAdditionalEntity(TransformUsageFlags.None, false, "gun cooldown");

                AddComponent<CooldownC>(burstCooldownE);
                SetComponentEnabled<CooldownC>(burstCooldownE, false);

                AddComponent<BurstSpawnC>(entity, new BurstSpawnC
                {
                    delays = authoring.burstSpawnDelays,
                    produceTimes = authoring.burstSpawns,
                    cooldownDelayE = burstCooldownE
                });

                SetComponentEnabled<BurstSpawnC>(entity, false);

                AddComponent<SemiSpawnC>(entity);
                SetComponentEnabled<SemiSpawnC>(entity, false);

                switch (authoring.delayType)
                {
                    case SpawnDelayType.Semi:
                        SetComponentEnabled<SemiSpawnC>(entity, true);
                        break;
                    case SpawnDelayType.Auto:
                        SetComponentEnabled<AutoSpawnC>(entity, true);

                        break;
                    case SpawnDelayType.Burst:
                        SetComponentEnabled<BurstSpawnC>(entity, true);

                        break;
                    default:
                        break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if ((spawnMoveType & SpawnProjectileMoveType.Forward) == SpawnProjectileMoveType.Forward)
            {
                var fullOffset = projectileOffset * projectiles;
                var halfOffset = fullOffset / 2f - (projectileOffset / 2f);

                for (int i = 0; i < projectiles; i++)
                {

                    var forward = twoDimMode ? transform.up : transform.forward;
                    var right = transform.right;

                    var pos1 = spawnPoint.position;
                    var pos2 = spawnPoint.position + forward * (projectileLifetime * projectileSpeed);

                    pos1 = pos1 + right * (-halfOffset + projectileOffset * i);
                    pos2 = pos2 + right * (-halfOffset + projectileOffset * i);

                    Gizmos.DrawLine(pos1, pos2);
                }
            }

            if ((spawnMoveType & SpawnProjectileMoveType.Angle) == SpawnProjectileMoveType.Angle)
            {
                var allAngle = math.radians(angleOffset * (projectiles - 1));
                var halfAllAngle = allAngle / 2;
                var forwardVec = twoDimMode ? transform.up : transform.forward;
                var upVec = twoDimMode ? -transform.forward : transform.up;
                for (int i = 0; i < projectiles; i++)
                {

                    var qut = quaternion.AxisAngle(upVec, math.radians(angleOffset * i) - halfAllAngle);

                    var forward = math.mul(qut, forwardVec);

                    var pos1 = spawnPoint.position;
                    var pos2 = spawnPoint.position + (Vector3)forward * projectileLifetime * projectileSpeed;

                    Gizmos.DrawLine(pos1, pos2);
                }
            }
        }
    }
}