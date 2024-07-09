namespace Brio.Game.Actor.Extensions;

using Dalamud.Game.ClientState.Objects.Types;
using global::Brio.Game.Actor.Appearance;
using global::Brio.Game.Actor.Interop;
using global::Brio.Game.Posing;
using global::Brio.Game.Types;
using global::Brio.Resources;
using global::Brio.Resources.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using StrictsDrawObjectData = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawObjectData;
using StructsBattleCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;
using StructsCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using StructsCharacterBase = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase;
using StructsDrawDataContainer = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;

internal static class CharacterExtensions
{
    public unsafe static StructsCharacter* Native(this ICharacter go)
    {
        return (StructsCharacter*)go.Address;
    }

    public unsafe static BrioDrawData* BrioDrawData(this ICharacter go)
    {
        return (BrioDrawData*)&go.Native()->DrawData;
    }

    public unsafe static bool HasCompanionSlot(this ICharacter go)
    {
        var native = go.Native();
        return native->CompanionObject != null;
    }

    public unsafe static bool HasSpawnedCompanion(this ICharacter go)
    {
        var native = go.Native();
        return native->CompanionObject != null &&
            (
            native->OrnamentData.OrnamentObject != null ||
            native->Mount.MountObject != null ||
            native->CompanionData.CompanionObject != null
            );
    }

    public unsafe static bool CalculateCompanionInfo(this ICharacter go, out CompanionContainer container)
    {
        container = GetCompanionInfo(go);
        return container.Kind != CompanionKind.None;
    }

    public unsafe static CompanionContainer GetCompanionInfo(this ICharacter go)
    {
        var native = go.Native();
        if(native->CompanionObject != null)
        {
            if(native->OrnamentData.OrnamentObject != null)
            {
                return new(CompanionKind.Ornament, native->OrnamentData.OrnamentId);
            }
            else if(native->Mount.MountObject != null)
            {
                return new(CompanionKind.Mount, native->Mount.MountId);
            }
            else if(native->CompanionData.CompanionObject != null)
            {
                return new(CompanionKind.Companion, (ushort)native->CompanionData.CompanionObject->Character.GameObject.BaseId);
            }
        }

        return new(CompanionKind.None, 0);
    }

    public unsafe static StructsBattleCharacter* Native(this IBattleChara go)
    {
        return (StructsBattleCharacter*)go.Address;
    }

    public static unsafe StrictsDrawObjectData* GetWeaponDrawObjectData(this ICharacter go, ActorEquipSlot slot)
    {
        StructsDrawDataContainer.WeaponSlot? weaponSlot = slot switch
        {
            ActorEquipSlot.MainHand => StructsDrawDataContainer.WeaponSlot.MainHand,
            ActorEquipSlot.OffHand => StructsDrawDataContainer.WeaponSlot.OffHand,
            ActorEquipSlot.Prop => StructsDrawDataContainer.WeaponSlot.Unk,
            _ => throw new Exception("Invalid weapon slot")
        };

        if(!weaponSlot.HasValue)
            return null;

        var drawData = &go.Native()->DrawData;

        fixed(StrictsDrawObjectData* drawObjData = &drawData->Weapon(weaponSlot.Value))
        {
            StrictsDrawObjectData* drawObjectData = drawObjData;
            return drawObjectData;
        }
    }

    public static unsafe BrioCharacterBase* GetCharacterBase(this ICharacter go) => go.GetDrawObject<BrioCharacterBase>();

    public unsafe class CharacterBaseInfo
    {
        public BrioCharacterBase* CharacterBase;
        public PoseInfoSlot Slot;
    }

    public static unsafe IReadOnlyList<CharacterBaseInfo> GetCharacterBases(this ICharacter go)
    {
        var list = new List<CharacterBaseInfo>();
        var charaBase = go.GetCharacterBase();

        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.Character });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.MainHand);
        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.MainHand });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.OffHand);
        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.OffHand });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.Prop);
        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.Prop });

        return list;
    }

    public static unsafe BrioCharacterBase* GetWeaponCharacterBase(this ICharacter go, ActorEquipSlot slot)
    {
        var weaponDrawData = go.GetWeaponDrawObjectData(slot);
        if(weaponDrawData != null)
        {
            return (BrioCharacterBase*)weaponDrawData->DrawObject;
        }

        return null;
    }

    public static unsafe BrioHuman* GetHuman(this ICharacter go)
    {
        var charaBase = go.GetCharacterBase();
        if(charaBase == null)
            return null;

        if(charaBase->CharacterBase.GetModelType() != StructsCharacterBase.ModelType.Human)
            return null;

        return (BrioHuman*)charaBase;
    }

    public static unsafe BrioHuman.ShaderParams* GetShaderParams(this ICharacter go)
    {
        var human = go.GetHuman();
        if(human != null && human->Shaders != null && human->Shaders->Params != null)
        {

            return human->Shaders->Params;
        }
        return null;
    }

    public static unsafe BrioCharaMakeType? GetCharaMakeType(this ICharacter go)
    {
        var drawData = go.Native()->DrawData;
        return GameDataProvider.Instance.CharaMakeTypes.Select(x => x.Value).FirstOrDefault(x => x.Race.Row == (uint)drawData.CustomizeData.Race && x.Tribe.Row == (uint)drawData.CustomizeData.Tribe && x.Gender == (Genders)drawData.CustomizeData.Sex);
    }
}
