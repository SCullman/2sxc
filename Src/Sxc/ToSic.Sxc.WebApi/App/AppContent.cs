﻿using System;
using System.Collections.Generic;
using System.Linq;
using ToSic.Eav;
using ToSic.Eav.Data;
using ToSic.Eav.Security;
using ToSic.Eav.Security.Permissions;
using ToSic.Eav.WebApi;
using ToSic.Eav.WebApi.Errors;
using ToSic.Eav.WebApi.Security;
using ToSic.Sxc.Apps;
using ToSic.Sxc.Conversion;

namespace ToSic.Sxc.WebApi.App
{
    internal class AppContent: BlockWithAppWebApiBackendBase<AppContent>
    {
        #region Constructor / DI

        public AppContent() : base("Sxc.ApiApC") { }

        #endregion


        #region Get Items

        internal IEnumerable<Dictionary<string, object>> GetItems(string contentType, string appPath = null)
        {
            var wrapLog = Log.Call($"get entities type:{contentType}, path:{appPath}");

            // if app-path specified, use that app, otherwise use from context
            var appIdentity = AppFinder.GetAppIdFromPathOrContext(appPath, _block);

            // get the app - if we have the context from the request, use that, otherwise generate full app
            var app = _block == null
                ? Factory.Resolve<Apps.App>().Init(appIdentity, Log)
                : GetApp(appIdentity.AppId, _block);

            // verify that read-access to these content-types is permitted
            var permCheck = ThrowIfNotAllowedInType(contentType, GrantSets.ReadSomething, app);

            var result = new EntityApi(appIdentity.AppId, permCheck.EnsureAny(GrantSets.ReadDraft), Log)
                .GetEntities(contentType)
                ?.ToList();
            wrapLog("found: " + result?.Count);
            return result;
        }


        #endregion

        #region Get One 

        /// <summary>
        /// Preprocess security / context, then get the item based on an passed in method, 
        /// ...then process/finish
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, object> GetOne(string contentType, Func<EntityApi, IEntity> getOne, string appPath)
        {
            Log.Add($"get and serialize after security check type:{contentType}, path:{appPath}");
            // if app-path specified, use that app, otherwise use from context
            var appIdentity = AppFinder.GetAppIdFromPathOrContext(appPath, _block);

            var entityApi = new EntityApi(appIdentity.AppId, true, Log);

            var itm = getOne(entityApi);
            var permCheck = ThrowIfNotAllowedInItem(itm, GrantSets.ReadSomething, GetApp(appIdentity.AppId, _block));

            // in case draft wasn't allow, get again with more restricted permissions 
            if (!permCheck.EnsureAny(GrantSets.ReadDraft))
            {
                entityApi = new EntityApi(appIdentity.AppId, false, Log);
                itm = getOne(entityApi);
            }

            return InitEavAndSerializer(appIdentity.AppId, _block?.EditAllowed ?? false).Convert(itm);
        }


        #endregion

        #region CreateOrUpdate


        internal Dictionary<string, object> CreateOrUpdate(string contentType, Dictionary<string, object> newContentItem, int? id = null, string appPath = null)
        {
            Log.Add($"create or update type:{contentType}, id:{id}, path:{appPath}");
            // if app-path specified, use that app, otherwise use from context
            var appIdentity = AppFinder.GetAppIdFromPathOrContext(appPath, _block);

            // Check that this ID is actually of this content-type,
            // this throws an error if it's not the correct type
            var itm = id == null
                ? null
                : new EntityApi(appIdentity.AppId, true, Log).GetOrThrow(contentType, id.Value);

            var realApp = GetApp(appIdentity.AppId, _block);
            if (itm == null) ThrowIfNotAllowedInType(contentType, Grants.Create.AsSet(), realApp);
            else ThrowIfNotAllowedInItem(itm, Grants.Update.AsSet(), realApp);

            // Convert to case-insensitive dictionary just to be safe!
            newContentItem = new Dictionary<string, object>(newContentItem, StringComparer.OrdinalIgnoreCase);

            // Now create the cleaned up import-dictionary so we can create a new entity
            var cleanedNewItem = new AppContentEntityBuilder(Log)
                .CreateEntityDictionary(contentType, newContentItem, appIdentity.AppId);

            var userName = _context.User.IdentityToken;

            if (id == null)
            {
                var entity = realApp.Data.Create(contentType, cleanedNewItem, userName);
                id = entity.EntityId;
            }
            else
                realApp.Data.Update(id.Value, cleanedNewItem, userName);

            return InitEavAndSerializer(appIdentity.AppId, _block?.EditAllowed ?? false)
                .Convert(realApp.Data.List.One(id.Value));
        }

        #endregion
        
        #region helpers / initializers to prep the EAV and Serializer

        private Eav.Conversion.EntitiesToDictionary InitEavAndSerializer(int appId, bool userMayEdit)
        {
            Log.Add($"init eav for a#{appId}");
            // Improve the serializer so it's aware of the 2sxc-context (module, portal etc.)
            var ser = Eav.WebApi.Helpers.Serializers.GetSerializerWithGuidEnabled();
            ((DataToDictionary)ser).WithEdit = userMayEdit;
            return ser;
        }
        #endregion


        #region Delete

        internal void Delete(string contentType, int id, string appPath)
        {
            Log.Add($"delete id:{id}, type:{contentType}, path:{appPath}");
            // if app-path specified, use that app, otherwise use from context
            var appId = AppFinder.GetAppIdFromPathOrContext(appPath, _block);

            // don't allow type "any" on this
            if (contentType == "any")
                throw new Exception("type any not allowed with id-only, requires guid");

            var entityApi = new EntityApi(appId.AppId, true, Log);
            var itm = entityApi.GetOrThrow(contentType, id);
            ThrowIfNotAllowedInItem(itm, Grants.Delete.AsSet(), GetApp(appId.AppId, _block));
            entityApi.Delete(itm.Type.Name, id);
        }

        internal void Delete(string contentType, Guid guid, string appPath)
        {
            Log.Add($"delete guid:{guid}, type:{contentType}, path:{appPath}");
            // if app-path specified, use that app, otherwise use from context
            var appIdentity = AppFinder.GetAppIdFromPathOrContext(appPath, _block);

            var entityApi = new EntityApi(appIdentity.AppId, true, Log);
            var itm = entityApi.GetOrThrow(contentType == "any" ? null : contentType, guid);

            ThrowIfNotAllowedInItem(itm, Grants.Delete.AsSet(), GetApp(appIdentity.AppId, _block));

            entityApi.Delete(itm.Type.Name, guid);
        }


        #endregion

        #region Permission Checks

        protected MultiPermissionsTypes ThrowIfNotAllowedInType(string contentType, List<Grants> requiredGrants, IApp alternateApp = null)
        {
            var permCheck = new MultiPermissionsTypes().Init(_context, alternateApp ?? _block.App, contentType, Log);
            if (!permCheck.EnsureAll(requiredGrants, out var error))
                throw HttpException.PermissionDenied(error);
            return permCheck;
        }

        protected MultiPermissionsItems ThrowIfNotAllowedInItem(IEntity itm, List<Grants> requiredGrants, IApp alternateApp = null)
        {
            var permCheck = new MultiPermissionsItems().Init(_context, alternateApp ?? _block.App, itm, Log);
            if (!permCheck.EnsureAll(requiredGrants, out var error))
                throw HttpException.PermissionDenied(error);
            return permCheck;
        }

        #endregion
    }
}
