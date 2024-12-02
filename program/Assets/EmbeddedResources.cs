using Data;
using System.Collections.Generic;

namespace Abacus
{
    public readonly struct EmbeddedResources : IEmbeddedResources
    {
        readonly IEnumerable<Address> IEmbeddedResources.Addresses
        {
            get
            {
                yield return new Address("Assets/Cav.world");
                yield return new Address("Assets/Textures/texture.jpg");
                yield return new Address("Assets/Textures/wave.png");
                yield return new Address("Assets/Textures/Blocks/Cobblestone.png");
                yield return new Address("Assets/Textures/Blocks/Dirt.png");
                yield return new Address("Assets/Textures/Blocks/Grass.png");
                yield return new Address("Assets/Textures/Blocks/GrassSide.png");
                yield return new Address("Assets/Textures/Blocks/Stone.png");
                yield return new Address("Assets/Textures/Spaceman/Falling.png");
                yield return new Address("Assets/Textures/Spaceman/Idle.png");
                yield return new Address("Assets/Textures/Spaceman/Idle2.png");
                yield return new Address("Assets/Textures/Spaceman/JumpingUp.png");
                yield return new Address("Assets/Textures/Spaceman/Skid.png");
                yield return new Address("Assets/Textures/Spaceman/Walk.png");
                yield return new Address("Assets/Textures/Spaceman/Walk2.png");
            }
        }
    }
}
