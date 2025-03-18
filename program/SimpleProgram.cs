using Simulation;
using System;
using Worlds;

namespace Abacus
{
    public partial struct SimpleProgram : IProgram<SimpleProgram>
    {
        private TimeSpan time;

        void IProgram<SimpleProgram>.Start(ref SimpleProgram program, in Simulator simulator, in World world)
        {
        }

        StatusCode IProgram<SimpleProgram>.Update(in TimeSpan delta)
        {
            time += delta;
            if (time >= TimeSpan.FromSeconds(5f))
            {
                return StatusCode.Success(0);
            }

            return StatusCode.Continue;
        }

        void IProgram<SimpleProgram>.Finish(in StatusCode statusCode)
        {
        }
    }
}