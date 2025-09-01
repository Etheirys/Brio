using Brio.Config;
using Brio.Game.Actor;
using Brio.Game.Core;
using Brio.IPC;
using Brio.MCDF.API.Data;
using Brio.MCDF.Game.FileCache;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok.Animation;
using FFXIVClientStructs.Havok.Common.Base.Types;
using FFXIVClientStructs.Havok.Common.Serialize.Util;
using K4os.Compression.LZ4.Legacy;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Brio.MCDF.Game.Services;

public class MCDFService
{
    public static readonly IImmutableList<string> AllowedFileExtensions = [".mdl", ".tex", ".mtrl", ".tmb", ".pap", ".avfx", ".atex", ".sklb", ".eid", ".phyb", ".pbd", ".scd", ".skp", ".shpk"];

    private readonly IFramework _framework;
    private readonly TargetService _targetService;
    private readonly ConfigurationService _configurationService;
    private readonly FileCacheService _fileCacheService;
    private readonly DalamudService _dalamudService;
    private readonly ActorRedrawService _actorRedrawService;
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly TransientResourceService _transientResourceService;

    private readonly CharacterHandlerService _characterHandlerService;

    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly CustomizePlusService _customizePlusService;

    public bool IsIPCAvailable => _penumbraService.IsAvailable && _glamourerService.IsAvailable;

    public Task<(MareCharaFileHeader LoadedFile, long ExpectedLength)>? LoadedMcdfHeader { get; private set; }

    public Task? McdfApplicationTask { get; private set; }
    public Task? UiBlockingComputation { get; private set; }
    public string DataApplicationProgress { get; private set; } = string.Empty;

    //



    private int _globalFileCounter = 0;

    public MCDFService(IFramework framework, CharacterHandlerService characterHandlerService, ActorAppearanceService actorAppearanceService, ConfigurationService configurationService, FileCacheService fileCacheService, TargetService targetService, ActorRedrawService actorRedrawService, DalamudService dalamudService,
        PenumbraService penumbraService, TransientResourceService transientResourceService, GlamourerService glamourerService, CustomizePlusService customizePlusService)
    {
        _framework = framework;
        _configurationService = configurationService;
        _fileCacheService = fileCacheService;
        _targetService = targetService;
        _dalamudService = dalamudService;
        _actorRedrawService = actorRedrawService;
        _actorAppearanceService = actorAppearanceService;
        _transientResourceService = transientResourceService;

        _characterHandlerService = characterHandlerService;

        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _customizePlusService = customizePlusService;
    }

    public async Task LoadMCDFHeader(string path)
    {
        await (LoadedMcdfHeader = LoadFileHeader(path));
    }
    public async Task SaveMCDF(string path, string description, IGameObject gameObject)
    {
        await Task.Run(async () => await SaveCharaFileAsync(description, path, gameObject).ConfigureAwait(false));
    }

    public void ApplyMCDFToGPoseTarget()
    {
        var canApply = _targetService.CanApplyMCDFToTarget();

        if(canApply.CanApply)
            _ = ApplyMCDF(canApply.GameObject);
    }

    // Load

    public async Task ApplyMCDF(IGameObject gameObject)
    {
        if(gameObject.Address == IntPtr.Zero || gameObject.ObjectKind != ObjectKind.Player)
            return;

        var name = gameObject.Name.TextValue;

        await (McdfApplicationTask = Task.Run(async () =>
        {
            List<string> actuallyExtractedFiles = [];
           
            Brio.Log.Info("Extracting MCDF");

            try
            {
                Guid applicationId = Guid.NewGuid();

                if(LoadedMcdfHeader == null || !LoadedMcdfHeader.IsCompletedSuccessfully) return;

                var playerChar = await _dalamudService.GetPlayerCharacterAsync().ConfigureAwait(false);
                bool isSelf = playerChar is not null && string.Equals(playerChar.Name.TextValue, name, StringComparison.Ordinal);

                if(isSelf) return;

                long expectedExtractedSize = LoadedMcdfHeader.Result.ExpectedLength;
                var charaFile = LoadedMcdfHeader.Result.LoadedFile;

                DataApplicationProgress = "Extracting MCDF data";
                Brio.Log.Debug($"{DataApplicationProgress}");

                var extractedFiles = McdfExtractFiles(charaFile, expectedExtractedSize, actuallyExtractedFiles);

                foreach(var entry in charaFile.CharaFileData.FileSwaps.SelectMany(k => k.GamePaths, (k, p) => new KeyValuePair<string, string>(p, k.FileSwapPath)))
                {
                    extractedFiles[entry.Key] = entry.Value;
                }

                DataApplicationProgress = "Applying MCDF data";
                Brio.Log.Debug($"{DataApplicationProgress}");

                await ApplyDataAsync(applicationId, (name, gameObject), isSelf, charaFile.FilePath,
                    extractedFiles, charaFile.CharaFileData.ManipulationData, charaFile.CharaFileData.GlamourerData,
                    charaFile.CharaFileData.CustomizePlusData, CancellationToken.None).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Brio.Log.Warning(ex, "Failed to extract MCDF");
                throw;
            }
            finally
            {
                // delete extracted files
                foreach(var file in actuallyExtractedFiles)
                {
                    File.Delete(file);
                }
            }
        }));
    }

