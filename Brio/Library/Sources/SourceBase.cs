﻿using Dalamud.Interface.Internal;
using System;

namespace Brio.Library.Sources;

public abstract class SourceBase : LibraryEntryBase
{
    private string _name;
    private IDalamudTextureWrap _icon;

    public SourceBase(string name, IDalamudTextureWrap icon)
        : base(null)
    {
        _name = name;
        _icon = icon;
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => _icon;

    public abstract void Scan();
}