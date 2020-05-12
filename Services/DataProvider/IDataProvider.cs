using System.Collections.Generic;
using System.Threading.Tasks;
using sm_coding_challenge.Models;

namespace sm_coding_challenge.Services.DataProvider
{
    public interface IDataProvider
    {
        Task<PlayerModel> GetPlayerById(string id);
        Task<IEnumerable<PlayerModel>> GetAllPlayers();
        Task<IEnumerable<PlayerModel>> GetMutiplePlayerByThierIds(IEnumerable<string> ids);
        Task<IEnumerable<PlayerModel>> GetLatestPlayerByThierIds(IEnumerable<string> ids);
    }
}
