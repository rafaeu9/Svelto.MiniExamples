namespace Svelto.ECS.Example.Survive.HUD
{
    public interface IWaveComponent
    {
        public int WaveCount { set; get; }
    }

    public interface ICurrentEnemyCountComponent
    {
        public int CurrentEnemyCount { set; get; }
    }
}