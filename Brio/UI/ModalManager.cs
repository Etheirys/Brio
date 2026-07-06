using Brio.Capabilities.Posing;
using Brio.Entities.Core;
using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Services;
using Brio.UI.Controls.Core;
using Brio.UI.Modals;
using Brio.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.UI;

public class ModalManager
{
    public static ModalManager Instance { get; private set; } = null!;

    private readonly RenameActorModal _renameActorModal;
    private readonly ExportSceneModal _exportSceneModal;
    private readonly ImportSceneModal _importSceneModal;
    private readonly SaveProjectModal _saveProjectModal;
    private readonly MetadataModal _exportPoseMetadataModal;
    private readonly LibraryWindow _libraryWindow;

    private readonly List<Modal> _modals = [];

    public bool IsRenameModalOpen => _renameActorModal.IsOpen;
    public bool IsExportSceneModalOpen => _exportSceneModal.IsOpen;
    public bool IsImportSceneModalOpen => _importSceneModal.IsOpen;
    public bool IsSaveProjectModalOpen => _saveProjectModal.IsOpen;
    public bool IsExportPoseMetadataModalOpen => _exportPoseMetadataModal.IsOpen;
    public bool IsLibraryModalOpen => _libraryWindow.IsModal;

    public bool IsAnyModalOpen => _modals.Any(m => m.IsOpen) || _libraryWindow.IsModal;

    public ModalManager(RenameActorModal renameActorModal, ExportSceneModal exportSceneModal, ImportSceneModal importSceneModal,
        SaveProjectModal saveProjectModal, MetadataModal exportPoseMetadataModal, LibraryWindow libraryWindow)
    {
        Instance = this;

        _renameActorModal = renameActorModal;
        _exportSceneModal = exportSceneModal;
        _importSceneModal = importSceneModal;
        _saveProjectModal = saveProjectModal;
        _exportPoseMetadataModal = exportPoseMetadataModal;
        _libraryWindow = libraryWindow;

        AddModal(_renameActorModal);
        AddModal(_exportSceneModal);
        AddModal(_importSceneModal);
        AddModal(_saveProjectModal);
        AddModal(_exportPoseMetadataModal);
    }

    public void AddModal(Modal modal)
    {
        _modals.Add(modal);
    }

    public void OpenRenameModal(Entity entity)
    {
        _renameActorModal.Open(entity);
    }

    public void OpenExportSceneModal()
    {
        _exportSceneModal.Open();
    }

    public void OpenImportSceneModal()
    {
        _importSceneModal.Open();
    }

    public void OpenSaveProjectModal()
    {
        _saveProjectModal.Open();
    }

    public void OpenExportPoseMetadataModal(PosingCapability capability, string path)
    {
        _exportPoseMetadataModal.Open(capability, path);
    }

    public void OpenEditMetadataModal(FileEntry fileEntry)
    {
        _exportPoseMetadataModal.Open(fileEntry);
    }

    public void OpenLibraryModal(FilterBase filter, Action<object> callback)
    {
        _libraryWindow.OpenModal(filter, callback);
    }

    public void Draw()
    {
        foreach(var modal in _modals)
        {
            modal.Draw();
        }

        _libraryWindow.DrawModal();
    }
}
