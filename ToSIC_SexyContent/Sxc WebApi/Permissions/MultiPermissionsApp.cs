﻿using System.Collections.Generic;
using System.Web.Http;
using DotNetNuke.Entities.Portals;
using ToSic.Eav.Apps;
using ToSic.Eav.Apps.Interfaces;
using ToSic.Eav.Data;
using ToSic.Eav.Interfaces;
using ToSic.Eav.Logging;
using ToSic.Eav.Logging.Simple;
using ToSic.Eav.Security.Permissions;
using ToSic.SexyContent.DataSources;
using ToSic.SexyContent.Environment.Dnn7;
using ToSic.SexyContent.WebApi.Errors;
using Factory = ToSic.Eav.Factory;
using IEntity = ToSic.Eav.Data.IEntity;

namespace ToSic.SexyContent.WebApi.Permissions
{
    internal class MultiPermissionsApp: MultiPermissionsBase
    {
        /// <summary>
        /// The current app which will be used and can be re-used externally
        /// </summary>
        public App App { get; }

        internal readonly SxcInstance SxcInstance;

        protected readonly PortalSettings PortalForSecurityCheck;

        protected readonly bool SamePortal;

        public MultiPermissionsApp(SxcInstance sxcInstance, int appId, ILog parentLog) :
            this(sxcInstance, SystemRuntime.ZoneIdOfApp(appId), appId, parentLog) { }

        protected MultiPermissionsApp(SxcInstance sxcInstance, int zoneId, int appId, ILog parentLog) 
            : base("Api.Perms", parentLog)
        {
            var wrapLog = Log.New("AppAndPermissions", $"..., appId: {appId}, ...");
            SxcInstance = sxcInstance;
            var tenant = new DnnTenant(PortalSettings.Current);
            var environment = Factory.Resolve<IEnvironmentFactory>().Environment(Log);
            var contextZoneId = environment.ZoneMapper.GetZoneId(tenant.Id);
            App = new App(tenant, zoneId, appId, 
                ConfigurationProvider.Build(sxcInstance, true),
                false, Log);
            SamePortal = contextZoneId == zoneId;
            PortalForSecurityCheck = SamePortal ? PortalSettings.Current : null;
            wrapLog($"ready for z/a:{zoneId}/{appId} t/z:{tenant.Id}/{contextZoneId} same:{SamePortal}");
        }

        protected override Dictionary<string, IPermissionCheck> InitializePermissionChecks()
            => InitPermissionChecksForApp();

        protected Dictionary<string, IPermissionCheck> InitPermissionChecksForApp()
            => new Dictionary<string, IPermissionCheck>
            {
                {"App", BuildPermissionChecker()}
            };

        public sealed override bool ZoneIsOfCurrentContextOrUserIsSuper(out HttpResponseException exp)
        {
            var wrapLog = Log.Call("ZoneChangedAndNotSuperUser()");
            var zoneSameOrSuperUser = SamePortal || PortalSettings.Current.UserInfo.IsSuperUser;
            exp = zoneSameOrSuperUser ? null: Http.PermissionDenied(
                $"accessing app {App.AppId} in zone {App.ZoneId} is not allowed for this user");

            wrapLog(zoneSameOrSuperUser ? $"sameportal:{SamePortal} - ok": "not ok, generate error");

            return zoneSameOrSuperUser;
        }



        /// <summary>
        /// Creates a permission checker for an app
        /// Optionally you can provide a type-name, which will be 
        /// included in the permission check
        /// </summary>
        /// <returns></returns>
        protected IPermissionCheck BuildPermissionChecker(IContentType type = null, IEntity item = null)
        {
            Log.Add($"BuildPermissionChecker(type:{type?.Name}, item:{item?.EntityId})");

            // user has edit permissions on this app, and it's the same app as the user is coming from
            return new DnnPermissionCheck(Log,
                instance: SxcInstance?.EnvInstance,
                app: App,
                portal: PortalForSecurityCheck,
                targetType: type,
                targetItem: item);
        }

    }
}