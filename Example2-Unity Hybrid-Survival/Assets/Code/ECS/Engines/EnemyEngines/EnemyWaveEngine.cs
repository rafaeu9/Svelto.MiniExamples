using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Svelto.ECS.Example.Survive.Enemies
{
    public class EnemyWaveEngine : IQueryingEntitiesEngine, IReactOnSwap<EnemyEntityViewComponent>, IStepEngine
    {
        private const int NUMBER_OF_ENEMIES_TO_SPAWN = 2;

        public EnemyWaveEngine(EnemyFactory enemyFactory, IEntityFunctions entityFunctions)
        {
            _entityFunctions = entityFunctions;
            _enemyFactory = enemyFactory;
        }

        public EntitiesDB entitiesDB { private get; set; }

        public void Ready()
        {
            _intervaledTick = IntervaledTick();
        }

        public void Step()
        {
            _intervaledTick.MoveNext();
        }

        public string name => nameof(EnemySpawnerEngine);

        public void MovedTo(ref EnemyEntityViewComponent entityComponent, ExclusiveGroupStruct previousGroup, EGID egid)
        {
            //is the enemy dead?
            if (egid.groupID.FoundIn(DeadEnemies.Groups))
            {
                _numberOfEnemyToSpawn--;
            }
        }

        private IEnumerator IntervaledTick()
        {
            IEnumerator<JSonEnemySpawnData[]> enemiestoSpawnJsons = ReadEnemySpawningDataServiceRequest();
            IEnumerator<JSonEnemyAttackData[]> enemyAttackDataJsons = ReadEnemyAttackDataServiceRequest();

            WaveData currentWave = new WaveData();

            while (enemiestoSpawnJsons.MoveNext())
                yield return null;
            while (enemyAttackDataJsons.MoveNext())
                yield return null;

            var enemiestoSpawn = enemiestoSpawnJsons.Current;
            var enemyAttackData = enemyAttackDataJsons.Current;

            var spawningTimes = new float[enemiestoSpawn.Length];

            for (var i = enemiestoSpawn.Length - 1; i >= 0 && _numberOfEnemyToSpawn > 0; --i)
                spawningTimes[i] = enemiestoSpawn[i].enemySpawnData.spawnTime;

            while (true)
            {
                //Wait until next wave
                while (_numberOfEnemyToSpawn > 0)
                    yield return null;

                if (_numberOfEnemyToSpawn <= 0)
                {
                    currentWave.WaveNumber++;
                    _numberOfEnemyToSpawn = currentWave.WaveNumber;

                    //Create big enemy
                    if (currentWave.WaveNumber % 3 == 0)
                    {
                        var spawnData = enemiestoSpawn[2];

                        var EnemyAttackComponent = new EnemyAttackComponent
                        {
                            attackDamage = enemyAttackData[2].enemyAttackData.attackDamage
                          ,
                            timeBetweenAttack = enemyAttackData[2].enemyAttackData.timeBetweenAttacks
                        };

                        var build = _enemyFactory.Build(spawnData.enemySpawnData, EnemyAttackComponent);
                        while (build.MoveNext())
                            yield return null;
                    }

                    for (var i = 0; _numberOfEnemyToSpawn > i; ++i)
                    {
                        var EnemyAttackComponent = new EnemyAttackComponent
                        {
                            attackDamage = enemyAttackData[0].enemyAttackData.attackDamage
                          ,
                            timeBetweenAttack = enemyAttackData[0].enemyAttackData.timeBetweenAttacks
                        };

                        var spawnData = enemiestoSpawn[0];

                        var build = _enemyFactory.Build(spawnData.enemySpawnData, EnemyAttackComponent);
                        while (build.MoveNext())
                            yield return null;

                        //var fromGroupId = ECSGroups.EnemiesToRecycleGroups + (uint)spawnData.enemySpawnData.targetType;
                        //I was having a problem with polling!!!!
                        //if (entitiesDB.HasAny<EnemyEntityViewComponent>(fromGroupId))
                        //    ReuseEnemy(fromGroupId, spawnData);
                        //else
                        //{
                        //}
                    }
                }
            }
        }

        /// <summary>
        ///     Reset all the component values when an Enemy is ready to be recycled.
        ///     it's important to not forget to reset all the states.
        ///     note that the only reason why we pool it the entities here is to reuse the implementors,
        ///     pure entity structs entities do not need pool and can be just recreated
        /// </summary>
        /// <param name="spawnData"></param>
        /// <returns></returns>
        private void ReuseEnemy(ExclusiveGroupStruct fromGroupId, JSonEnemySpawnData spawnData)
        {
            Svelto.Console.LogDebug("reuse enemy " + spawnData.enemySpawnData.enemyPrefab);

            var (healths, enemyViews, count) =
                entitiesDB.QueryEntities<HealthComponent, EnemyEntityViewComponent>(fromGroupId);

            if (count > 0)
            {
                healths[0].currentHealth = 100;

                var spawnInfo = spawnData.enemySpawnData.spawnPoint;

                enemyViews[0].transformComponent.position = spawnInfo;
                enemyViews[0].movementComponent.navMeshEnabled = true;
                enemyViews[0].movementComponent.setCapsuleAsTrigger = false;
                enemyViews[0].layerComponent.layer = GAME_LAYERS.ENEMY_LAYER;
                enemyViews[0].animationComponent.reset = true;

                _entityFunctions.SwapEntityGroup<EnemyEntityDescriptor>(enemyViews[0].ID, AliveEnemies.BuildGroup);
            }
        }

        private static IEnumerator<JSonEnemySpawnData[]> ReadEnemySpawningDataServiceRequest()
        {
            var json = Addressables.LoadAssetAsync<TextAsset>("EnemySpawningData");

            while (json.IsDone == false)
                yield return null;

            var enemiestoSpawn = JsonHelper.getJsonArray<JSonEnemySpawnData>(json.Result.text);

            yield return enemiestoSpawn;
        }

        private static IEnumerator<JSonEnemyAttackData[]> ReadEnemyAttackDataServiceRequest()
        {
            var json = Addressables.LoadAssetAsync<TextAsset>("EnemyAttackData");

            while (json.IsDone == false)
                yield return null;

            var enemiesAttackData = JsonHelper.getJsonArray<JSonEnemyAttackData>(json.Result.text);

            yield return enemiesAttackData;
        }

        private readonly EnemyFactory _enemyFactory;
        private readonly IEntityFunctions _entityFunctions;

        private int _numberOfEnemyToSpawn;
        private IEnumerator _intervaledTick;
    }
}