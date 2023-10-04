using Brio.Core;
using Brio.Game.Core;
using Brio.Game.Render;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Collections.Generic;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Runtime.InteropServices;
using System;
using Brio.Game.Actor.Extensions;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Brio.Game.Actor;

public class ActorRedrawService : ServiceBase<ActorRedrawService>
{
    public unsafe bool CanRedraw(GameObject gameObject) => !_redrawsActive.Contains(gameObject.AsNative()->ObjectIndex);

    private List<int> _redrawsActive = new();

    private nint _customizeBuffer;

    public ActorRedrawService()
    {
        _customizeBuffer = Marshal.AllocHGlobal(68);
    }

    public unsafe RedrawResult Redraw(GameObject gameObject, RedrawType redrawType)
    {
        if(!CanRedraw(gameObject))
            return RedrawResult.Failed;

        var rawObject = gameObject.AsNative();
        var index = rawObject->ObjectIndex;
        _redrawsActive.Add(index);

        var originalPosition = rawObject->Position;
        var originalRotation = Quaternion.Identity;

        if(rawObject->DrawObject != null)
        {
            originalPosition = rawObject->DrawObject->Object.Position;
            originalRotation = rawObject->DrawObject->Object.Rotation;
        }

        // In place
        bool drewInPlace = redrawType.HasFlag(RedrawType.AllowOptimized);

        if(drewInPlace)
        {
            // Can only optimize redraw a character
            if(rawObject->IsCharacter())
            {
                Character* chara = (Character*)rawObject;
                CharacterBase* charaBase = (CharacterBase*)rawObject->DrawObject;
                // Can only optimize redraw a human
                if(charaBase != null && charaBase->GetModelType() == CharacterBase.ModelType.Human)
                {
                    // We can't change certain values
                    Human* human = ((Human*)rawObject->DrawObject);
                    if(human->Customize.Race != chara->DrawData.CustomizeData[0]
                        || human->Customize.Sex != chara->DrawData.CustomizeData[1]
                        || human->Customize.BodyType != chara->DrawData.CustomizeData[2]
                        || human->Customize.Clan != chara->DrawData.CustomizeData[4]
                        || human->FaceId != chara->DrawData.CustomizeData[5]
                        || chara->CharacterData.ModelCharaId != 0)
                    {
                        drewInPlace = false;
                    }

                    if(drewInPlace)
                    {
                        // Cutomize and gear
                        Buffer.MemoryCopy(&chara->DrawData.CustomizeData, (void*)_customizeBuffer, 28, 28);
                        Buffer.MemoryCopy(&chara->DrawData.Head, (void*)(_customizeBuffer + 28), 40, 40);
                        drewInPlace = ((Human*)rawObject->DrawObject)->UpdateDrawData((byte*)_customizeBuffer, false);
                    }
                }
                else
                {
                    drewInPlace = false;
                }

                if(drewInPlace)
                {
                    // Weapons
                    byte shouldRedrawWeapon = (byte) (redrawType.HasFlag(RedrawType.ForceRedrawWeaponsOnOptimized) ? 1 : 0);
                    chara->DrawData.LoadWeapon(DrawDataContainer.WeaponSlot.MainHand, chara->DrawData.Weapon(DrawDataContainer.WeaponSlot.MainHand).ModelId, shouldRedrawWeapon, 0, 0, 0);
                    chara->DrawData.LoadWeapon(DrawDataContainer.WeaponSlot.OffHand, chara->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).ModelId, shouldRedrawWeapon, 0, 0, 0);

                    _redrawsActive.Remove(index);
                    return RedrawResult.Optmized;
                }
            }
        }

        if(!redrawType.HasFlag(RedrawType.AllowFull))
        {
            _redrawsActive.Remove(index);
            return RedrawResult.Failed;
        }

        if(redrawType.HasFlag(RedrawType.ForceAllowNPCAppearance))
            RenderHookService.Instance.PushForceNpcHack();

        // Full redraw
        rawObject->DisableDraw();
        rawObject->EnableDraw();

        if(redrawType.HasFlag(RedrawType.ForceAllowNPCAppearance))
            RenderHookService.Instance.PopForceNpcHack();

        // Handle position update
        if(redrawType.HasFlag(RedrawType.PreservePosition))
        {
            Dalamud.Framework.RunUntilSatisfied(() => rawObject->RenderFlags == 0 && rawObject->DrawObject != null,
            (success) =>
            {
                _redrawsActive.Remove(index);

                if(success)
                {
                    rawObject->DrawObject->Object.Rotation = originalRotation;
                    rawObject->DrawObject->Object.Position = originalPosition;
                    return true;
                }
                
                return false;
            }, 50, 3, true);
        }
        else
        {
            _redrawsActive.Remove(index);
        }

        return RedrawResult.Full;
    }

    public override void Stop()
    {
        _redrawsActive.Clear();
    }

    public override void Dispose()
    {
        Marshal.FreeHGlobal(_customizeBuffer);
    }
}

[Flags]
public enum RedrawType
{
    None = 0,
    AllowOptimized = 1,
    AllowFull = 2,
    ForceRedrawWeaponsOnOptimized = 4,
    PreservePosition = 8,
    ForceAllowNPCAppearance = 16,

    All = AllowOptimized | AllowFull | ForceRedrawWeaponsOnOptimized | PreservePosition | ForceAllowNPCAppearance
}

public enum RedrawResult
{
    Optmized,
    Full,
    Failed
}
