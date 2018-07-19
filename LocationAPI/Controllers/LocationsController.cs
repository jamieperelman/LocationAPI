using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Http;
using System.Web.Http.Description;
using LocationAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


// TO DO: write other attribute routes, get weather based on other attributes

namespace LocationAPI.Controllers
{
    public class LocationsController : ApiController
    {
        private LocationAPIContext db = new LocationAPIContext();
        private HttpClient client = new HttpClient();

        //TO DO: probably should go in a config file somewhere? WebAPIConfig? 
        static string apiUrl = "http://api.openweathermap.org/data/2.5";
        static string apiKey = "baa7fa20fe5c08123faafe2a5aeef165";
        static string units = "metric";

        // GET: api/Locations
        public IQueryable<Location> GetLocations()
        {
            return db.Locations;
        }

        // GET: api/Locations/5        
        [ResponseType(typeof(Location))]
        public async Task<IHttpActionResult> GetLocation(int id)
        {
            Location location = await db.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            return Ok(location);
        }

        // GET: api/Locations/5/weather
        [Route("api/locations/{id:int}/weather")]
        [ResponseType(typeof(Location))]
        public async Task<IHttpActionResult> GetWeather(int id)
        {
            //weather JSON object
            JObject weatherObj = new JObject();

            //get location object - if location is not in db, declare not found
            Location location = await db.Locations.FindAsync(id);           
            if (location == null)
            {
                return NotFound();
            }

            //key to identify correct weather in cache
            string cacheKey = $"{id},{location.Name},{location.Country}";

            //check cache first
            if (HttpContext.Current.Cache[cacheKey] != null)
            {
                System.Diagnostics.Debug.WriteLine($"Getting <{cacheKey}> from cache...");
                weatherObj = HttpContext.Current.Cache[$"{cacheKey}"] as JObject;                
            }
            else
            {
                //not in cache - pull name out of location
                System.Diagnostics.Debug.WriteLine($"Getting <{id}> from API...");                               
                
                //set headers to return json
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                //first get current weather - TO DO: should encode location name/country? 
                System.Diagnostics.Debug.WriteLine($"Getting current weather <{cacheKey}>...");                                              
                var response = await client.GetAsync($"{apiUrl}/weather?q={location.Name},{location.Country}&units={units}&APPID={apiKey}");
                //TO DO: retry a couple of times on failure, spaced out by second and then 5 seconds?                 
                weatherObj["current"] = JObject.Parse(await response.Content.ReadAsStringAsync());

                //then get forcast and merge together for one Json Object to return
                //need to check previous response does not have error - or deal with error later after trying both?
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Getting future forecast <{id}>...");
                    response = await client.GetAsync($"{apiUrl}/forecast?q={location.Name},{location.Country}&units={units}&APPID={apiKey}");
                    weatherObj["future"] = (JObject.Parse(await response.Content.ReadAsStringAsync()));

                    //put weather object into cache - key is location id
                    //do not cache if failure
                    if (response.IsSuccessStatusCode)
                    {
                        HttpContext.Current.Cache.Insert($"{cacheKey}", weatherObj, null, DateTime.Now.AddMinutes(5), Cache.NoSlidingExpiration);
                    }
                }  

                //double check that if either response was unsuccessful, we pass that message on.
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"There was an error getting weather data for location <{cacheKey}> <{response.StatusCode}>");
                    return ResponseMessage(response);
                }

            }

            //if weatherObj is null, return not found            
            if (weatherObj == null)
            {
                return NotFound();
            }
            
            return Ok(weatherObj);
        }
    
        // PUT: api/Locations/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutLocation(int id, Location location)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != location.Id)
            {
                return BadRequest();
            }

            db.Entry(location).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LocationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Locations
        [ResponseType(typeof(Location))]
        public async Task<IHttpActionResult> PostLocation(Location location)
        {
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Locations.Add(location);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = location.Id }, location);
        }

        // DELETE: api/Locations/5
        [ResponseType(typeof(Location))]
        public async Task<IHttpActionResult> DeleteLocation(int id)
        {
            Location location = await db.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }
            
            db.Locations.Remove(location);
            await db.SaveChangesAsync();

            //remove weather from cache if there
            string cacheKey = $"{id},{location.Name},{location.Country}";
            if (HttpContext.Current.Cache[cacheKey] != null)
            {
                HttpContext.Current.Cache.Remove(cacheKey);
            }

            return Ok(location);
        }
        

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                //client dispose? 
            }
            base.Dispose(disposing);
        }

        private bool LocationExists(int id)
        {
            return db.Locations.Count(e => e.Id == id) > 0;
        }
    }
}