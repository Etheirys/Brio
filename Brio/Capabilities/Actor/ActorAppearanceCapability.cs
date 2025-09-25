using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Files;
using Brio.Game.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Actor.Interop;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.Types;
using Brio.IPC;
using Brio.MCDF.Game.Services;
using Brio.Resources;
using Brio.UI.Widgets.Actor;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Brio.Capabilities.Actor;

public class ActorAppearanceCapability : ActorCharacterCapability
{
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly TargetService _targetService;
    private readonly PenumbraService _penumbraService;
    private readonly CustomizePlusService _customizePlusService;
    private readonly GlamourerService _glamourerService;

    private readonly CharacterHandlerService _characterHandlerService;
    private readonly EntityManager _entityManager;

    private readonly GPoseService _gposeService;
    private readonly IFramework _framework;
    private readonly MCDFService _mCDFService;

    public string CurrentCollection => _penumbraService.GetCollectionForObject(Character);
    public PenumbraService PenumbraService => _penumbraService;

    public bool IsCollectionOverridden => _oldCollection != null;
    private string? _oldCollection = null;

    public bool IsDesignOverridden;
    public bool IsProfileOverridden;

    public string CurrentDesign { get; set; } = "None";
    public GlamourerService GlamourerService => _glamourerService;


    public (string? name, Guid? id) SelectedDesign { get; set; } = ("None", null);
    public (string? data, Guid? id) CurrentProfile => _customizePlusService.GetActiveProfile(Character);
    public CustomizePlusService CustomizePlusService => _customizePlusService;


    private ActorAppearance? _originalAppearance = null;
    public bool IsAppearanceOverridden => _originalAppearance.HasValue || HasMCDF || IsDesignOverridden || IsProfileOverridden | IsCollectionOverridden;

    public bool HasPenumbraIntegration => _penumbraService.IsAvailable;
    public bool HasGlamourerIntegration => _glamourerService.IsAvailable;
    public bool HasCustomizePlusIntegration => _customizePlusService.IsAvailable;

    public ActorAppearance CurrentAppearance => _actorAppearanceService.GetActorAppearance(Character);

    public ActorAppearance OriginalAppearance => _originalAppearance ?? CurrentAppearance;

    public ModelShaderOverride _modelShaderOverride = new();

    public unsafe bool IsHuman => Character.GetHuman() != null;

    public bool CanTint => _actorAppearanceService.CanTint;

    public bool HasMCDF;
    public bool CanMCDF => _mCDFService.IsIPCAvailable;
    public bool IsSelf => _targetService.IsSelf(GameObject);

    public bool IsAnyMCDFLoading => _mCDFService.IsApplyingMCDF;

    public bool IsHidden => CurrentAppearance.ExtendedAppearance.Transparency == 0;

    public ActorAppearanceCapability(ActorEntity parent, CharacterHandlerService characterHandlerService, EntityManager entityManager, MCDFService mCDFService, IFramework framework, ActorAppearanceService actorAppearanceService,
        CustomizePlusService customizePlusService, PenumbraService penumbraService, TargetService targetService, GlamourerService glamourerService,
        GPoseService gPoseService) : base(parent)
    {
        _actorAppearanceService = actorAppearanceService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _gposeService = gPoseService;
        _customizePlusService = customizePlusService;
        _framework = framework;
        _entityManager = entityManager;
        _mCDFService = mCDFService;
        _targetService = targetService;
        _characterHandlerService = characterHandlerService;

        Widget = new ActorAppearanceWidget(this);

        _gposeService.OnGPoseStateChange += OnGPoseStateChanged;
        _penumbraService.OnPenumbraRedraw += OnPenumbraRedraw;

        SetSelectedProfile();
    }

    public async Task LoadMCDF(string path)
    {
        try
        {
            if(_mCDFService.IsApplyingMCDF)
            {
                Brio.NotifyError("Another MCDF is loading, Please wait for it to finish.");
                return;
            }

            Entity.LoadingDescription = "Loading MCDF...";
            Entity.IsLoading = true;

            await _mCDFService.LoadMCDFHeader(path);
            await _mCDFService.ApplyMCDF(GameObject);

            HasMCDF = true;
            Entity.IsLoading = false;
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Exception while Loading MCDF");
        }
    }

    public async Task SaveMcdf(string path, string dis)
    {
        try
        {
            Entity.LoadingDescription = "Saving MCDF...";
            Entity.IsLoading = true;
            await _mCDFService.SaveMCDF(path, dis, GameObject);
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Exception while Loading MCDF");
        }
        finally 
        {
            Entity.IsLoading = false;
        }
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

        _ = _actorAppearanceService.Redraw(Character, HasMCDF);
    }
    public void ResetCollection()
    {
        if(IsCollectionOverridden)
        {
            _penumbraService.SetCollectionForObject(Character, Guid.Parse(_oldCollection!));
            _oldCollection = null;
            _ = _actorAppearanceService.Redraw(Character, HasMCDF);
        }
    }

    public void SetDesign(Guid design)
    {
        IsDesignOverridden = true;
        _ = _glamourerService.ApplyDesign(design, Character);
    }
    public void ResetDesign(bool checkResetLock = true)
    {
        if(IsDesignOverridden)
        {
            HasMCDF = false;
            IsDesignOverridden = false;
            _glamourerService.RevertCharacter(Character);

            if(checkResetLock && _glamourerService.CheckForLock(Character))
            {
                ResetCollection();
                ResetProfile(false);
            }
        }
    }
    public void SetProfile(string data)
    {
        IsProfileOverridden = true;

        _customizePlusService.SetProfile(Character, data);
    }
    public void ResetProfile(bool checkResetLock = true)
    {
        if(IsProfileOverridden)
        {
            IsProfileOverridden = false;
            _customizePlusService.RemoveTemporaryProfile(Character);

            if(checkResetLock && _glamourerService.CheckForLock(Character))
            {
                ResetCollection();
                ResetDesign(false);
            }

            SetSelectedProfile();
        }
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
            foreach(IPCProfileDataTuple item in profiles)
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
    public void SetProfileToNone(bool force = false)
    {
        var profile = _customizePlusService.GetActiveProfile(Character);

        if(profile.Item1 is null && force is false)
        {
            ResetProfile();
        }
        else
        {
            SetProfile(Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")));
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

        if(Entity is ActorEntity actor && actor.IsProp is true)
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

    public unsafe void ExportAppearance(string file)
    {
        var currentAppearance = CurrentAppearance;
        BrioHuman.ShaderParams* shaders = Character.GetShaderParams();

        ActorAppearanceExtended actor = new() { Appearance = currentAppearance };
        if(shaders != null)
        {
            actor.ShaderParams = *shaders;
        }

        AnamnesisCharaFile appearance = actor;
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

    public Task ApplyInvisibleClothes()
    {
        var appearance = _actorAppearanceService.GetActorAppearance(Character);
        appearance.Equipment = ActorEquipment.Invisible();
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
        await _actorAppearanceService.Redraw(Character, HasMCDF);

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
        if(HasMCDF)
        {
            _ = _characterHandlerService.Revert(GameObject);
            HasMCDF = false;
        }
        else
        {
            ResetDesign();
            ResetCollection();
            ResetProfile();
        }

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
        ResetProfile();
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
        ResetProfile();
        _ = ResetAppearance();
    }
}
