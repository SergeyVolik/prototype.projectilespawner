using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Prototype.ProjectileSpawner
{
    [UpdateInGroup(typeof(ProjectileSpawnerGroup))]
    [UpdateAfter(typeof(ProjectileSpawnerDelaySystem))]
    public partial struct ProjectileSpawnerExecuteSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileSpawnerC>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (gun, e) in SystemAPI.Query<ProjectileSpawnerC>().WithAll<ExecuteProjectileSpawn>().WithEntityAccess())
            {
                SpawnProjectile(ref state, ecb, gun, e);

                ecb.SetComponentEnabled<ExecuteProjectileSpawn>(e, false);
            }
        }

        [BurstCompile]
        private void SpawnProjectile(ref SystemState state, EntityCommandBuffer ecb, in ProjectileSpawnerC gun, Entity gunEntity)
        {
            var fullOffset = gun.projectileOffset * gun.projectiles;
            var halfOffset = fullOffset / 2f - (gun.projectileOffset / 2f);

            var spawnPointLTW = SystemAPI.GetComponentRO<LocalToWorld>(gun.projectileSpawnPoint);
            var ltwSpawnPos = spawnPointLTW.ValueRO.Position;

            var ltwSpawnForward = gun.twoDimMode ? spawnPointLTW.ValueRO.Up : spawnPointLTW.ValueRO.Forward;
            var ltwSpawnRight = spawnPointLTW.ValueRO.Right;
            var ltwSpawnUp = gun.twoDimMode ? -spawnPointLTW.ValueRO.Forward : spawnPointLTW.ValueRO.Up;

            var allAngle = math.radians(gun.angleOffset * (gun.projectiles - 1));
            var halfAllAngle = allAngle / 2;

            var upWorld = gun.twoDimMode ? math.back() : math.up();


            for (int i = 0; i < gun.projectiles; i++)
            {
                float3 vector = ltwSpawnForward;
                float3 spawnPos = ltwSpawnPos;

                if ((gun.spawnType & SpawnProjectileMoveType.Forward) == SpawnProjectileMoveType.Forward)
                {
                    spawnPos = spawnPos + ltwSpawnRight * (-halfOffset + gun.projectileOffset * i);

                    var rotation = gun.twoDimMode ? quaternion.identity : quaternion.LookRotation(vector, math.up());
                    var instance = ecb.Instantiate(gun.projectilePrefab);

                    SetupProjectileInstance(ref state, ecb, gun, gunEntity, vector, spawnPos, rotation, instance);
                }

                if ((gun.spawnType & SpawnProjectileMoveType.Angle) == SpawnProjectileMoveType.Angle)
                {
                    var qut = quaternion.AxisAngle(ltwSpawnUp, math.radians(gun.angleOffset * i) - halfAllAngle);
                    vector = math.mul(qut, ltwSpawnForward);

                    var rotation = gun.twoDimMode ? quaternion.identity : quaternion.LookRotation(vector, math.up());
                    var instance = ecb.Instantiate(gun.projectilePrefab);

                    SetupProjectileInstance(ref state, ecb, gun, gunEntity, vector, spawnPos, rotation, instance);
                }
            }
        }

        private void SetupProjectileInstance(ref SystemState state, EntityCommandBuffer ecb, in ProjectileSpawnerC gun, Entity gunEntity, float3 vector, float3 spawnPos, quaternion rotation, Entity projectileInstance)
        {
            //PrototypeDebug.Log($"SetupProjectileInstance {projectileInstance}");
            ecb.SetComponent(projectileInstance, LocalTransform.FromPositionRotation(spawnPos, rotation));

            ecb.AddComponent(projectileInstance, new PhysicsVelocity
            {
                Linear = vector * gun.speed
            });

            ecb.AddComponent(projectileInstance, new ProjectileC
            {
                damage = gun.damage
            });

            ecb.SetupLifetime(projectileInstance, new LifetimeC
            {
                value = gun.projectileLifetime
            });

            if (SystemAPI.HasComponent<OwnerC>(gunEntity))
            {
                ecb.AddComponent(projectileInstance, new OwnerC
                {
                    value = SystemAPI.GetComponentRO<OwnerC>(gunEntity).ValueRO.value
                });
            }
        }
    }
}
