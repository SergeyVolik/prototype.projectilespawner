using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

namespace Prototype.ProjectileSpawner
{
    
    [UpdateInGroup(typeof(ProjectileSpawnerGroup))]
    public partial struct ProjectileSpawnerDelaySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileSpawnerC>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            SemiSpawner(ref state, ecb);
            AutoSpawner(ref state, ecb);
            BurstSpawner(ref state, ecb);
        }

        [BurstCompile]
        private void SemiSpawner(ref SystemState state, EntityCommandBuffer ecb)
        {

            foreach (var item in SystemAPI.Query<EnabledRefRW<ProjectileSpawnerC>>().WithAll<CooldownC, SemiSpawnC>())
            {
                item.ValueRW = false;
            }

            foreach (var (gun, e) in SystemAPI.Query<ProjectileSpawnerC>().WithNone<CooldownC>().WithAll<SemiSpawnC>().WithEntityAccess())
            {
                ecb.AddComponentAndEnable(e, new CooldownC
                {
                    t = 0,
                    duration = gun.delayBetweenShots,
                });

                ecb.SetComponentEnabled<ExecuteProjectileSpawn>(e, true);


            }
        }

        [BurstCompile]
        private void AutoSpawner(ref SystemState state, EntityCommandBuffer ecb)
        {

            foreach (var (gun, e) in SystemAPI.Query<ProjectileSpawnerC>().WithNone<CooldownC>().WithAll<AutoSpawnC>().WithEntityAccess())
            {
                ecb.AddComponentAndEnable(e, new CooldownC
                {
                    t = 0,
                    duration = gun.delayBetweenShots,
                });

                ecb.SetComponentEnabled<ExecuteProjectileSpawn>(e, true);

            }
        }

        [BurstCompile]
        private void BurstSpawner(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var (gun, burstSpawnRef, e) in
                SystemAPI.Query<ProjectileSpawnerC, RefRW<BurstSpawnC>>().WithNone<CooldownC>().WithAll<BurstSpawnC>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<CooldownC>(burstSpawnRef.ValueRO.cooldownDelayE))
                    continue;

                ecb.AddComponentAndEnable<CooldownC>(e, new CooldownC
                {
                    duration = burstSpawnRef.ValueRO.delays
                });

                burstSpawnRef.ValueRW.currentProduceCount++;

                ecb.SetComponentEnabled<ExecuteProjectileSpawn>(e, true);


                if (burstSpawnRef.ValueRW.currentProduceCount >= burstSpawnRef.ValueRW.produceTimes)
                {
                    burstSpawnRef.ValueRW.currentProduceCount = 0;
                    ecb.AddComponentAndEnable(e, new CooldownC
                    {
                        t = 0,
                        duration = gun.delayBetweenShots,
                    });
                }
            }
        }


    }
}
