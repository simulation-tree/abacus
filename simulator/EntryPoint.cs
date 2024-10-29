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
            using (World world = new())
            {
                using (AbacusSimulator simulator = new(world))
                {
                    using (Program program = Program.Create<VoxelGame>(world))
                    {
                        DateTime lastTime = DateTime.UtcNow;
                        uint returnCode;
                        do
                        {
                            DateTime now = DateTime.UtcNow;
                            TimeSpan delta = now - lastTime;
                            lastTime = now;

                            simulator.Update(delta);
                        }
                        while (!program.IsFinished(out returnCode));
                        return (int)returnCode;
                    }
                }
            }
        }
    }
}