    public Task<(MareCharaFileHeader loadedCharaFile, long expectedLength)> LoadFileHeader(string filePath)
    {
        try
        {
            using var file = File.OpenRead(filePath);
            using var zipStream = new LZ4Stream(file, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression);
            using var reader = new BinaryReader(zipStream);
            var loadedFile = MareCharaFileHeader.FromBinaryReader(filePath, reader);

            long expectedLength = 0;

            if(loadedFile != null)
            {
                var itemNr = 0;
                foreach(var item in loadedFile.CharaFileData.Files)
                {
                    itemNr++;
                    expectedLength += item.Length;
                }
            }
            else
            {
                throw new InvalidOperationException("MCDF Header was null");
            }
            return Task.FromResult((loadedFile, expectedLength));

        }
        catch(Exception ex)
        {
            throw new InvalidOperationException($"Could not parse MCDF header of file {filePath}", ex);
        }
    }

    public Dictionary<string, string> McdfExtractFiles(MareCharaFileHeader? charaFileHeader, long expectedLength, List<string> extractedFiles)
    {
        if(charaFileHeader == null)
            return [];

        using var lz4Stream = new LZ4Stream(File.OpenRead(charaFileHeader.FilePath), LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression);
        using var reader = new BinaryReader(lz4Stream);
        MareCharaFileHeader.AdvanceReaderToData(reader);

        long totalRead = 0;
        Dictionary<string, string> gamePathToFilePath = new(StringComparer.Ordinal);
        foreach(var fileData in charaFileHeader.CharaFileData.Files)
        {
            var fileName = Path.Combine(_fileCacheService.CacheFolder, "brio_" + fileData.Hash + ".tmp");
            extractedFiles.Add(fileName);
            var length = fileData.Length;
            var bufferSize = length;
            using var fs = File.OpenWrite(fileName);
            using var wr = new BinaryWriter(fs);
            Brio.Log.Debug("Reading {length} of {fileName}", length.ToByteString(), fileName);
            var buffer = reader.ReadBytes(bufferSize);
            wr.Write(buffer);
            wr.Flush();
            wr.Close();
            if(buffer.Length == 0) throw new EndOfStreamException("Unexpected EOF");
            foreach(var path in fileData.GamePaths)
            {
                gamePathToFilePath[path] = fileName;
                Brio.Log.Debug("{path} => {fileName} [{hash}]", path, fileName, fileData.Hash);
            }
            totalRead += length;
            Brio.Log.Debug("Read {read}/{expected} bytes", totalRead.ToByteString(), expectedLength.ToByteString());
        }

        return gamePathToFilePath;
    }

