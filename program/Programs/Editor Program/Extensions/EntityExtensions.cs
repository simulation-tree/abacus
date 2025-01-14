using System;
using Worlds;

namespace Editor
{
    public static class EntityExtensions
    {
        public static ref EditorState GetEditorState<T>(this T entity) where T : unmanaged, IEntity
        {
            World world = entity.GetWorld();
            if (world.TryGetFirstComponent<EditorState>(out uint editorStateEntity))
            {
                return ref world.GetComponent<EditorState>(editorStateEntity);
            }

            throw new InvalidOperationException("No editor state found");
        }
    }
}