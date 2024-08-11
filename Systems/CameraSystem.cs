using Rendering;
using Rendering.Components;
using Rendering.Events;
using Simulation;
using System.Numerics;
using Transforms;

public class CameraSystem : SystemBase
{
    private readonly Query<IsCamera> cameraQuery;

    public CameraSystem(World world) : base(world)
    {
        cameraQuery = new(world);
        Subscribe<CameraUpdate>(Update);
    }

    public override void Dispose()
    {
        cameraQuery.Dispose();
        base.Dispose();
    }

    private void Update(CameraUpdate update)
    {
        cameraQuery.Update();
        foreach (Query<IsCamera>.Result result in cameraQuery)
        {
            Camera camera = new(world, result.entity);

            //todo: should have methods that let user to switch camera from projection to ortho and back
            ref CameraProjection projection = ref camera.TryGetComponentRef<Camera, CameraProjection>(out bool has);
            if (!has)
            {
                projection = ref camera.AddComponentRef<Camera, CameraProjection>();
            }

            CalculateProjection(camera, ref projection);
        }
    }

    private void CalculateProjection(Camera camera, ref CameraProjection component)
    {
        //destination may be gone if a window is destroyed
        eint destinationEntity = world.GetComponent<CameraOutput>(camera.GetEntityValue()).destination;
        if (!world.ContainsEntity(destinationEntity)) return;

        Transform cameraTransform = camera.BecomeTransform();
        Vector3 position = cameraTransform.GetPosition();
        Quaternion rotation = cameraTransform.GetRotation();
        Matrix4x4 projection = Matrix4x4.Identity;
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, rotation);
        Vector3 target = position + forward;
        Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, up);

        Destination destination = camera.GetDestination();
        bool isOrthographic = camera.IsOrthographic();
        if (camera.TryGetComponent(out CameraOrthographicSize orthographicSize))
        {
            if (!isOrthographic)
            {
                throw new InvalidOperationException($"Camera cannot have both {nameof(CameraOrthographicSize)} and {nameof(CameraFieldOfView)} components");
            }

            (uint width, uint height) = destination.GetDestinationSize();
            (float min, float max) = camera.GetDepth();
            projection = Matrix4x4.CreateOrthographic(orthographicSize.value * width, orthographicSize.value * height, min, max);
        }
        else if (camera.TryGetComponent(out CameraFieldOfView fov))
        {
            if (isOrthographic)
            {
                throw new InvalidOperationException($"Camera cannot have both {nameof(CameraOrthographicSize)} and {nameof(CameraFieldOfView)} components");
            }

            float aspect = destination.GetAspectRatio();
            (float min, float max) = camera.GetDepth();
            projection = Matrix4x4.CreatePerspectiveFieldOfView(fov.value, aspect, min, max);
            projection.M11 *= -1; //flip x axis
        }
        else
        {
            throw new InvalidOperationException($"Camera does not have either {nameof(CameraOrthographicSize)} or {nameof(CameraFieldOfView)} component");
        }

        component = new(projection, view);
    }
}