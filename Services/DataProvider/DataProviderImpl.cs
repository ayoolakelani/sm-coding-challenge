using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using sm_coding_challenge.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;

namespace sm_coding_challenge.Services.DataProvider
{
    public class DataProvider : IDataProvider
    {
        private JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }; //setting will be reused by other methods
        HttpClientHandler handler;
        private TimeSpan Timeout;
        private readonly ILogger<IDataProvider> _logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _config;

        public DataProvider(IConfiguration config, ILogger<IDataProvider> logger, IDistributedCache cache)
        {
            _logger = logger;
            _config = config;
            _cache = cache;
            handler = new HttpClientHandler{AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate};
            int defaultTimeOut = config.GetValue<int>("Settings:TimeOut");  //get default timeout from config
            Timeout = TimeSpan.FromSeconds(defaultTimeOut);
        }

        public async Task<IEnumerable<PlayerModel>> GetMutiplePlayerByThierIds(IEnumerable<string> ids)
        {


            try
            {
                var playerList = await GetAllPlayersAsync(); //get alllist from cache
                if (playerList != null && playerList.Any())
                {
                    return playerList.Where(p => ids.Contains(p.Id)); //find player in list from
                }
                return null;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(DataProvider)); //log exception for futher investigation;
                return null;
            }
       }

        public async Task<IEnumerable<PlayerModel>> GetLatestPlayerByThierIds(IEnumerable<string> ids)
        {
            try
            {
                var playerList = await GetAllPlayersAsync(ListType.LATEST_PLAYERS); //get alllist from cache
                if (playerList != null && playerList.Any())
                {
                    return playerList.Where(p => ids.Contains(p.Id)); //find player in list from
                }
                return null;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(DataProvider)); //log exception for futher investigation;
                return null;
            }
        }

        public async  Task<PlayerModel> GetPlayerById(string id)
        {
          
                try
                {
                    var playerList = await GetAllPlayersAsync(); //get list from cache
                    if(playerList!= null && playerList.Any())
                    {
                        return playerList.FirstOrDefault(p => p.Id == id); //find player in list from
                    }
                    return null;
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, nameof(DataProvider)); //log exception for futher investigation;
                return null;
                }
            
        
        }

        public async Task<IEnumerable<PlayerModel>> GetAllPlayers()
        {
            
            try
            {
                var players = await  GetAllPlayersAsync(); //get alllist from cache
                if (players != null)
                    return players;
                    return new PlayerModel[] { };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(DataProvider)); //log exception for futher investigation;
                return new PlayerModel[] { };  //return emptylist
            }
        }

        //utility cache method to get Player list   from cache  depending on type (ALL|LATEST)
        private async Task<IEnumerable<PlayerModel>> GetAllPlayersAsync(ListType listType = ListType.ALL_PLAYERS)
        {

           
            try
            {

                //get list of players in cache as it doesn't change regularly
                IEnumerable<PlayerModel> players = null;
                var listkey = listType == ListType.LATEST_PLAYERS ? "LatestPlayers" : "AllPlayers"; // get list key by listtype
                // await _cache.RemoveAsync(listkey);
                var playersString  = await  _cache.GetStringAsync(listkey);
                if(string.IsNullOrEmpty(playersString)) //if(no list in cache)
                {
                    //  get the endpoint key from AppSettings
                    var endpointKey = listType == ListType.LATEST_PLAYERS ? "LatestPlayersUrl" : "AllPlayersUrl";
                    var client = new HttpClient(handler);
                        
                              client.Timeout = Timeout;
                              //use interpolation to get the right Url 
                                var response = await client.GetAsync(_config.GetValue<string>($"Settings:{endpointKey}")); //endpoint should be configurable
                                if (response.IsSuccessStatusCode)
                                {
                                    var stringData = await response.Content.ReadAsStringAsync();
                                    var dataResponse = JsonConvert.DeserializeObject<DataResponseModel>(stringData, settings);
                                         // combin all playrs to one list and check for nullable lists
                                        var AllplayersList = Enumerable.Empty<PlayerModel>();
                                        if (dataResponse.Rushing != null)
                                         AllplayersList = AllplayersList.Concat(dataResponse.Rushing);
                                        if (dataResponse.Kicking != null)
                                            AllplayersList = AllplayersList.Concat(dataResponse.Kicking);

                                        if (dataResponse.Passing != null)
                                            AllplayersList = AllplayersList.Concat(dataResponse.Passing);

                                        if (dataResponse.Receiving != null)
                                            AllplayersList = AllplayersList.Concat(dataResponse.Receiving);  //ensure list is unique
                                        players = AllplayersList.Distinct(new PlayerComparer());
                                     int refreshDays = _config.GetValue<int>($"Settings:RefreshDays");
                                        var options = new DistributedCacheEntryOptions
                                        {
                                                 AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(refreshDays), //invalidate cache in 7 days
                                                   SlidingExpiration = TimeSpan.FromDays(1)  //invalidate if no reqeust in 1 day
                                        };
                                       //save list of players to cache;
                                   _ =  _cache.SetStringAsync(listkey, JsonConvert.SerializeObject(players), options); 
                                    
                                }
                  
                        
                    }
                    else
                    {
                             players =  JsonConvert.DeserializeObject<IEnumerable<PlayerModel>>(playersString, settings);
                            
                    }
                //return player list to caller
                return players;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(DataProvider)); //log exception for futher investigation;
                return null;
            }

        }

       


        //Helper class to help removes duplicates identify players
        public class PlayerComparer : IEqualityComparer<PlayerModel>
        {

            public bool Equals(PlayerModel x, PlayerModel y)
            {
                if (object.ReferenceEquals(x, y)) return true;

                if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
                    return false;

                return x.Id == y.Id;
            }
            public int GetHashCode(PlayerModel player)
            {
                if (Object.ReferenceEquals(player, null)) return 0;
                int hash = player.Id == null ? 0 : player.Id.GetHashCode();
                return hash;
            }

        }

        private enum ListType
        {
            ALL_PLAYERS,
            LATEST_PLAYERS

        }
        
    }
}
