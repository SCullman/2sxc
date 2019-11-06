﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using ToSic.Eav.Apps;
using ToSic.Eav.Data;
using ToSic.Eav.Data.Builder;
using ToSic.Eav.ImportExport.Json.Format;
using ToSic.Eav.Interfaces;
using ToSic.Eav.Logging;
using ToSic.Eav.Logging.Simple;
using ToSic.Eav.WebApi.Formats;
using IEntity = ToSic.Eav.Data.IEntity;

namespace ToSic.SexyContent.WebApi.EavApiProxies
{
    internal class SaveDataValidator: ValidatorBase
    {
        public AllInOne Package;
        internal AppRuntime AppRead;

        public SaveDataValidator(AllInOne package, ILog parentLog) 
            : base("Val.Save", parentLog, "start save validator", nameof(SaveDataValidator))
        {
            Package = package;
        }

        public void PrepareForEntityChecks(AppRuntime appRead) => AppRead = appRead;

        internal bool ContainsOnlyExpectedNodes(out HttpResponseException preparedException)
        {
            var wrapLog = Log.Call(nameof(ContainsOnlyExpectedNodes));
            if (Package.ContentTypes != null)
                Add("package contained content-types, unexpected!");
            if (Package.InputTypes != null)
                Add("package contained input types, unexpected!");
            if (Package.Features != null)
                Add("package contained features, unexpected!");

            // check that items are mostly intact
            if (Package.Items == null || Package.Items.Count == 0)
                Add("package didn't contain items, unexpected!");
            else
            {
                // do various validity tests on items
                VerifyAllGroupAssignmentsValid(Package.Items);
                ValidateEachItemInBundle(Package.Items);
            }

            var ok= BuildExceptionIfHasIssues(out preparedException, "ContainsOnlyExpectedNodes() done");
            wrapLog($"{ok}");
            return ok;
        }

        /// <summary>
        /// Do various validity checks on each item
        /// </summary>
        private void ValidateEachItemInBundle(IList<BundleWithHeader<JsonEntity>> list)
        {
            var wrapLog = Log.Call(nameof(ValidateEachItemInBundle), $"{list.Count}");
            foreach (var item in list)
            {
                if (item.Header == null || item.Entity == null)
                    Add($"item {list.IndexOf(item)} header or entity is missing");
                else if(item.Header.Guid != item.Entity.Guid) // check this first (because .Group may not exist)
                {
                    if(item.Header.Group == null)
                        Add($"item {list.IndexOf(item)} has guid mismatch on header/entity, and doesn't have a group");
                    else if (!item.Header.Group.SlotIsEmpty)
                        Add($"item {list.IndexOf(item)} header / entity guid miss match");
                    // otherwise we're fine
                }
            }

            wrapLog("done");
        }

        /// <summary>
        /// ensure all want to save to the same assignment type - either in group or not!
        /// </summary>
        private void VerifyAllGroupAssignmentsValid(IReadOnlyCollection<BundleWithHeader<JsonEntity>> list)
        {
            var wrapLog = Log.Call(nameof(VerifyAllGroupAssignmentsValid), $"{list.Count}");
            var groupAssignments = list.Select(i => i.Header.Group).Where(g => g != null).ToList();
            if (groupAssignments.Count == 0)
            {
                wrapLog("none of the items is part of a list/group");
                return;
            }

            if (groupAssignments.Count != list.Count)
                Add($"Items in package with group: {groupAssignments} " +
                    $"- should be 0 or {list.Count} (items in list) " +
                    "- must stop, never expect items to come from different sources");
            else
            {
                var firstInnerContentAppId = groupAssignments.First().ContentBlockAppId;
                if (list.Any(i => i.Header.Group.ContentBlockAppId != firstInnerContentAppId))
                    Add("not all items have the same Group.ContentBlockAppId - this is required when using groups");
            }

            wrapLog("done");
        }


