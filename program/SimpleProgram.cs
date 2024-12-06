using Simulation;
using System;
using Unmanaged;
using Worlds;

namespace Abacus
{
    public partial struct SimpleProgram : IProgram
    {
        private TimeSpan time;

        void IProgram.Initialize(in Simulator simulator, in Allocation allocation, in World world)
        {
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            time += delta;
            if (time >= TimeSpan.FromSeconds(5f))
            {
                return StatusCode.Success(1);
            }

            return StatusCode.Continue;
        }

        void IDisposable.Dispose()
        {
        }
    }
}