    private async Task ApplyDataAsync(Guid applicationId, (string Name, IGameObject GameObject) tempHandler, bool isSelf, string UID,
        Dictionary<string, string> modPaths, string? manipData, string? glamourerData, string? customizeData, CancellationToken token)
    {
        Guid? cPlusId = null;
        Guid penumbraCollection;
        try
        {
            DataApplicationProgress = "Reverting previous Application";
          
            await _penumbraService.Redraw(tempHandler.GameObject);
            await _actorRedrawService.RedrawAndWait(tempHandler.GameObject);

            await _glamourerService.UnlockAndRevertCharacter(tempHandler.GameObject);
            await _glamourerService.UnlockAndRevertCharacterByName(tempHandler.Name);

            _customizePlusService.SetProfile(tempHandler.GameObject, "{}");

            await Task.Delay(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);

            DataApplicationProgress = "Applying Penumbra information";

            var idx = await _framework.RunOnFrameworkThread(() => tempHandler.GameObject?.ObjectIndex).ConfigureAwait(false) ?? 0;
            Brio.Log.Debug($"{DataApplicationProgress} idx:{idx}");

            penumbraCollection = await _penumbraService.CreateTemporaryCollectionAsync($"Brio_{idx}").ConfigureAwait(false);

            await _penumbraService.AssignTemporaryCollectionAsync(penumbraCollection, idx).ConfigureAwait(false);
            await _penumbraService.SetTemporaryModsAsync(applicationId, penumbraCollection, modPaths).ConfigureAwait(false);
            await _penumbraService.SetManipulationDataAsync(applicationId, penumbraCollection, manipData ?? string.Empty).ConfigureAwait(false);

            DataApplicationProgress = "Applying Glamourer and redrawing Character";
            Brio.Log.Debug($"{DataApplicationProgress}");

            await _glamourerService.ApplyAllAsync(tempHandler.GameObject, glamourerData, applicationId).ConfigureAwait(false);

            await _actorRedrawService.RedrawAndWait(tempHandler.GameObject);

            await _penumbraService.RemoveTemporaryCollectionAsync(applicationId, penumbraCollection).ConfigureAwait(false);

            DataApplicationProgress = "Applying Customize+ data";

            if(!string.IsNullOrEmpty(customizeData))
            {
                Brio.Log.Debug($"{DataApplicationProgress}");
                cPlusId =  await _customizePlusService.SetBodyScaleAsync(tempHandler.GameObject, customizeData).ConfigureAwait(false);
            }
            else
            {
                Brio.Log.Debug($"{DataApplicationProgress} IsNullOrEmpty");
                cPlusId = await _customizePlusService.SetBodyScaleAsync(tempHandler.GameObject, Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))).ConfigureAwait(false);
            }

