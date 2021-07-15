using Svelto.ECS.Example.Survive.HUD;
using Svelto.ECS.Hybrid;
using Svelto.ECS.Example.Survive.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Svelto.ECS.Example.Survive.Implementors.HUD
{
    public class EnemyCountImplementor : MonoBehaviour, IImplementor, ICurrentEnemyCountComponent
    {
        public int CurrentEnemyCount
        {
            get => _CurrentEnemyCount;
            set
            {
                _CurrentEnemyCount = value;
                _text.text = "score: " + _CurrentEnemyCount;
            }
        }

        private void Awake()
        {
            // Set up the reference.
            _text = GetComponent<Text>();

            // Reset the score.
            _CurrentEnemyCount = 0;
        }

        private int _CurrentEnemyCount;
        private Text _text;
    }
}