using Abacus;
using Automations.Events;
using Automations.Systems;
using Programs;
using Simulation;
using System;

public class AutomationSimulator
{
    public int Run()
    {
        uint returnCode;
        using (World world = new())
        {
            AutomationPlayingSystem automations = new(world);
            StateMachineSystem stateMachines = new(world);

            //play the simulation
            using (Program program = Program.Create<SimpleProgram>(world))
            {
                DateTime time = DateTime.UtcNow;
                TimeSpan delta = TimeSpan.Zero;
                do
                {
                    world.Submit(new AutomationUpdate(delta));
                    world.Poll();

                    DateTime now = DateTime.UtcNow;
                    delta = now - time;
                    time = now;
                    returnCode = program.Update(delta);
                }
                while (returnCode == 0);
            }

            //finish
            stateMachines.Dispose();
            automations.Dispose();
        }

        return (int)returnCode;
    }
}
