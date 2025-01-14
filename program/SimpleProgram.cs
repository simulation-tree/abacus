using Simulation;
using System;
using Unmanaged;
using Worlds;

namespace Abacus
{
    public partial struct SimpleProgram : IProgram
    {
        private TimeSpan time;

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            time += delta;
            if (time >= TimeSpan.FromSeconds(5f))
            {
                return StatusCode.Success(0);
            }

            return StatusCode.Continue;
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
        }
    }
}