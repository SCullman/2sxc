using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Oqtane.Modules;
using Oqtane.Services;
using Oqtane.Shared;

namespace ToSic.Sxc.Oqt.Client.Services
{
    public class SxcService : ServiceBase, ISxcService, IService
    {
        private readonly SiteState _siteState;

        public SxcService(HttpClient http, SiteState siteState) : base(http)
        {
            _siteState = siteState;
        }

         private string Apiurl => CreateApiUrl(_siteState.Alias, "Sxc");

        public async Task<List<Shared.Models.Sxc>> GetSxcsAsync(int ModuleId)
        {
            List<Shared.Models.Sxc> Sxcs = await GetJsonAsync<List<Shared.Models.Sxc>>(CreateAuthorizationPolicyUrl($"{Apiurl}?moduleid={ModuleId}", ModuleId));
            return Sxcs.OrderBy(item => item.Name).ToList();
        }

        public async Task<Shared.Models.Sxc> GetSxcAsync(int SxcId, int ModuleId)
        {
            return await GetJsonAsync<Shared.Models.Sxc>(CreateAuthorizationPolicyUrl($"{Apiurl}/{SxcId}", ModuleId));
        }

        public async Task<Shared.Models.Sxc> AddSxcAsync(Shared.Models.Sxc Sxc)
        {
            return await PostJsonAsync<Shared.Models.Sxc>(CreateAuthorizationPolicyUrl($"{Apiurl}", Sxc.ModuleId), Sxc);
        }

        public async Task<Shared.Models.Sxc> UpdateSxcAsync(Shared.Models.Sxc Sxc)
        {
            return await PutJsonAsync<Shared.Models.Sxc>(CreateAuthorizationPolicyUrl($"{Apiurl}/{Sxc.SxcId}", Sxc.ModuleId), Sxc);
        }

        public async Task DeleteSxcAsync(int SxcId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/{SxcId}", ModuleId));
        }
    }
}