        internal bool EntityIsOk(int count, IEntity newEntity, out HttpResponseException preparedException)
        {
            var wrapLog = Log.Call<bool>(nameof(EntityIsOk));
            if (newEntity == null)
            {
                Add($"entity {count} couldn't deserialize");
                var notOk = BuildExceptionIfHasIssues(out preparedException);
                return wrapLog("newEntity is null", notOk);
            }

            if (newEntity.Attributes.Count == 0)
                Add($"entity {count} doesn't have attributes (or they are invalid)");

            var ok = BuildExceptionIfHasIssues(out preparedException, "EntityIsOk() done");
            return wrapLog("", ok);
        }

        internal bool IfUpdateValidateAndCorrectIds(int count, IEntity newEntity, out HttpResponseException preparedException)
        {
            var wrapLog = Log.Call(nameof(IfUpdateValidateAndCorrectIds));
            var previousEntity = AppRead.Entities.Get(newEntity.EntityId)
                                 ?? AppRead.Entities.Get(newEntity.EntityGuid);

            if (previousEntity != null)
            {
                Log.Add("found previous entity, will check types/ids/attributes");
                CompareTypes(count, previousEntity, newEntity);

                // for saving, ensure we are using the DB entity-ID 
                if (newEntity.EntityId == 0)
                {
                    Log.Add("found existing entity - will set the ID to that to overwrite");
                    newEntity.ResetEntityId(previousEntity.EntityId);
                }

                CompareIdentities(count, previousEntity, newEntity);
                CompareAttributes(count, previousEntity, newEntity);
            }
            else
                Log.Add("no previous entity found");

            var ok = BuildExceptionIfHasIssues(out preparedException, "EntityIsOk() done");

            wrapLog($"{ok}");
            return ok;
        }


        private void CompareTypes(int count, IEntityLight originalEntity, IEntityLight newEntity)
        {
            var wrapLog = Log.Call(nameof(CompareTypes), $"ids:{newEntity.Type.StaticName}/{originalEntity.Type.StaticName}");
            if(originalEntity.Type.StaticName != newEntity.Type.StaticName)
                Add($"entity type mismatch on {count}");
            wrapLog("done");
        }

        private void CompareIdentities(int count, IEntityLight originalEntity, IEntityLight newEntity)
        {
            var wrapLog = Log.Call(nameof(CompareIdentities), $"ids:{newEntity.EntityId}/{originalEntity.EntityId}");
            if(originalEntity.EntityId != newEntity.EntityId)
                Add($"entity ID mismatch on {count} - {newEntity.EntityId}/{originalEntity.EntityId}");

            Log.Add($"Guids:{newEntity.EntityGuid}/{originalEntity.EntityGuid}");
            if(originalEntity.EntityGuid != newEntity.EntityGuid)
                Add($"entity GUID mismatch on {count} - {newEntity.EntityGuid}/{originalEntity.EntityGuid}");
            wrapLog("done");
        }

        private void CompareAttributes(int count, IEntity original, IEntity ent)
        {
            var wrapLog = Log.Call(nameof(CompareAttributes));

            if (original.Attributes.Count != ent.Attributes.Count)
                Add($"entity {count} has different amount " +
                    $"of attributes {ent.Attributes.Count} " +
                    $"than the original {original.Attributes.Count}");
            else
                foreach (var origAttr in original.Attributes)
                {
                    var newAttr = ent.Attributes.FirstOrDefault(a => a.Key == origAttr.Key);
                    if (newAttr.Equals(default(KeyValuePair<string, IAttribute>)))
                        Add($"attribute {origAttr.Key} not found in save");
                    else if (origAttr.Value.Type != newAttr.Value.Type)
                        Add($"found different type on attribute {origAttr.Key} " +
                            $"- '{origAttr.Value.Type}'/'{newAttr.Value.Type}'");
                }

            wrapLog("done");
        }
    }
}