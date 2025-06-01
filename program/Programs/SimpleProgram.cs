namespace Abacus
{
    public class SimpleProgram : Program
    {
        private double time;

        public SimpleProgram(Application application) : base(application)
        {
            time = 0;
        }

        public override bool Update(double deltaTime)
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