using Abacus.Manager.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Abacus.Manager
{
    public static class CommandsRegistry
    {
        private static readonly Dictionary<string, ICommand> commands = new();

        public static IReadOnlyCollection<ICommand> Commands => commands.Values;

        public static void RegisterAll()
        {
            //todo: have a source generator perform this
            Register<Help>();
            Register<Quit>();
            Register<Test>();
            Register<List>();
            Register<Build>();
            Register<Clean>();
            Register<Commit>();
            Register<Fetch>();
            Register<Tag>();
            Register<Generate>();
            Register<Wait>();
            Register<Clear>();
            Register<Push>();
            Register<UML>();
            Register<UpdateOrganizationName>();
        }

        public static void Register<T>() where T : unmanaged, ICommand
        {
            T command = new();
            commands.Add(command.Name.ToString(), command);
        }

        public static ICommand Get(ReadOnlySpan<char> name)
        {
            string nameString = name.ToString();
            if (commands.TryGetValue(nameString, out ICommand? command))
            {
                return command;
            }
            else throw new($"Command with name `{name}` not found");
        }

        public static bool TryGet(ReadOnlySpan<char> name, [NotNullWhen(true)] out ICommand? command)
        {
            string nameString = name.ToString();
            return commands.TryGetValue(nameString, out command);
        }
    }
}