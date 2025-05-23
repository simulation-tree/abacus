using Simulation;
using System;
using Worlds;

namespace Abacus
{
    public abstract class Program : IDisposable
    {
        public readonly Simulator simulator;
        public readonly World world;

        public Program(Simulator simulator)
        {
            this.simulator = simulator;
            world = simulator.world;
        }

        public abstract void Dispose();

        public abstract bool Update(Simulator simulator, double deltaTime);
    }
}