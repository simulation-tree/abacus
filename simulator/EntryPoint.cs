using Abacus;
using Programs;
using System;

namespace AbacusSimulator
{
    public static class EntryPoint
    {
        private static int Main(string[] args)
        {
            using (AbacusSimulator simulator = new())
            {
                using (Program program = Program.Create<ControlsTest>(simulator.world))
                {
                    uint returnCode = 0;
                    do
                    {
                        TimeSpan delta = simulator.Update();
                        returnCode = program.Update(delta);
                    }
                    while (returnCode != default);
                    return (int)returnCode;
                }
            }
        }
    }
}