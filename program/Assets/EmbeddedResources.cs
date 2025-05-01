using Data;
using Data.Functions;

namespace Abacus
{
    public readonly struct EmbeddedResources : IEmbeddedResourceBank
    {
        readonly void IEmbeddedResourceBank.Load(Register register)
        {
            register.Invoke("Assets/Cav.world");
            register.Invoke("Assets/Textures/texture.jpg");
            register.Invoke("Assets/Textures/wave.png");
            register.Invoke("Assets/Textures/Blocks/Cobblestone.png");
            register.Invoke("Assets/Textures/Blocks/Dirt.png");
            register.Invoke("Assets/Textures/Blocks/Grass.png");
            register.Invoke("Assets/Textures/Blocks/GrassSide.png");
            register.Invoke("Assets/Textures/Blocks/Stone.png");
            register.Invoke("Assets/Textures/Spaceman/Falling.png");
            register.Invoke("Assets/Textures/Spaceman/Idle.png");
            register.Invoke("Assets/Textures/Spaceman/Idle2.png");
            register.Invoke("Assets/Textures/Spaceman/JumpingUp.png");
            register.Invoke("Assets/Textures/Spaceman/Skid.png");
            register.Invoke("Assets/Textures/Spaceman/Walk.png");
            register.Invoke("Assets/Textures/Spaceman/Walk2.png");
            register.Invoke("Assets/Skyboxes/Clouds/clouds1_down.bmp");
            register.Invoke("Assets/Skyboxes/Clouds/clouds1_east.bmp");
            register.Invoke("Assets/Skyboxes/Clouds/clouds1_north.bmp");
            register.Invoke("Assets/Skyboxes/Clouds/clouds1_south.bmp");
            register.Invoke("Assets/Skyboxes/Clouds/clouds1_up.bmp");
            register.Invoke("Assets/Skyboxes/Clouds/clouds1_west.bmp");
            register.Invoke("Assets/UI/TestUI.json5");
        }
    }
}
