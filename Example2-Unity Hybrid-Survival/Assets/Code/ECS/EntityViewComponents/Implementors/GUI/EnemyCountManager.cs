using Svelto.ECS.Example.Survive.HUD;
using Svelto.ECS.Hybrid;
using UnityEngine;
using UnityEngine.UI;

namespace Svelto.ECS.Example.Survive.Implementors.HUD
{
    public class EnemyCountManager : MonoBehaviour, IImplementor, ICountComponent
    {
        public int count
        {
            get => _count;
            set
            {
                _count = value;
                _text.text = "Enemies: " + _count;
            }
        }

        private void Awake()
        {
            // Set up the reference.
            _text = GetComponent<Text>();

            // Reset the score.
            _count = 0;
        }

        private int _count;
        private Text _text;
    }
}