using Brio.Capabilities.Camera;
using Brio.Entities.Core;
using Dalamud.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Camera;

internal class CameraEntity : Entity
{
    public override string FriendlyName => "Camera";

    public override FontAwesomeIcon Icon => FontAwesomeIcon.Camera;

    public CameraEntity(IServiceProvider serviceProvider) : base("camera", serviceProvider)
    {
    }

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<CameraCapability>(_serviceProvider, this));

        base.OnAttached();
    }
}
