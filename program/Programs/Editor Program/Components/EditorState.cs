using Unmanaged;
using Worlds;

namespace Editor
{
    public struct EditorState
    {
        public World editingWorld;
        public bool loaded;

        public EditorState(World editingWorld)
        {
            this.editingWorld = editingWorld;
        }

        public void LoadWorld(World loadedWorld)
        {
            editingWorld.Dispose();
            editingWorld = new(loadedWorld.Schema.Clone());
            editingWorld.Append(loadedWorld);
            loaded = true;
        }

        public void Reset()
        {
            editingWorld.Clear();
            loaded = false;
        }
    }
}