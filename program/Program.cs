using Simulation;
using System;
using Worlds;

namespace Abacus
{
    public abstract class Program : IDisposable
    {
        public readonly Simulator simulator;
        public readonly World world;

        public Program(Application application)
        {
            simulator = application.simulator;
            world = application.world;
        }

        public abstract void Dispose();

        public abstract bool Update(double deltaTime);
    }
}