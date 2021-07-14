using Svelto.ECS.Hybrid;

namespace Svelto.ECS.Example.Survive.HUD
{
    public struct HUDEntityViewComponent : IEntityViewComponent
    {
        public IAnimationComponent HUDAnimator;
        public IDamageHUDComponent damageImageComponent;
        public IHealthSliderComponent healthSliderComponent;
        public IScoreComponent scoreComponent;
        public ICountComponent countComponent;
        public EGID ID { get; set; }
    }
}