            _characterHandlerService.CharacterHandler.Add(new CharacterHolder(tempHandler.GameObject, cPlusId, tempHandler.Name));
        }
        finally
        {
            if(token.IsCancellationRequested)
            {
                DataApplicationProgress = "Application aborted. Reverting Character...";
                await _characterHandlerService.RevertMCDF(new CharacterHolder(tempHandler.GameObject, cPlusId, tempHandler.Name)).ConfigureAwait(false);
            }

            DataApplicationProgress = string.Empty;
        }
    }

    // Save

    public async Task ExportSelfAsMCDFAsync(string description, string filePath)
    {
        var gposeTaget = await _framework.RunOnTick(() =>
        {
            if(_dalamudService.GetIsPlayerPresent())
            {
                return _dalamudService.GetPlayerCharacter();
            }
            return null;
        });

        if(gposeTaget is not null && gposeTaget.Address == IntPtr.Zero)
            return;

        await Task.Run(async () => await SaveCharaFileAsync(description, filePath, gposeTaget!).ConfigureAwait(false));
    }
    public async Task ExportTargetAsMCDFAsync(string description, string filePath)
    {
        var gposeTaget = await _framework.RunOnFrameworkThread(_targetService.CanApplyMCDFToTarget);

        if(gposeTaget.CanApply == false)
            return;

        await Task.Run(async () => await SaveCharaFileAsync(description, filePath, gposeTaget.GameObject).ConfigureAwait(false));
    }

    internal async Task SaveCharaFileAsync(string description, string filePath, IGameObject gameObject)
    {
        var tempFilePath = filePath + ".tmp";

        try
        {
            Brio.Log.Info("Starting MCDF export...");

            var data = await CreatePlayerData(gameObject).ConfigureAwait(false);
            if(data == null) return;
         
            MareCharaFileData mareCharaFileData = new MareCharaFileData(_fileCacheService, "", data);
            MareCharaFileHeader output = new(MareCharaFileHeader.CurrentVersion, mareCharaFileData);

            // Why do I need this and Mare didn't huh?!?
            await Task.Run(async () =>
            {
                using var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                using var lz4 = new LZ4Stream(fs, LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression);
                using var writer = new BinaryWriter(lz4);
                output.WriteToStream(writer);

                foreach(var item in output.CharaFileData.Files)
                {
                    var file = _fileCacheService.GetFileCacheByHash(item.Hash)!;

                    var fsRead = File.OpenRead(file.ResolvedFilepath);
                    await using(fsRead.ConfigureAwait(false))
                    {
                        using var br = new BinaryReader(fsRead);
                        byte[] buffer = new byte[item.Length];
                        br.Read(buffer, 0, item.Length);
                        writer.Write(buffer);
                    }
                }

                writer.Flush();
                await lz4.FlushAsync().ConfigureAwait(false);
                await fs.FlushAsync().ConfigureAwait(false);
                fs.Close();
                File.Move(tempFilePath, filePath, true);
           
                Brio.Log.Info("MCDF export complete!");
            });
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Failure Saving Mare Chara File, deleting output");
            File.Delete(tempFilePath);
        }
    }

    public async Task<API.Data.CharacterData?> CreatePlayerData(IGameObject gameObject)
    {
        CharacterDataEX newCdata = new();
        var fragment = await BuildCharacterData(gameObject, CancellationToken.None).ConfigureAwait(false);
       
        newCdata.SetFragment(API.Data.Enum.ObjectKind.Player, fragment);
    
        if(newCdata.FileReplacements.TryGetValue(API.Data.Enum.ObjectKind.Player, out var playerData) && playerData != null)
        {
            foreach(var data in playerData.Select(g => g.GamePaths))
            {
                data.RemoveWhere(g => g.EndsWith(".pap", StringComparison.OrdinalIgnoreCase)
                    || g.EndsWith(".tmb", StringComparison.OrdinalIgnoreCase)
                    || g.EndsWith(".scd", StringComparison.OrdinalIgnoreCase)
                    || (g.EndsWith(".avfx", StringComparison.OrdinalIgnoreCase)
                        && !g.Contains("/weapon/", StringComparison.OrdinalIgnoreCase)
                        && !g.Contains("/equipment/", StringComparison.OrdinalIgnoreCase))
                    || (g.EndsWith(".atex", StringComparison.OrdinalIgnoreCase)
                        && !g.Contains("/weapon/", StringComparison.OrdinalIgnoreCase)
                        && !g.Contains("/equipment/", StringComparison.OrdinalIgnoreCase)));
            }

            playerData.RemoveWhere(g => g.GamePaths.Count == 0);
        }

        return newCdata.ToAPI();
    }

    public async Task<CharacterDataFragment?> BuildCharacterData(IGameObject playerRelatedObject, CancellationToken token)
    {
        if(IsIPCAvailable is false)
        {
            throw new InvalidOperationException("Penumbra or Glamourer is not connected");
        }

        if(playerRelatedObject == null) return null;

        bool pointerIsZero = true;
        try
        {
            pointerIsZero = playerRelatedObject.Address == IntPtr.Zero;
            try
            {
                pointerIsZero = await CheckForNullDrawObject(playerRelatedObject.Address).ConfigureAwait(false);
            }
            catch
            {
                pointerIsZero = true;
                Brio.Log.Debug("NullRef for {object}", playerRelatedObject);
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Could not create data for {object}", playerRelatedObject);
        }

        if(pointerIsZero)
        {
            Brio.Log.Debug("Pointer was zero for {objectKind}", playerRelatedObject.ObjectKind);
            return null;
        }

        try
        {
            return await CreateCharacterData(playerRelatedObject, token).ConfigureAwait(false);
        }
        catch(OperationCanceledException)
        {
            Brio.Log.Debug("Cancelled creating Character data for {object}", playerRelatedObject);
            throw;
        }
        catch(Exception e)
        {
            Brio.Log.Warning(e, "Failed to create {object} data", playerRelatedObject);
        }

        return null;
    }

    private async Task<bool> CheckForNullDrawObject(IntPtr playerPointer)
    {
        return await _framework.RunOnFrameworkThread(() => CheckForNullDrawObjectUnsafe(playerPointer)).ConfigureAwait(false);
    }
    private unsafe bool CheckForNullDrawObjectUnsafe(IntPtr playerPointer)
    {
        return ((Character*)playerPointer)->GameObject.DrawObject == null;
    }

    private async Task<CharacterDataFragment> CreateCharacterData(IGameObject playerRelatedObject, CancellationToken ct)
    {
        var objectKind = playerRelatedObject.ObjectKind;
        CharacterDataFragment fragment = objectKind == ObjectKind.Player ? new CharacterDataFragmentPlayer() : new();

        Brio.Log.Verbose("Building character data for {obj}", playerRelatedObject);

        // wait until chara is not drawing and present so nothing spontaneously explodes
        await _actorRedrawService.WaitForDrawing(playerRelatedObject).ConfigureAwait(false);
        int totalWaitTime = 10000;
        while(!await _dalamudService.IsObjectPresentAsync(playerRelatedObject).ConfigureAwait(false) && totalWaitTime > 0)
        {
            Brio.Log.Debug("Character is null but it shouldn't be, waiting");
            await Task.Delay(50, ct).ConfigureAwait(false);
            totalWaitTime -= 50;
        }

        if(_glamourerService.CheckForLock(playerRelatedObject))
        {
            Brio.Log.Information("Unable to apply MCDF, Actor is Locked by Glamourer");
            Brio.NotifyError("Unable to apply MCDF, Actor is Locked by Glamourer");

            throw new Exception("Glamourer has Lock");
        }

        ct.ThrowIfCancellationRequested();

        Dictionary<string, List<ushort>>? boneIndices =
            objectKind != ObjectKind.Player
            ? null
            : await _framework.RunOnFrameworkThread(() => GetSkeletonBoneIndices(playerRelatedObject)).ConfigureAwait(false);

        DateTime start = DateTime.UtcNow;

        // penumbra call, it's currently broken (How is this broken?) (KEN)
        Dictionary<string, HashSet<string>>? resolvedPaths;

        resolvedPaths = (await _penumbraService.GetCharacterData(playerRelatedObject).ConfigureAwait(false));
        if(resolvedPaths == null) throw new InvalidOperationException("Penumbra returned null data");

        ct.ThrowIfCancellationRequested();

        fragment.FileReplacements = [.. new HashSet<FileReplacement>(resolvedPaths.Select(c => new FileReplacement([.. c.Value], c.Key)), FileReplacementComparer.Instance).Where(p => p.HasFileReplacement)];
        fragment.FileReplacements.RemoveWhere(c => c.GamePaths.Any(g => !AllowedFileExtensions.Any(e => g.EndsWith(e, StringComparison.OrdinalIgnoreCase))));

        ct.ThrowIfCancellationRequested();

        Brio.Log.Verbose("== Static Replacements ==");
        foreach(var replacement in fragment.FileReplacements.Where(i => i.HasFileReplacement).OrderBy(i => i.GamePaths.First(), StringComparer.OrdinalIgnoreCase))
        {
            Brio.Log.Verbose("=> {repl}", replacement);
            ct.ThrowIfCancellationRequested();
        }

        await _transientResourceService.WaitForRecording(ct).ConfigureAwait(false);

        // if it's pet then it's summoner, if it's summoner we actually want to keep all filereplacements alive at all times
        // or we get into redraw city for every change and nothing works properly
        if(objectKind == ObjectKind.Companion)
        {
            foreach(var item in fragment.FileReplacements.Where(i => i.HasFileReplacement).SelectMany(p => p.GamePaths))
            {
                if(_transientResourceService.AddTransientResource(API.Data.Enum.ObjectKind.Pet, item))
                {
                    Brio.Log.Verbose("Marking static {item} for Pet as transient", item);
                }
            }

            Brio.Log.Verbose("Clearing {count} Static Replacements for Pet", fragment.FileReplacements.Count);
            fragment.FileReplacements.Clear();
        }

        ct.ThrowIfCancellationRequested();

        Brio.Log.Verbose("Handling transient update for {obj}", playerRelatedObject);

        // remove all potentially gathered paths from the transient resource manager that are resolved through static resolving
        _transientResourceService.ClearTransientPaths(API.Data.Enum.ObjectKind.Player, fragment.FileReplacements.SelectMany(c => c.GamePaths).ToList());

        // get all remaining paths and resolve them
        var transientPaths = ManageSemiTransientData(API.Data.Enum.ObjectKind.Player);
        var resolvedTransientPaths = await GetFileReplacementsFromPaths(transientPaths, new HashSet<string>(StringComparer.Ordinal)).ConfigureAwait(false);

        Brio.Log.Verbose("== Transient Replacements ==");
        foreach(var replacement in resolvedTransientPaths.Select(c => new FileReplacement([.. c.Value], c.Key)).OrderBy(f => f.ResolvedPath, StringComparer.Ordinal))
        {
            Brio.Log.Verbose("=> {repl}", replacement);
            fragment.FileReplacements.Add(replacement);
        }

        // clean up all semi transient resources that don't have any file replacement (aka null resolve)
        _transientResourceService.CleanUpSemiTransientResources(API.Data.Enum.ObjectKind.Player, [.. fragment.FileReplacements]);

        ct.ThrowIfCancellationRequested();

        // make sure we only return data that actually has file replacements
        fragment.FileReplacements = new HashSet<FileReplacement>(fragment.FileReplacements.Where(v => v.HasFileReplacement).OrderBy(v => v.ResolvedPath, StringComparer.Ordinal), FileReplacementComparer.Instance);

        // gather up data from ipc
        //Task<string> getHeelsOffset = _ipcManager.Heels.GetOffsetAsync();
        //Task<string> getHonorificTitle = _ipcManager.Honorific.GetTitle();

        Task<string> getGlamourerData = _glamourerService.GetCharacterCustomizationAsync(playerRelatedObject.Address);
        Task<string?> getCustomizeData = _customizePlusService.GetScaleAsync(playerRelatedObject.Address);
        fragment.GlamourerString = await getGlamourerData.ConfigureAwait(false);
        Brio.Log.Verbose("Glamourer is now: {data}", fragment.GlamourerString);
        var customizeScale = await getCustomizeData.ConfigureAwait(false);
        fragment.CustomizePlusScale = customizeScale ?? string.Empty;
        Brio.Log.Verbose("Customize is now: {data}", fragment.CustomizePlusScale);
       
        if(objectKind == ObjectKind.Player)
        {
            var playerFragment = (fragment as CharacterDataFragmentPlayer)!;
            playerFragment.ManipulationString = _penumbraService.GetMetaManipulations();

            //    playerFragment!.HonorificData = await getHonorificTitle.ConfigureAwait(false);
            //    Brio.Log.Verbose("Honorific is now: {data}", playerFragment!.HonorificData);

            //    playerFragment!.HeelsData = await getHeelsOffset.ConfigureAwait(false);
            //    Brio.Log.Verbose("Heels is now: {heels}", playerFragment!.HeelsData);

            //    playerFragment!.MoodlesData = await _ipcManager.Moodles.GetStatusAsync(playerRelatedObject.Address).ConfigureAwait(false) ?? string.Empty;
            //    Brio.Log.Verbose("Moodles is now: {moodles}", playerFragment!.MoodlesData);

            //    playerFragment!.PetNamesData = _ipcManager.PetNames.GetLocalNames();
            //    Brio.Log.Verbose("Pet Nicknames is now: {petnames}", playerFragment!.PetNamesData);
        }

        ct.ThrowIfCancellationRequested();

        var toCompute = fragment.FileReplacements.Where(f => !f.IsFileSwap).ToArray();
        Brio.Log.Verbose("Getting Hashes for {amount} Files", toCompute.Length);
        var computedPaths = _fileCacheService.GetFileCachesByPaths(toCompute.Select(c => c.ResolvedPath).ToArray());
        foreach(var file in toCompute)
        {
            ct.ThrowIfCancellationRequested();
            file.Hash = computedPaths[file.ResolvedPath]?.Hash ?? string.Empty;
        }
          
        var removed = fragment.FileReplacements.RemoveWhere(f => !f.IsFileSwap && string.IsNullOrEmpty(f.Hash));
        if(removed > 0)
        {
            Brio.Log.Verbose("Removed {amount} of invalid files", removed);
        }
    
        ct.ThrowIfCancellationRequested();

        if(objectKind == ObjectKind.Player)
        {
            try
            {
                await VerifyPlayerAnimationBones(boneIndices, (fragment as CharacterDataFragmentPlayer)!, ct).ConfigureAwait(false);
            }
            catch(OperationCanceledException e)
            {
                Brio.Log.Debug(e, "Cancelled during player animation verification");
                throw;
            }
            catch(Exception e)
            {
                Brio.Log.Warning(e, "Failed to verify player animations, continuing without further verification");
            }
        }
      
        Brio.Log.Info("Building character data for {obj} took {time}ms", objectKind, TimeSpan.FromTicks(DateTime.UtcNow.Ticks - start.Ticks).TotalMilliseconds);

        return fragment;
    }

    private async Task VerifyPlayerAnimationBones(Dictionary<string, List<ushort>>? boneIndices, CharacterDataFragmentPlayer fragment, CancellationToken ct)
    {
        if(boneIndices == null) return;

        foreach(var kvp in boneIndices)
        {
            Brio.Log.Verbose("Found {skellyname} ({idx} bone indices) on player: {bones}", kvp.Key, kvp.Value.Any() ? kvp.Value.Max() : 0, string.Join(',', kvp.Value));
        }

        if(boneIndices.All(u => u.Value.Count == 0)) return;

        int noValidationFailed = 0;
        foreach(var file in fragment.FileReplacements.Where(f => !f.IsFileSwap && f.GamePaths.First().EndsWith("pap", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            ct.ThrowIfCancellationRequested();

            var skeletonIndices = await _framework.RunOnFrameworkThread(() => GetBoneIndicesFromPap(file.Hash)).ConfigureAwait(false);
            bool validationFailed = false;
            if(skeletonIndices != null)
            {
                // 105 is the maximum vanilla skellington spoopy bone index
                if(skeletonIndices.All(k => k.Value.Max() <= 105))
                {
                    Brio.Log.Verbose("All indices of {path} are <= 105, ignoring", file.ResolvedPath);
                    continue;
                }

                Brio.Log.Verbose("Verifying bone indices for {path}, found {x} skeletons", file.ResolvedPath, skeletonIndices.Count);

                foreach(var boneCount in skeletonIndices.Select(k => k).ToList())
                {
                    if(boneCount.Value.Max() > boneIndices.SelectMany(b => b.Value).Max())
                    {
                        Brio.Log.Debug("Found more bone indices on the animation {path} skeleton {skl} (max indice {idx}) than on any player related skeleton (max indice {idx2})",
                             file.ResolvedPath, boneCount.Key, boneCount.Value.Max(), boneIndices.SelectMany(b => b.Value).Max());
                        validationFailed = true;
                        break;
                    }
                }
            }

            if(validationFailed)
            {
                noValidationFailed++;
                Brio.Log.Verbose("Removing {file} from sent file replacements and transient data", file.ResolvedPath);
                fragment.FileReplacements.Remove(file);
                foreach(var gamePath in file.GamePaths)
                {
                    _transientResourceService.RemoveTransientResource(API.Data.Enum.ObjectKind.Player, gamePath);
                }
            }
        }

        if(noValidationFailed > 0)
        {
            //_mareMediator.Publish(new NotificationMessage("Invalid Skeleton Setup",
            //    $"Your client is attempting to send {noValidationFailed} animation files with invalid bone data. Those animation files have been removed from your sent data. " +
            //    $"Verify that you are using the correct skeleton for those animation files (Check /xllog for more information).",
            //    NotificationType.Warning, TimeSpan.FromSeconds(10)));
        }
    }

    private async Task<IReadOnlyDictionary<string, string[]>> GetFileReplacementsFromPaths(HashSet<string> forwardResolve, HashSet<string> reverseResolve)
    {
        var forwardPaths = forwardResolve.ToArray();
        var reversePaths = reverseResolve.ToArray();
        Dictionary<string, List<string>> resolvedPaths = new(StringComparer.Ordinal);
        var (forward, reverse) = await _penumbraService.ResolvePathsAsync(forwardPaths, reversePaths).ConfigureAwait(false);
        for(int i = 0; i < forwardPaths.Length; i++)
        {
            var filePath = forward[i].ToLowerInvariant();
            if(resolvedPaths.TryGetValue(filePath, out var list))
            {
                list.Add(forwardPaths[i].ToLowerInvariant());
            }
            else
            {
                resolvedPaths[filePath] = [forwardPaths[i].ToLowerInvariant()];
            }
        }

        for(int i = 0; i < reversePaths.Length; i++)
        {
            var filePath = reversePaths[i].ToLowerInvariant();
            if(resolvedPaths.TryGetValue(filePath, out var list))
            {
                list.AddRange(reverse[i].Select(c => c.ToLowerInvariant()));
            }
            else
            {
                resolvedPaths[filePath] = [.. reverse[i].Select(c => c.ToLowerInvariant())];
            }
        }

        return resolvedPaths.ToDictionary(k => k.Key, k => k.Value.ToArray(), StringComparer.OrdinalIgnoreCase).AsReadOnly();
    }

    private HashSet<string> ManageSemiTransientData(API.Data.Enum.ObjectKind objectKind)
    {
        _transientResourceService.PersistTransientResources(objectKind);

        HashSet<string> pathsToResolve = new(StringComparer.Ordinal);
        foreach(var path in _transientResourceService.GetSemiTransientResources(objectKind).Where(path => !string.IsNullOrEmpty(path)))
        {
            pathsToResolve.Add(path);
        }

        return pathsToResolve;
    }

    public unsafe Dictionary<string, List<ushort>>? GetSkeletonBoneIndices(IGameObject handler)
    {
        if(handler.Address == nint.Zero) return null;
        var chara = (CharacterBase*)(((Character*)handler.Address)->GameObject.DrawObject);
        if(chara->GetModelType() != CharacterBase.ModelType.Human) return null;
        var resHandles = chara->Skeleton->SkeletonResourceHandles;
        Dictionary<string, List<ushort>> outputIndices = [];
        try
        {
            for(int i = 0; i < chara->Skeleton->PartialSkeletonCount; i++)
            {
                var handle = *(resHandles + i);
                //Brio.Log.Verbose("Iterating over SkeletonResourceHandle #{i}:{x}", i, ((nint)handle).ToString("X"));
                if((nint)handle == nint.Zero) continue;
                var curBones = handle->BoneCount;
                // this is unrealistic, the filename shouldn't ever be that long
                if(handle->FileName.Length > 1024) continue;
                var skeletonName = handle->FileName.ToString();
                if(string.IsNullOrEmpty(skeletonName)) continue;
                outputIndices[skeletonName] = new();
                for(ushort boneIdx = 0; boneIdx < curBones; boneIdx++)
                {
                    var boneName = handle->HavokSkeleton->Bones[boneIdx].Name.String;
                    if(boneName == null) continue;
                    outputIndices[skeletonName].Add((ushort)(boneIdx + 1));
                }
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Could not process skeleton data");
        }

        return (outputIndices.Count != 0 && outputIndices.Values.All(u => u.Count > 0)) ? outputIndices : null;
    }

    public unsafe Dictionary<string, List<ushort>>? GetBoneIndicesFromPap(string hash)
    {
        if(_configurationService.Configuration.MCDF.DataStorage.BonesDictionary.TryGetValue(hash, out var bones)) return bones;

        var cacheEntity = _fileCacheService.GetFileCacheByHash(hash);
        if(cacheEntity == null) return null;

        using BinaryReader reader = new BinaryReader(File.Open(cacheEntity.ResolvedFilepath, FileMode.Open, FileAccess.Read, FileShare.Read));

        // most of this shit is from vfxeditor, surely nothing will change in the pap format :copium:
        reader.ReadInt32(); // ignore
        reader.ReadInt32(); // ignore
        reader.ReadInt16(); // read 2 (num animations)
        reader.ReadInt16(); // read 2 (modelid)
        var type = reader.ReadByte();// read 1 (type)
        if(type != 0) return null; // it's not human, just ignore it, whatever

        reader.ReadByte(); // read 1 (variant)
        reader.ReadInt32(); // ignore
        var havokPosition = reader.ReadInt32();
        var footerPosition = reader.ReadInt32();
        var havokDataSize = footerPosition - havokPosition;
        reader.BaseStream.Position = havokPosition;
        var havokData = reader.ReadBytes(havokDataSize);
        if(havokData.Length <= 8) return null; // no havok data

        var output = new Dictionary<string, List<ushort>>(StringComparer.OrdinalIgnoreCase);
        var tempHavokDataPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + ".hkx";
        var tempHavokDataPathAnsi = Marshal.StringToHGlobalAnsi(tempHavokDataPath);

        try
        {
            File.WriteAllBytes(tempHavokDataPath, havokData);

            var loadoptions = stackalloc hkSerializeUtil.LoadOptions[1];
            loadoptions->TypeInfoRegistry = hkBuiltinTypeRegistry.Instance()->GetTypeInfoRegistry();
            loadoptions->ClassNameRegistry = hkBuiltinTypeRegistry.Instance()->GetClassNameRegistry();
            loadoptions->Flags = new hkFlags<hkSerializeUtil.LoadOptionBits, int>
            {
                Storage = (int)(hkSerializeUtil.LoadOptionBits.Default)
            };

            var resource = hkSerializeUtil.LoadFromFile((byte*)tempHavokDataPathAnsi, null, loadoptions);
            if(resource == null)
            {
                throw new InvalidOperationException("Resource was null after loading");
            }

            var rootLevelName = @"hkRootLevelContainer"u8;
            fixed(byte* n1 = rootLevelName)
            {
                var container = (hkRootLevelContainer*)resource->GetContentsPointer(n1, hkBuiltinTypeRegistry.Instance()->GetTypeInfoRegistry());
                var animationName = @"hkaAnimationContainer"u8;
                fixed(byte* n2 = animationName)
                {
                    var animContainer = (hkaAnimationContainer*)container->findObjectByName(n2, null);
                    for(int i = 0; i < animContainer->Bindings.Length; i++)
                    {
                        var binding = animContainer->Bindings[i].ptr;
                        var boneTransform = binding->TransformTrackToBoneIndices;
                        string name = binding->OriginalSkeletonName.String! + "_" + i;
                        output[name] = [];
                        for(int boneIdx = 0; boneIdx < boneTransform.Length; boneIdx++)
                        {
                            output[name].Add((ushort)boneTransform[boneIdx]);
                        }
                        output[name].Sort();
                    }
                }
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Could not load havok file in {path}", tempHavokDataPath);
        }
        finally
        {
            Marshal.FreeHGlobal(tempHavokDataPathAnsi);
            File.Delete(tempHavokDataPath);
        }

        _configurationService.Configuration.MCDF.DataStorage.BonesDictionary[hash] = output;
        _configurationService.Save();
        return output;
    }
}
