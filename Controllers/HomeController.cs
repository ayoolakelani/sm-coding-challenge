using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using sm_coding_challenge.Models;
using sm_coding_challenge.Services.DataProvider;

namespace sm_coding_challenge.Controllers
{


    public class HomeController : Controller
    {


        private IDataProvider _dataProvider;
        public HomeController(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }



        [ResponseCache(Duration = 6000, VaryByQueryKeys = new[] { "*" })]  //cache pages to reduce mutiple hits to server
        public async Task<IActionResult> Index()
        {
            var  Players = (await _dataProvider.GetAllPlayers()).Select(i => i.Id);
            var PlayerIds = string.Join(",", Players);
            ViewBag.PlayerIds = PlayerIds;
            return View();
        }


        [HttpGet]
        [Route("player")] //added routing to better manage
        [ResponseCache(Duration = 6000, VaryByQueryKeys = new[] { "*" })]  //cache pages to reduce mutiple hits to server
        public async Task<IActionResult> GetPlayerAysnc([FromQuery] string id)  //converted to async await
        {
            try  //handle all unhandled exceptions
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var player = await _dataProvider.GetPlayerById(id);

                    if (player != null)
                        return Json(player);
                    else
                        return Json(new { });
                }
                else
                    return Json(new { });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occured " });
            }
        }

        [HttpGet]
        [ResponseCache(VaryByQueryKeys = new[] { "*" }, Duration =  6000)]  //cache pages to reduce mutiple hits to server for 1 hour 
        [Route("players")]
        public async Task<IActionResult> GetPlayersAsync([FromQuery]string ids)
        {
            try
            {
                if (!string.IsNullOrEmpty(ids))
                {
                    var playerIds = ids.Split(',');
                    var returnList = await _dataProvider.GetMutiplePlayerByThierIds(playerIds.Distinct());  //pass mutiple ids and query at once rather than call for each
                    if(returnList != null && returnList.Any())
                    return Json(returnList);
                    else
                        return Json(new { });
                }
                else
                    return Json(new { });
         
                return Json(new { });
        }
            catch(Exception)
            {
                return BadRequest(new { message = "An error occured " });
            }
        }

        [HttpGet]
        [Route("latest")]

        public async Task<IActionResult> LatestPlayersAsync(string ids)
        {
            try
            {
                if (!string.IsNullOrEmpty(ids))
                {
                    var playerIds = ids.Split(',');
                    var returnList = await _dataProvider.GetLatestPlayerByThierIds(playerIds.Distinct());  //pass mutiple ids and query at once rather than call for each
                    if (returnList != null && returnList.Any())
                        return Json(returnList);
                    else
                        return Json(new { });
                }
                else
                    return Json(new { });

               
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occured " });
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
