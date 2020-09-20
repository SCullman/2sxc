﻿using System;
using System.Web.Http;
using DotNetNuke.Security;
using DotNetNuke.Web.Api;
using ToSic.SexyContent.WebApi;
using ToSic.Sxc.WebApi.FieldList;

namespace ToSic.Sxc.Dnn.WebApi.Cms
{
    public class ListController: SxcApiController
    {
        protected override string HistoryLogName => "Api.List";

        private FieldListBackend FieldBacked => Eav.Factory.Resolve<FieldListBackend>().Init(GetContext(), GetBlock(), Log);

        /// <summary>
        /// used to be GET Module/ChangeOrder
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="fields"></param>
        /// <param name="index"></param>
        /// <param name="toIndex"></param>
        [HttpPost]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        public void Move(Guid? parent, string fields, int index, int toIndex)
            => FieldBacked.ChangeOrder(parent, fields, index, toIndex);


        //[HttpGet]
        //[DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        //public bool Publish(string part, int index) 
        //    => FieldBacked.Publish(part, index);

        /// <summary>
        /// Used to be Get Module/RemoveFromList
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="fields"></param>
        /// <param name="index"></param>
        [HttpDelete]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        public void Delete(Guid? parent, string fields, int index)
            => FieldBacked.Remove(parent, fields, index);

    }
}