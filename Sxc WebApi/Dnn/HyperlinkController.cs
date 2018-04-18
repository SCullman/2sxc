﻿using System;
using System.Web.Http;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Web.Api;
using ToSic.Eav.Implementations.ValueConverter;
using ToSic.Eav.Security.Permissions;
using ToSic.SexyContent.Environment.Dnn7.EavImplementation;
using ToSic.SexyContent.WebApi.Permissions;

namespace ToSic.SexyContent.WebApi.Dnn
{
	[SupportedModules("2sxc,2sxc-app")]
	public class HyperlinkController : SxcApiControllerBase
	{

		[HttpGet]
		[DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
		public object GetFileByPath(string relativePath)
		{
		    var context = GetContext(SxcInstance, Log);
            relativePath = relativePath.Replace(context.Dnn.Portal.HomeDirectory, "");
			var file = FileManager.Instance.GetFile(context.Dnn.Portal.PortalId, relativePath);
			if (CanUserViewFile(file))
				return new
				{
				    file.FileId
				};

			return null;
		}

		[HttpGet]
		[DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
		public string ResolveHyperlink(string hyperlink)
		{
		    var permCheck = new AppAndPermissions(SxcInstance, SxcInstance.App.AppId, Log);

		    permCheck.EnsureOrThrow(GrantSets.WriteSomething);

            var conv = new DnnValueConverter();
		    var fullLink = conv.Convert(ConversionScenario.GetFriendlyValue, "Hyperlink", hyperlink);
		    
            // if the user may only create drafts, then he/she may only see stuff from the adam folder
		    //var permCheck = set;
		    if (permCheck.Permissions.UserMay(GrantSets.WritePublished))
                return fullLink;

		    return !(fullLink.IndexOf("/adam/", StringComparison.Ordinal) > 0) 
                ? hyperlink 
                : fullLink;
		}

		private static bool CanUserViewFile(IFileInfo file)
		{
		    if (file == null) return false;
		    var folder = (FolderInfo)FolderManager.Instance.GetFolder(file.FolderId);
		    return FolderPermissionController.CanViewFolder(folder);
		}

	}
}