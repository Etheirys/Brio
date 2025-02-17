using Brio.Core;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Brio.Game.Types;
using Brio.IPC;
using Brio.Resources;
using Brio.UI.Widgets.Actor;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Threading.Tasks;

namespace Brio.Capabilities.Actor;

public class ActorAppearanceCapability : ActorCharacterCapability
{
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly PenumbraService _penumbraService;
    private readonly CustomizePlusService _customizePlusService;
    private readonly GlamourerService _glamourerService;
    private readonly MareService _mareService;
    private readonly GPoseService _gposeService;
    private readonly IFramework _framework;

    public string CurrentCollection => _penumbraService.GetCollectionForObject(Character);
    public PenumbraService PenumbraService => _penumbraService;

    public bool IsCollectionOverridden => _oldCollection != null;
    private string? _oldCollection = null;


    public string CurrentDesign { get; set; } = "None";
    public GlamourerService GlamourerService => _glamourerService;


    public (string? name, Guid? id) SelectedDesign { get; set; } = ("None", null);
    public (string? data, Guid? id) CurrentProfile => _customizePlusService.GetActiveProfile(Character);
    public CustomizePlusService CustomizePlusService => _customizePlusService;


    private ActorAppearance? _originalAppearance = null;
    public bool IsAppearanceOverridden => _originalAppearance.HasValue;

    public bool HasPenumbraIntegration => _penumbraService.IsAvailable;
    public bool HasGlamourerIntegration => _glamourerService.IsAvailable;
    public bool HasCustomizePlusIntegration => _customizePlusService.IsAvailable;

    public ActorAppearance CurrentAppearance => _actorAppearanceService.GetActorAppearance(Character);

    public ActorAppearance OriginalAppearance => _originalAppearance ?? CurrentAppearance;

    public ModelShaderOverride _modelShaderOverride = new();

    public unsafe bool IsHuman => Character.GetHuman() != null;

    public bool CanTint => _actorAppearanceService.CanTint;

    public bool CanMcdf => _mareService.IsAvailable;

    public bool IsHidden => CurrentAppearance.ExtendedAppearance.Transparency == 0;

    public ActorAppearanceCapability(ActorEntity parent, IFramework framework, ActorAppearanceService actorAppearanceService, CustomizePlusService customizePlusService, PenumbraService penumbraService, GlamourerService glamourerService, MareService mareService, GPoseService gPoseService) : base(parent)
    {
        _actorAppearanceService = actorAppearanceService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _mareService = mareService;
        _gposeService = gPoseService;
        _customizePlusService = customizePlusService;
        _framework = framework;

        Widget = new ActorAppearanceWidget(this);

        _gposeService.OnGPoseStateChange += OnGPoseStateChanged;
        _penumbraService.OnPenumbraRedraw += OnPenumbraRedraw;
    }

    public bool LoadMcdf(string path)
    {
        return _mareService.LoadMcdfAsync(path, GameObject);
    }

    public void SetCollection(Guid collection)
    {
        if(IsCollectionOverridden && collection.ToString().Equals(_oldCollection))
        {
            ResetCollection();
            return;
        }

        var old = _penumbraService.SetCollectionForObject(Character, collection);

        if(!IsCollectionOverridden)
            _oldCollection = old.ToString();

        _ = _actorAppearanceService.Redraw(Character);
    }
    public void ResetCollection()
    {
        if(IsCollectionOverridden)
        {
            _penumbraService.SetCollectionForObject(Character, Guid.Parse(_oldCollection!));
            _oldCollection = null;
            _ = _actorAppearanceService.Redraw(Character);
        }
    }

    public void SetDesign(Guid design)
    {
        _ = _glamourerService.ApplyDesign(design, Character);
    }
    public void ResetDesign(bool isRest = false)
    {
        _glamourerService.RevertCharacter(Character);

        if(isRest == false && _glamourerService.CheckForLock(Character))
        {
            ResetCollection();
            ResetProfile(true);
        }
    }

    public void SetProfile(string data)
    {
        _customizePlusService.SetProfile(Character, data);
    }
    public void ResetProfile(bool isRest = false)
    {
        _customizePlusService.RemoveTemporaryProfile(Character);

        if(isRest == false && _glamourerService.CheckForLock(Character))
        {
            ResetCollection();
            ResetDesign(true);
        }

        SetSelectedProfile();
    }
    public Guid? GetActiveProfile()
    {
        return _customizePlusService.GetActiveProfile(Character).Item2;
    }
    public void SetSelectedProfile()
    {
        var profiles = _customizePlusService.GetProfiles();

        var activeProfile = GetActiveProfile();
        if(activeProfile is not null)
        {
            foreach(var item in profiles)
            {
                if(item.UniqueId == activeProfile.Value)
                {
                    SelectedDesign = (item.Name, activeProfile.Value);
                }
            }
        }
        else
        {
            SelectedDesign = ("None", null);
        }
    }

    public async void SetAppearanceAsTask(ActorAppearance appearance, AppearanceImportOptions options)
        => await SetAppearance(appearance, options);

