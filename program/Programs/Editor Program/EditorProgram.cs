using Data;
using UI;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;
using Windows;
using Worlds;
using Collections.Generic;

namespace Editor
{
    public partial struct EditorProgram : IProgram
    {
        private readonly World world;
        private readonly Simulator simulator;
        private readonly Settings settings;
        private readonly Array<Text> args;
        private EditorWindow<LaunchWindow> launcherWindow;
        private EditorWindow<WorldWindow> worldWindow;
        private EditorWindow<EntityWindow> entityWindow;
        private bool? loaded;
        private State state;

        public enum State
        {
            Idle,
            DestroyingWindows,
            CreatingWindows
        }

        private EditorProgram(Simulator simulator, World world, Array<Text> args)
        {
            this.simulator = simulator;
            this.world = world;
            this.args = args;
            settings = new(world);

            uint editorStateEntity = world.CreateEntity();
            ref EditorState editorState = ref world.AddComponent<EditorState>(editorStateEntity);
            editorState.editingWorld = new();

            //try to load a world from the first argument
            if (args.Length > 0)
            {
                Text firstArg = args[0];
                DataRequest request = new(world, firstArg);
                simulator.UpdateSystems(TimeSpan.MinValue, world);
                if (request.TryGetData(out USpan<byte> data))
                {
                    Trace.WriteLine($"Loaded world from `{firstArg}`");

                    using ByteReader reader = new(data);
                    using World loadedWorld = reader.ReadObject<World>();
                    editorState.LoadWorld(loadedWorld);
                }
                else
                {
                    Trace.WriteLine($"Failed to load world from `{firstArg}`");
                }
            }
            else
            {
                Trace.WriteLine("No world to load");
            }
        }

        public EditorProgram(string[] args)
        {
            this.args = new((uint)args.Length);
            for (uint i = 0; i < args.Length; i++)
            {
                this.args[i] = new(args[i]);
            }
        }

        public EditorProgram()
        {
            this.args = new(0);
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
            ref EditorState editorState = ref settings.GetEditorState();
            editorState.editingWorld.Dispose();

            foreach (Text arg in args)
            {
                arg.Dispose();
            }

            args.Dispose();
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            ref EditorProgram program = ref allocation.Read<EditorProgram>();
            program = new(simulator, world, program.args);
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            ref EditorState editorState = ref settings.GetEditorState();
            if (state == State.DestroyingWindows)
            {
                if (!editorState.loaded)
                {
                    if (worldWindow != default)
                    {
                        worldWindow.Dispose();
                        worldWindow = default;
                    }

                    if (entityWindow != default)
                    {
                        entityWindow.Dispose();
                        entityWindow = default;
                    }
                }
                else
                {
                    if (launcherWindow != default)
                    {
                        launcherWindow.Dispose();
                        launcherWindow = default;
                    }
                }

                state = State.CreatingWindows;
            }
            else if (state == State.CreatingWindows)
            {
                if (!editorState.loaded)
                {
                    if (launcherWindow == default)
                    {
                        launcherWindow = new(world, settings, new(200, 200), new(200, 200), 0);
                    }
                }
                else
                {
                    if (worldWindow == default)
                    {
                        worldWindow = new(world, settings, new(0, 200), new(200, 200), 1);
                    }

                    if (entityWindow == default)
                    {
                        entityWindow = new(world, settings, new(400, 200), new(200, 200), 2);
                    }
                }

                state = State.Idle;
            }

            if (loaded != editorState.loaded)
            {
                loaded = editorState.loaded;
                state = State.DestroyingWindows;
            }
            else if (state == State.Idle)
            {
                if (!IsAnyWindowOpen(world))
                {
                    return StatusCode.Success(0);
                }
            }

            SharedFunctions.UpdateUISettings(world);
            return StatusCode.Continue;
        }

        private static bool IsAnyWindowOpen(World world)
        {
            return world.TryGetFirst(out Window _);
        }
    }
}