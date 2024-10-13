using Abacus;
using Programs;
using Simulation;
using System;

namespace AbacusSimulator
{
    public static class EntryPoint
    {
        private static int Main(string[] args)
        {
            World gameWorld = new();
            AbacusSimulator simulator = new();
            simulator.Start(gameWorld);
            Program game = Program.Create<ControlsTest>(gameWorld);

            uint gameReturnCode;
            DateTime lastTime = DateTime.UtcNow;
            do
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan delta = now - lastTime;
                lastTime = now;

                simulator.Update(gameWorld, delta);
                gameReturnCode = game.Update(delta);
            }
            while (gameReturnCode != default);

            game.Dispose();
            simulator.Dispose(gameWorld);
            gameWorld.Dispose();

            return (int)gameReturnCode;
        }
    }
}