    public async Task SetAppearance(ActorAppearance appearance, AppearanceImportOptions options)
    {
        Brio.Log.Debug($"Setting appearance for gameobject {GameObject.ObjectIndex}...");

        _originalAppearance ??= _actorAppearanceService.GetActorAppearance(Character);
        _ = await _actorAppearanceService.SetCharacterAppearance(Character, appearance, options);

        if(options.HasFlag(AppearanceImportOptions.Shaders))
        {
            ApplyShaderOverride();
        }
        else
        {
            _modelShaderOverride.Reset();
        }

        if(Entity is ActorEntity actor && actor.IsProp == true)
            await _framework.RunOnTick(() =>
            {
                AttachWeapon();
            }, delayTicks: 5);

        Brio.Log.Debug($"Appearance set for gameobject {GameObject.ObjectIndex}.");
    }

    public void ImportAppearance(string file, AppearanceImportOptions options)
    {
        var doc = ResourceProvider.Instance.GetFileDocument<AnamnesisCharaFile>(file);
        if(doc.Race == 0 && doc.ModelType == 0)
        {
            EventBus.Instance.NotifyError("Invalid character appearance file.");
            return;
        }

        if(doc != null)
            _ = SetAppearance(doc, options);
    }

    public void ExportAppearance(string file)
    {
        AnamnesisCharaFile appearance = CurrentAppearance;
        ResourceProvider.Instance.SaveFileDocument(file, appearance);
    }

    public Task MakeHuman() => SetAppearance(new ActorAppearanceUnion(SpecialAppearances.DefaultHumanEventNpc), AppearanceImportOptions.All);

    public Task RemoveAllEquipment()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        appearance.Equipment = new ActorEquipment();
        appearance.Weapons.MainHand = SpecialAppearances.EmperorsMainHand;
        appearance.Weapons.OffHand = SpecialAppearances.EmperorsOffHand;
        return SetAppearance(appearance, AppearanceImportOptions.Gear);
    }

    public Task ApplySmallclothes()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        appearance.Equipment = ActorEquipment.Smallclothes();
        appearance.Weapons.MainHand = SpecialAppearances.EmperorsMainHand;
        appearance.Weapons.OffHand = SpecialAppearances.EmperorsOffHand;
        return SetAppearance(appearance, AppearanceImportOptions.Gear);
    }

    public Task ToggleHide()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);

        if(IsHidden)
        {
            appearance.ExtendedAppearance.Transparency = 1f;
        }
        else
        {
            appearance.ExtendedAppearance.Transparency = 0f;
        }

        return SetAppearance(appearance, AppearanceImportOptions.ExtendedAppearance);
    }

    public Task Hide()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        appearance.ExtendedAppearance.Transparency = 1f;
        return SetAppearance(appearance, AppearanceImportOptions.ExtendedAppearance);
    }

    public Task Show()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        appearance.ExtendedAppearance.Transparency = 0f;
        return SetAppearance(appearance, AppearanceImportOptions.ExtendedAppearance);
    }

    public Task ApplyEmperors()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        appearance.Equipment = ActorEquipment.Emperors();
        appearance.Weapons.MainHand = SpecialAppearances.EmperorsMainHand;
        appearance.Weapons.OffHand = SpecialAppearances.EmperorsOffHand;
        return SetAppearance(appearance, AppearanceImportOptions.Gear);
    }


    public async Task Redraw()
    {
        await _actorAppearanceService.Redraw(Character);
        ApplyShaderOverride();

        if(Entity is ActorEntity actor && actor.IsProp == true)
            await _framework.RunOnTick(() =>
            {
                AttachWeapon();
            }, delayTicks: 5);

        return;
    }

    public async Task ResetAppearance()
    {
        _modelShaderOverride.Reset();
        if(_originalAppearance.HasValue)
        {
            var oldAppearance = _originalAppearance.Value;
            _originalAppearance = null;
            await _actorAppearanceService.SetCharacterAppearance(Character, oldAppearance, AppearanceImportOptions.All, true);
        }
    }

    public unsafe void AttachWeapon()
    {
        Character.Native()->Timeline.TimelineSequencer.PlayTimeline(5616);
    }

    public Task SetProp(WeaponModelId modelId)
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        appearance.Weapons.MainHand = modelId;
        return SetAppearance(appearance, AppearanceImportOptions.Weapon);
    }

    public WeaponModelId GetProp()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        return appearance.Weapons.MainHand;
    }

    private unsafe void ApplyShaderOverride()
    {
        var shaders = Character.GetShaderParams();
        if(shaders != null)
        {
            _modelShaderOverride.Apply(ref *shaders);
        }
    }

    private void OnGPoseStateChanged(bool newState)
    {
        if(newState)
            return;

        ResetCollection();
        _ = ResetAppearance();
    }

    private void OnPenumbraRedraw(int gameObjectId)
    {
        if(Character.ObjectIndex == gameObjectId && IsAppearanceOverridden)
            _ = SetAppearance(CurrentAppearance, AppearanceImportOptions.All);
    }

    public override void Dispose()
    {
        _gposeService.OnGPoseStateChange -= OnGPoseStateChanged;
        _penumbraService.OnPenumbraRedraw -= OnPenumbraRedraw;

        ResetCollection();
        _ = ResetAppearance();
    }
}
