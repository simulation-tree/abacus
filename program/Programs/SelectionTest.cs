using Cameras;
using Simulation;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using UI;
using Windows;
using Worlds;

namespace Abacus
{
    public class SelectionTest : Program
    {
        private readonly Window window;

        public unsafe SelectionTest(Simulator simulator) : base(simulator)
        {
            window = new(world, "Selection Test", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.IsResizable = true;

            Settings settings = new(world);
            Camera camera = Camera.CreateOrthographic(world, window, 1f);
            Canvas canvas = new(settings, camera);

            Button buttonA = new(new(&Pressed), canvas);
            buttonA.Position = new(100, 60);
            buttonA.Size = new(32, 32);
            buttonA.Color = new(1, 0, 0, 1);

            Button buttonB = new(new(&Pressed), canvas);
            buttonB.Position = new(200, 60);
            buttonB.Size = new(32, 32);
            buttonB.Color = new(0, 1, 0, 1);

            Button buttonC = new(new(&Pressed), canvas);
            buttonC.Position = new(200 + 16, 60 + 16);
            buttonC.Size = new(32, 32);
            buttonC.Color = new Vector4(0, 0, 1, 0.5f);

            [UnmanagedCallersOnly]
            static void Pressed(Entity buttonEntity)
            {
                Trace.WriteLine($"Button {buttonEntity} pressed");
            }
        }

        public override bool Update(Simulator simulator, double deltaTime)
        {
            if (!IsAnyWindowOpen(world))
            {
                return false;
            }

            SharedFunctions.UpdateUISettings(world);
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

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }
    }
}