using Simulation;
using System;
using Worlds;

namespace Abacus
{
    public class Application : IDisposable
    {
        public readonly Simulator simulator;
        public readonly World world;

        public Application(Schema schema)
        {
            world = new(schema);
            simulator = new();
        }

        public virtual void Dispose()
        {
            simulator.Dispose();
            world.Dispose();
        }

        public void Run(Program program)
        {
            UpdateLoop updateLoop = new();
            double deltaTime;
            do
            {
                deltaTime = updateLoop.GetDeltaTime();
                Update(deltaTime);
            }
            while (program.Update(deltaTime));
        }

        protected virtual void Update(double deltaTime)
        {
        }
    }
}