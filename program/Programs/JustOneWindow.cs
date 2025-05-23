using Simulation;
using Windows;
using Worlds;

namespace Abacus
{
    public class JustOneWindow : Program
    {
        private readonly Window window;

        public JustOneWindow(Simulator simulator) : base(simulator)
        {
            window = new(world, "Just One Window", new(200, 200), new(900, 720), "vulkan");
            window.IsResizable = true;
        }

        public override bool Update(Simulator simulator, double deltaTime)
        {
            if (!IsAnyWindowOpen(world))
            {
                return false;
            }

            return true;
        }

        public override void Dispose()
        {
            if (!window.IsDestroyed)
            {
                window.Dispose();
            }
        }

        private static bool IsAnyWindowOpen(World world)
        {
            return world.CountEntities<Window>() > 0;
        }
    }
}