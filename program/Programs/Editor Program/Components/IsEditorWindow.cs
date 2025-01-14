using Worlds;

namespace Editor
{
    [Component]
    public readonly struct IsEditorWindow
    {
        public readonly rint canvasReference;
        public readonly rint containerReference;

        public IsEditorWindow(rint canvasReference, rint containerReference)
        {
            this.canvasReference = canvasReference;
            this.containerReference = containerReference;
        }
    }
}