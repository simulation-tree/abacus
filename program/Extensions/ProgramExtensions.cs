using Simulation;

namespace Abacus
{
    public static class ProgramExtensions
    {
        public static bool Update<T>(this T program, Simulator simulator, double deltaTime) where T : Program
        {
            return program.Update(simulator, deltaTime);
        }
    }
}