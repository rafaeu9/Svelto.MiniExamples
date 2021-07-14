using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Svelto.Common;
using Svelto.ECS.Example.Survive.Enemies;

namespace Svelto.ECS.Example.Survive.HUD
{
    [Sequenced(nameof(EnginesNames.UpdateEnemyCountEngine))]
    public class UpdateEnemyCountEngine : IQueryingEntitiesEngine, IStepEngine
    {
        public EntitiesDB entitiesDB { get; set; }

        public void Ready()
        {
            _listenForEnemyDeath = ListenForEnemyDeath();
        }

        public void Step()
        {
            _listenForEnemyDeath.MoveNext();
        }

        public string name => nameof(UpdateEnemyCountEngine);

        private IEnumerator ListenForEnemyDeath()
        {
            var consumer = _consumerFactory.GenerateConsumer<DeathComponent>("CountEngine", 1);

            while (entitiesDB.HasAny<HUDEntityViewComponent>(ECSGroups.GUICanvas) == false)
                yield return null;

            var hudEntityView = entitiesDB.QueryUniqueEntity<HUDEntityViewComponent>(ECSGroups.GUICanvas);

            while (true)
            {
                //hudEntityView.countComponent.count =

                while (consumer.TryDequeue(out _, out var egid))
                {
                    if (egid.groupID.FoundIn(AliveEnemies.Groups))
                    {
                        //entitiesDB.QueryEntitiesAndIndex<EnemyCount>(egid, out var index);

                        //hudEntityView.countComponent.count += playerTargets[index].scoreValue;
                    }
                }

                yield return null;
            }
        }

        public UpdateEnemyCountEngine(IEntityStreamConsumerFactory consumerFactory)
        {
            _consumerFactory = consumerFactory;
        }

        private readonly IEntityStreamConsumerFactory _consumerFactory;
        private IEnumerator _listenForEnemyDeath;
    }
}