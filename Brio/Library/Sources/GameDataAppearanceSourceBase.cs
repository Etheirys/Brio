﻿using Brio.Entities;
using Brio.Resources;
using Dalamud.Interface.Textures.TextureWraps;

namespace Brio.Library.Sources;

public abstract class GameDataAppearanceSourceBase : SourceBase
{
    private GameDataProvider _lumina;
    private EntityManager _entityManager;

    public GameDataAppearanceSourceBase(GameDataProvider lumina, EntityManager entityManager)
     : base()
    {
        _lumina = lumina;
        _entityManager = entityManager;
    }

    public override IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png");
    protected GameDataProvider Lumina => _lumina;
    protected EntityManager EntityManager => _entityManager;
}
