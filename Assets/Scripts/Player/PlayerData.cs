namespace Sandbox.Player
{
    [System.Serializable]
    public class PlayerData
    {
        public float[] position = new float[3];
        public HealthComponent savedHealth;
        public PlayerComponent savedPlayer;
        public ScoreComponent savedScore;

    }
}
