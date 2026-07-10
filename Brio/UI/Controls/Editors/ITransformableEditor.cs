using Brio.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class ITransformableEditor
{
    private Transform? _trackingTransform;
    private Vector3? _trackingEuler;

    public void Draw(string id, ITransformable target, float offset = 0.01f, bool compact = false)
    {
        using(ImRaii.PushId(id))
        using(ImRaii.Disabled(target.IsTransformFrozen))
        {
            var before = target.Transform;
            var realTransform = _trackingTransform ?? before;
            var realEuler = _trackingEuler ?? before.Rotation.ToEuler();

            (var pdidChange, var panyActive) = ImBrio.DragFloat3("###_itransform_position", ref realTransform.Position, offset, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position", enableExpanded: compact);
            ImBrio.VerticalPadding(2);
            (var rdidChange, var ranyActive) = ImBrio.DragFloat3("###_itransform_rotation", ref realEuler, offset * 100f, FontAwesomeIcon.ArrowsSpin, "Rotation", enableExpanded: compact);
            ImBrio.VerticalPadding(2);
            (var sdidChange, var sanyActive) = ImBrio.DragFloat3("###_itransform_scale", ref realTransform.Scale, offset, FontAwesomeIcon.ExpandAlt, "Scale", enableExpanded: compact);
            ImBrio.VerticalPadding(2);

            bool didChange = pdidChange | rdidChange | sdidChange;
            bool anyActive = panyActive | ranyActive | sanyActive;

            realTransform.Rotation = realEuler.ToQuaternion();

            if(didChange)
                target.Transform = realTransform;

            if(anyActive)
            {
                _trackingTransform = realTransform;
                _trackingEuler = realEuler;
            }
            else
            {
                if(_trackingTransform.HasValue || _trackingEuler.HasValue)
                    target.Snapshot();

                _trackingTransform = null;
                _trackingEuler = null;
            }
        }
    }
}
