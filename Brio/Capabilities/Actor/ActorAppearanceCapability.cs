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
using System;
using System.Threading.Tasks;

namespace Brio.Capabilities.Actor;

internal class ActorAppearanceCapability : ActorCharacterCapability
{
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly MareService _mareService;
    private readonly GPoseService _gposeService;

    public string CurrentCollection => _penumbraService.GetCollectionForObject(Character);
    public PenumbraService PenumbraService => _penumbraService;

    public bool IsCollectionOverridden => _oldCollection != null;
    private string? _oldCollection = null;

    private ActorAppearance? _originalAppearance = null;
    public bool IsAppearanceOverridden => _originalAppearance.HasValue;

    public bool HasPenumbraIntegration => _penumbraService.IsPenumbraAvailable;

    public ActorAppearance CurrentAppearance => _actorAppearanceService.GetActorAppearance(Character);

    public ActorAppearance OriginalAppearance => _originalAppearance ?? CurrentAppearance;

    public ModelShaderOverride _modelShaderOverride = new();

    public unsafe bool IsHuman => Character.GetHuman() != null;

    public bool CanTint => _actorAppearanceService.CanTint;

    public bool CanMcdf => _mareService.IsMareAvailable;

    public ActorAppearanceCapability(ActorEntity parent, ActorAppearanceService actorAppearanceService, PenumbraService penumbraService, GlamourerService glamourerService, MareService mareService, GPoseService gPoseService) : base(parent)
    {
        _actorAppearanceService = actorAppearanceService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _mareService = mareService;
        _gposeService = gPoseService;
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
