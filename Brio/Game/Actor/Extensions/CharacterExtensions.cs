namespace Brio.Game.Actor.Extensions;

using StructsCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using StructsBattleCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;
using StructsDrawDataContainer = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;
using StructsCharacterBase = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase;
using StrictsDrawObjectData = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawObjectData;


using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Linq;
using global::Brio.Game.Types;
using global::Brio.Game.Actor.Appearance;
using global::Brio.Game.Actor.Interop;
using global::Brio.Resources.Sheets;
using global::Brio.Resources;
using System.Collections.Generic;
using global::Brio.Game.Posing;

internal static class CharacterExtensions
{
    public unsafe static StructsCharacter* Native(this Character go)
    {
        return (StructsCharacter*)go.Address;
    }

    public unsafe static bool HasCompanionSlot(this Character go)
    {
        var native = go.Native();
        return native->CompanionObject != null;
    }

    public unsafe static bool HasSpawnedCompanion(this Character go)
    {
        var native = go.Native();
        return native->CompanionObject != null &&
            (
            native->Ornament.OrnamentObject != null ||
            native->Mount.MountObject != null ||
            native->Companion.CompanionObject != null
            );
    }

    public unsafe static bool CalculateCompanionInfo(this Character go, out CompanionContainer container)
    {
        container = GetCompanionInfo(go);
        return container.Kind != CompanionKind.None;
    }

    public unsafe static CompanionContainer GetCompanionInfo(this Character go)
    {
        var native = go.Native();
        if (native->CompanionObject != null)
        {
            if (native->Ornament.OrnamentObject != null)
            {
                return new(CompanionKind.Ornament, native->Ornament.OrnamentId);
            }
            else if (native->Mount.MountObject != null)
            {
                return new(CompanionKind.Mount, native->Mount.MountId);
            }
            else if (native->Companion.CompanionObject != null)
            {
                return new(CompanionKind.Companion, (ushort)native->Companion.CompanionObject->Character.GameObject.DataID);
            }
        }

        return new(CompanionKind.None, 0);
    }

    public unsafe static StructsBattleCharacter* Native(this BattleChara go)
    {
        return (StructsBattleCharacter*)go.Address;
    }

    public static unsafe StrictsDrawObjectData* GetWeaponDrawObjectData(this Character go, ActorEquipSlot slot)
    {
        StructsDrawDataContainer.WeaponSlot? weaponSlot = slot switch
        {
            ActorEquipSlot.MainHand => StructsDrawDataContainer.WeaponSlot.MainHand,
            ActorEquipSlot.OffHand => StructsDrawDataContainer.WeaponSlot.OffHand,
            ActorEquipSlot.Prop => StructsDrawDataContainer.WeaponSlot.Unk,
            _ => throw new Exception("Invalid weapon slot")
        };

        if (!weaponSlot.HasValue)
            return null;

        var drawData = &go.Native()->DrawData;

        fixed (StrictsDrawObjectData* drawObjData = &drawData->Weapon(weaponSlot.Value))
        {
            StrictsDrawObjectData* drawObjectData = drawObjData;
            return drawObjectData;
        }
    }

    public static unsafe BrioCharacterBase* GetCharacterBase(this Character go) => go.GetDrawObject<BrioCharacterBase>();

    public unsafe class CharacterBaseInfo
    {
        public BrioCharacterBase* CharacterBase;
        public PoseInfoSlot Slot;
    }

    public static unsafe IReadOnlyList<CharacterBaseInfo> GetCharacterBases(this Character go)
    {
        var list = new List<CharacterBaseInfo>();
        var charaBase = go.GetCharacterBase();

        if (charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.Character });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.MainHand);
        if (charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.MainHand });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.OffHand);
        if (charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.OffHand });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.Prop);
        if (charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.Prop });

        return list;
    }

    public static unsafe BrioCharacterBase* GetWeaponCharacterBase(this Character go, ActorEquipSlot slot)
    {
        var weaponDrawData = go.GetWeaponDrawObjectData(slot);
        if (weaponDrawData != null)
        {
            return (BrioCharacterBase*)weaponDrawData->DrawObject;
        }

        return null;
    }

    public static unsafe BrioHuman* GetHuman(this Character go)
    {
        var charaBase = go.GetCharacterBase();
        if (charaBase == null)
            return null;

        if (charaBase->CharacterBase.GetModelType() != StructsCharacterBase.ModelType.Human)
            return null;

        return (BrioHuman*)charaBase;
    }

    public static unsafe BrioHuman.ShaderParams* GetShaderParams(this Character go)
    {
        var human = go.GetHuman();
        if (human != null && human->Shaders != null && human->Shaders->Params != null)
        {
            return human->Shaders->Params;
        }
        return null;
    }

    public static unsafe BrioCharaMakeType? GetCharaMakeType(this Character go)
    {
        var drawData = go.Native()->DrawData;
        return GameDataProvider.Instance.CharaMakeTypes.Select(x => x.Value).FirstOrDefault(x => x.Race.Row == (uint)drawData.CustomizeData.Race && x.Tribe.Row == (uint)drawData.CustomizeData.Clan && x.Gender == (Genders)drawData.CustomizeData.Sex);
    }
}