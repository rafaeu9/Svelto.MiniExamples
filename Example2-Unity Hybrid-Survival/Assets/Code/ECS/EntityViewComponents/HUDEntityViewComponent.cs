using Svelto.ECS.Hybrid;

namespace Svelto.ECS.Example.Survive.HUD
{
    public struct HUDEntityViewComponent : IEntityViewComponent
    {
        public IAnimationComponent HUDAnimator;
        public IDamageHUDComponent damageImageComponent;
        public IHealthSliderComponent healthSliderComponent;
        public IScoreComponent scoreComponent;
        public IWaveComponent waveComponent;
        public ICurrentEnemyCountComponent currentEnemyComponent;

        public EGID ID { get; set; }
    }
}