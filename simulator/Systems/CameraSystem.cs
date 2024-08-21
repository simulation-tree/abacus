using Rendering;
using Rendering.Components;
using Rendering.Events;
using Simulation;
using System;
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
        foreach (var x in cameraQuery)
        {
            eint cameraEntity = x.entity;
            Camera camera = new(world, cameraEntity);

            //todo: should have methods that let user to switch camera from projection to ortho and back
            ref CameraProjection projection = ref world.TryGetComponentRef<CameraProjection>(cameraEntity, out bool has);
            if (!has)
            {
                projection = ref world.AddComponentRef<CameraProjection>(cameraEntity);
            }

            CalculateProjection(camera, ref projection);
        }
    }

    private void CalculateProjection(Camera camera, ref CameraProjection component)
    {
        //destination may be gone if a window is destroyed
        Destination destination = camera.Destination;
        if (!world.ContainsEntity(destination)) return;

        Entity cameraEntity = camera;
        Transform cameraTransform = cameraEntity.Become<Transform>();
        Vector3 position = cameraTransform.Position;
        Quaternion rotation = cameraTransform.Rotation;
        Matrix4x4 projection = Matrix4x4.Identity;
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, rotation);
        Vector3 target = position + forward;
        Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, up);

        if (cameraEntity.TryGetComponent(out CameraOrthographicSize orthographicSize))
        {
            if (cameraEntity.ContainsComponent<CameraFieldOfView>())
            {
                throw new InvalidOperationException($"Camera cannot have both {nameof(CameraOrthographicSize)} and {nameof(CameraFieldOfView)} components");
            }

            (uint width, uint height) = destination.DestinationSize;
            (float min, float max) = camera.Depth;
            projection = Matrix4x4.CreateOrthographic(orthographicSize.value * width, orthographicSize.value * height, min, max);
        }
        else if (cameraEntity.TryGetComponent(out CameraFieldOfView fov))
        {
            if (cameraEntity.ContainsComponent<CameraOrthographicSize>())
            {
                throw new InvalidOperationException($"Camera cannot have both {nameof(CameraOrthographicSize)} and {nameof(CameraFieldOfView)} components");
            }

            float aspect = destination.AspectRatio;
            (float min, float max) = camera.Depth;
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