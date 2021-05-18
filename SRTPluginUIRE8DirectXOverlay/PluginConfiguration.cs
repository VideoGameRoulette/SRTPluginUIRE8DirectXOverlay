namespace SRTPluginUIRE2DirectXOverlay
{
    public class PluginConfiguration
    {
        public bool Debug { get; set; }
        public bool NoInventory { get; set; }
        public float ScalingFactor { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }

        public float EnemyHPPositionX { get; set; }
        public float EnemyHPPositionY { get; set; }

        public PluginConfiguration()
        {
            Debug = false;
            NoInventory = true;
            ScalingFactor = 0.75f;
            PositionX = 5f;
            PositionY = 30f;
            EnemyHPPositionX = -1;
            EnemyHPPositionY = -1;
        }
    }
}
