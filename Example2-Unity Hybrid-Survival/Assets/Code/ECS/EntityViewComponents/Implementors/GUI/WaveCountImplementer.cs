using Svelto.ECS.Example.Survive.HUD;
using Svelto.ECS.Hybrid;
using Svelto.ECS.Example.Survive.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Svelto.ECS.Example.Survive.Implementors.HUD
{
    public class WaveCountImplementer : MonoBehaviour, IImplementor, IWaveComponent
    {
        public int WaveCount
        {
            get => _WaveCount;
            set
            {
                _WaveCount = value;
                _text.text = "score: " + _WaveCount;
            }
        }

        private void Awake()
        {
            // Set up the reference.
            _text = GetComponent<Text>();

            // Reset the score.
            _WaveCount = 0;
        }

        private int _WaveCount;
        private Text _text;
    }
}