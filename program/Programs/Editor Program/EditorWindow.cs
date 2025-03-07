using Cameras;
using Rendering;
using System;
using System.Numerics;
using Transforms;
using UI;
using Unmanaged;
using Windows;
using Worlds;

namespace Editor
{
    public readonly struct EditorWindow<T> : IEntity, IEquatable<EditorWindow<T>> where T : unmanaged, IVirtualWindow
    {
        private readonly Entity entity;

        public unsafe EditorWindow(World world, Settings settings, Vector2 position, Vector2 size, Layer layer)
        {
            LayerMask layerMask = new(layer);

            ASCIIText256 title = default(T).Title;
            Window window = new(world, title, position, size, "vulkan", new(&CloseFunction.OnWindowClosed));
            window.ClearColor = new(0.5f, 0.5f, 0.5f, 1);
            window.IsResizable = true;

            Camera camera = Camera.CreateOrthographic(world, window, 1f, layerMask);
            camera.SetParent(window);

            Canvas canvas = new(settings, camera, layerMask, layerMask);
            canvas.SetParent(window);

            Transform container = new(world);
            container.SetParent(canvas);

            rint canvasReference = window.AddReference(canvas);
            rint containerReference = window.AddReference(container);
            window.AddComponent(new IsEditorWindow(canvasReference, containerReference));
            entity = window;

            default(T).OnCreated(container, canvas);
        }

        readonly void IEntity.Describe(ref Archetype archetype)
        {
            archetype.AddComponentType<IsEditorWindow>();
            archetype.Add<Window>();
        }

        public readonly void Dispose()
        {
            entity.Dispose();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is EditorWindow<T> window && Equals(window);
        }

        public readonly bool Equals(EditorWindow<T> other)
        {
            return entity.Equals(other.entity);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(entity);
        }

        public static bool operator ==(EditorWindow<T> left, EditorWindow<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EditorWindow<T> left, EditorWindow<T> right)
        {
            return !(left == right);
        }
    }
}