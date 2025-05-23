using Simulation;

namespace Abacus
{
    public class SimpleProgram : Program
    {
        private double time;

        public SimpleProgram(Simulator simulator) : base(simulator)
        {
            time = 0;
        }

        public override bool Update(Simulator simulator, double deltaTime)
        {
            time += deltaTime;
            if (time >= 5f)
            {
                return false;
            }

            return true;
        }

        public override void Dispose()
        {
        }
    }
}