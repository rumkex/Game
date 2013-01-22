using ComponentKit.Model;

namespace Demo.Scripting
{
    public class PlayerStateComponent: Component
    {
        public bool CanHitWall { get; set; }

        public bool CanGetOver { get; set; }

        public bool CanPush { get; set; }

        public bool CanClimb { get; set; }
    }
}
