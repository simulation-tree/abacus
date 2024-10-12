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
            AbacusSimulator simulator = new();
            World gameWorld = new();
            World editorWorld = new();
            simulator.Start(gameWorld);
            simulator.Start(editorWorld);
            Program game = Program.Create<ControlsTest>(gameWorld);
            Program editor = Program.Create<VoxelGame>(editorWorld);
            
            uint gameReturnCode;
            uint editorReturnCode;
            DateTime lastTime = DateTime.UtcNow;
            do
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan delta = now - lastTime;
                lastTime = now;

                simulator.Update(gameWorld, delta);
                gameReturnCode = game.Update(delta);

                simulator.Update(editorWorld, delta);
                editorReturnCode = editor.Update(delta);
            }
            while (gameReturnCode != default && editorReturnCode != default);

            editor.Dispose();
            game.Dispose();
            simulator.Dispose(editorWorld);
            simulator.Dispose(gameWorld);

            return (int)gameReturnCode;
        }
    }
}