﻿using System;
using System.Collections.Generic;
using System.IO;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using ToSic.Eav.Apps;
using ToSic.Eav.Logging;
using ToSic.Eav.Persistence;
using ToSic.Eav.Persistence.Interfaces;
using ToSic.Eav.Persistence.Logging;
using ToSic.Eav.Run;
using ToSic.Sxc.Engines;
using App = ToSic.Sxc.Apps.App;

namespace ToSic.Sxc.Dnn.ImportExport
{
    public class DnnImportExportEnvironment : HasLog, IImportExportEnvironment
    {
        #region Constructors

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="tenant"></param>
        public DnnImportExportEnvironment(IEnvironment environment, ITenant tenant) : base("Dnn.ImExEn")
        {
            Environment = environment;
            _tenant = tenant;
        }

        protected IEnvironment Environment;
        private readonly ITenant _tenant; 

        public IImportExportEnvironment Init(ILog parent)
        {
            Log.LinkTo(parent);
            return this;
        }

        #endregion


        public List<Message> Messages { get; } = new List<Message>();

        /// <inheritdoc />
        /// <summary>
        /// Copy all files from SourceFolder to DestinationFolder
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destinationFolder">The portal-relative path where the files should be copied to</param>
        public void TransferFilesToTenant(string sourceFolder, string destinationFolder)
        {
            Log.Add($"transfer files to tenant from:{sourceFolder} to:{destinationFolder}");
            var messages = Messages;
            var files = Directory.GetFiles(sourceFolder, "*.*");

            var dnnFileManager = FileManager.Instance;
            var dnnFolderManager = FolderManager.Instance;
            var portalId = _tenant.Id;

            if (!dnnFolderManager.FolderExists(portalId, destinationFolder))
                dnnFolderManager.AddFolder(portalId, destinationFolder);
            var folderInfo = dnnFolderManager.GetFolder(portalId, destinationFolder);

            foreach (var sourceFilePath in files)
            {
                Log.Add($"file:{sourceFilePath}");
                var destinationFileName = Path.GetFileName(sourceFilePath);

                if (!dnnFileManager.FileExists(folderInfo, destinationFileName))
                {
                    try
                    {
                        using (var stream = File.OpenRead(sourceFilePath))
                            dnnFileManager.AddFile(folderInfo, destinationFileName, stream, false);
                    }
                    catch (InvalidFileExtensionException e)
                    {
                        messages.Add(
                            new Message(
                                "File '" + destinationFileName +
                                "' not copied because the file extension is not allowed.",
                                Message.MessageTypes.Error));
                        Exceptions.LogException(e);
                    }
                    catch (Exception e)
                    {
                        messages.Add(new Message("Can't copy file '" + destinationFileName + "' because of an unkown error. It's likely that your files and folders are not in sync with DNN, usually re-syncing will fix the issue.", Message.MessageTypes.Warning));
                        Exceptions.LogException(e);
                    }
                }
                else
                    messages.Add(new Message("File '" + destinationFileName + "' not copied because it already exists", Message.MessageTypes.Warning));
            }

            // Call the method recursively to handle subdirectories
            foreach (var sourceFolderPath in Directory.GetDirectories(sourceFolder))
            {
                Log.Add($"subfolder:{sourceFolderPath}");
                var newDestinationFolder = Path.Combine(destinationFolder, sourceFolderPath.Replace(sourceFolder, "").TrimStart('\\')).Replace('\\', '/');
                TransferFilesToTenant(sourceFolderPath, newDestinationFolder);
            }
        }

        public Version TenantVersion => typeof(PortalSettings).Assembly.GetName().Version;

        public string DefaultLanguage => _tenant.DefaultLanguage;

        public string TemplatesRoot(int zoneId, int appId)
        {
            var app = Eav.Factory.Resolve<App>().InitNoData(new AppIdentity(zoneId, appId), Log);

            // Copy all files in 2sexy folder to (portal file system) 2sexy folder
            var templateRoot = Environment.MapPath(TemplateHelpers.GetTemplatePathRoot(Settings.TemplateLocations.PortalFileSystem, app));
            return templateRoot;
        }

        public string TargetPath(string folder)
        {
            var appPath = Path.Combine(_tenant.AppsRoot, folder);
            return Environment.MapPath(appPath);
        }

        #region stuff we need for Import

        public void MapExistingFilesToImportSet(Dictionary<int, string> filesAndPaths, Dictionary<int, int> fileIdMap)
        {
            Log.Add($"will map files - map-size:{fileIdMap.Count}");
            var portalId = _tenant.Id;

            var fileManager = FileManager.Instance;
            var folderManager = FolderManager.Instance;

            foreach (var file in filesAndPaths)
            {
                var fileId = file.Key;
                var relativePath = file.Value;

                var fileName = Path.GetFileName(relativePath);
                var directory = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
                if (directory == null) continue;

                if (!folderManager.FolderExists(portalId, directory))
                    continue;

                var folderInfo = folderManager.GetFolder(portalId, directory);

                if (!fileManager.FileExists(folderInfo, fileName))
                    continue;

                var fileInfo = fileManager.GetFile(folderInfo, fileName);
                fileIdMap.Add(fileId, fileInfo.FileId);
            }
        }

        public void CreateFoldersAndMapToImportIds(Dictionary<int, string> foldersAndPath, Dictionary<int, int> folderIdCorrectionList, List<Message> importLog)
        {
            Log.Add("create folder and map IDs - start");
            var portalId = _tenant.Id;

            var folderManager = FolderManager.Instance;

            foreach (var file in foldersAndPath)
            {
                try
                {
                    if (String.IsNullOrEmpty(file.Value)) continue;
                    var directory = Path.GetDirectoryName(file.Value)?.Replace('\\', '/');
                    if (directory == null) continue;
                    // if not exist, create - important because we need for metadata assignment
                    var folderInfo = (!folderManager.FolderExists(portalId, directory))
                        ? folderManager.AddFolder(portalId, directory)
                        : folderManager.GetFolder(portalId, directory);

                    folderIdCorrectionList.Add(file.Key, folderInfo.FolderID);
                }
                catch (Exception)
                {
                    importLog.Add(
                        new Message(
                            "Had a problem with folder id '" + file.Key + "' path '" + file.Value +
                            "' - you'll have to figure out yourself it this is a problem",
                            Message.MessageTypes.Warning));
                }
            }
            Log.Add("create folder and map IDs - completed");
        }

        public string ModuleVersion => Settings.ModuleVersion;

        public string FallbackContentTypeScope => Settings.AttributeSetScope;

        public SaveOptions SaveOptions(int zoneId) => new SaveOptions(DefaultLanguage, new ZoneRuntime(zoneId, Log).Languages(true));

        #endregion

    }
}