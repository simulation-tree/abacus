using InteractionKit;
using InteractionKit.Functions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Worlds;

namespace Editor
{
    public readonly struct EntityWindow : IVirtualWindow
    {
        FixedString IVirtualWindow.Title => "Entity";
        VirtualWindowClose IVirtualWindow.CloseCallback => default;

        unsafe void IVirtualWindow.OnCreated(Transform container, Canvas canvas)
        {
            Settings settings = canvas.Settings;

            Button newButton = new(new(&PressedReturn), canvas);
            newButton.SetParent(container);
            newButton.Color = new(0.2f, 0.2f, 0.2f, 1);
            newButton.Anchor = Anchor.TopLeft;
            newButton.Pivot = new(0f, 1f, 0f);
            newButton.Size = new(180f, settings.SingleLineHeight);
            newButton.Position = new(4f, -4f);

            Label newButtonLabel = new(canvas, "Return from Entity");
            newButtonLabel.SetParent(newButton);
            newButtonLabel.Anchor = Anchor.TopLeft;
            newButtonLabel.Position = new(4f, -4f);
            newButtonLabel.Pivot = new(0f, 1f, 0f);

            [UnmanagedCallersOnly]
            static void PressedReturn(Entity button)
            {
                Trace.WriteLine("Pressed Return");
                ref EditorState editorState = ref button.GetEditorState();
                editorState.Reset();
            }
        }
    }
}