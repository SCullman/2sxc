﻿using System;
using ToSic.Sxc.Context;

namespace ToSic.Sxc.WebApi.Adam
{
    /// <summary>
    /// Backend for the API
    /// Is meant to be transaction based - so create a new one for each thing as the initializers set everything for the transaction
    /// </summary>
    public class AdamTransGetItems<TFolderId, TFileId> : AdamTransactionBase<AdamTransGetItems<TFolderId, TFileId>, TFolderId, TFileId>, IAdamTransGetItems
    {
        public AdamTransGetItems(Lazy<AdamState<TFolderId, TFileId>> adamState, IContextResolver ctxResolver) : base(adamState, ctxResolver, "Adm.TrnItm") { }
    }
}
