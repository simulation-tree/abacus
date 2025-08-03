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

        /// <summary>
        /// Updates the program forward with the given <paramref name="deltaTime"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the program is continuing to run, <see langword="false"/> if it should stop.</returns>
        public abstract bool Update(double deltaTime);
    }
}