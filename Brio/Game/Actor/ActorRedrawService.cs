using Brio.Core;
using Brio.Game.Core;
using Brio.Game.Render;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Penumbra.Api;
using System.Collections.Generic;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Runtime.InteropServices;
using System;
using DrawObjectObject = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;
using Brio.Config;

namespace Brio.Game.Actor;

public class ActorRedrawService : ServiceBase<ActorRedrawService>
{
    public unsafe bool CanRedraw(GameObject gameObject) => !_redrawsActive.Contains(gameObject.AsNative()->ObjectIndex);

    private List<int> _redrawsActive = new();

    private nint _customizeBuffer;

    public override void Start()
    {
        _customizeBuffer = Marshal.AllocHGlobal(68);
        base.Start();
    }

    public unsafe RedrawResult Redraw(GameObject gameObject, RedrawType redrawType)
    {
        if(!CanRedraw(gameObject))
            return RedrawResult.Failed;

        var raw = gameObject.AsNative();
        var index = raw->ObjectIndex;
        _redrawsActive.Add(index);

        var originalPosition = raw->DrawObject->Object.Position;
        var originalRotation = raw->DrawObject->Object.Rotation;

        // In place
        bool drewInPlace = redrawType.HasFlag(RedrawType.AllowOptimized);

        if(drewInPlace)
        {
            // Can only optimize redraw a character
            if(raw->IsCharacter())
            {
                Character* chara = (Character*)raw;
                CharacterBase* charaBase = (CharacterBase*)raw->DrawObject;
                // Can only optimize redraw a human
                if(charaBase->GetModelType() == CharacterBase.ModelType.Human)
                {
                    // We can't change certain values
                    Human* human = ((Human*)raw->DrawObject);
                    if(human->Race != chara->CustomizeData[0]
                        || human->Sex != chara->CustomizeData[1]
                        || human->BodyType != chara->CustomizeData[2]
                        || human->Clan != chara->CustomizeData[4]
                        || chara->ModelCharaId != 0)
                    {
                        drewInPlace = false;
                    }

                    if(drewInPlace)
                    {
                        // Cutomize and gear
                        Buffer.MemoryCopy(chara->CustomizeData, (void*)_customizeBuffer, 28, 28);
                        Buffer.MemoryCopy(chara->EquipSlotData, (void*)(_customizeBuffer + 28), 40, 40);
                        drewInPlace = ((Human*)raw->DrawObject)->UpdateDrawData((byte*)_customizeBuffer, false);
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
                    chara->DrawData.LoadWeapon(DrawDataContainer.WeaponSlot.MainHand, chara->DrawData.MainHandModel, shouldRedrawWeapon, 0, 0, 0);
                    chara->DrawData.LoadWeapon(DrawDataContainer.WeaponSlot.OffHand, chara->DrawData.OffHandModel, shouldRedrawWeapon, 0, 0, 0);

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

        var wasNpcHack = RenderHookService.Instance.ApplyNPCOverride;
        if(redrawType.HasFlag(RedrawType.ForceAllowNPCAppearance))
            RenderHookService.Instance.ApplyNPCOverride = true;

        // Full redraw
        raw->DisableDraw();
        raw->EnableDraw();

        if(redrawType.HasFlag(RedrawType.ForceAllowNPCAppearance))
            RenderHookService.Instance.ApplyNPCOverride = wasNpcHack;

        // Handle position update
        if(redrawType.HasFlag(RedrawType.PreservePosition))
        {
            Dalamud.Framework.RunUntilSatisfied(() => raw->RenderFlags == 0,
            (_) =>
            {
                raw->DrawObject->Object.Rotation = originalRotation;
                raw->DrawObject->Object.Position = originalPosition;
                _redrawsActive.Remove(index);
                return true;
            }, 50, 3, true);
        }
        else
        {
            _redrawsActive.Remove(index);
        }

        return RedrawResult.Full;
    }

    public override void Dispose()
    {
        Marshal.FreeHGlobal(_customizeBuffer);
        _redrawsActive.Clear();
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
