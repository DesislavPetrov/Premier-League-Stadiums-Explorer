using CsvHelper;
using CsvHelper.Configuration;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace PremierLeagueStadiumsExplorer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public string MapboxAccessToken { get; }

        public IndexModel(IConfiguration configuration, IHostingEnvironment  hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            MapboxAccessToken = configuration["Mapbox:AccessToken"];
        }

        public void OnGet()
        {
            
        }

        public IActionResult OnGetStadiums()
        {
            var configuration = new Configuration
            {
                BadDataFound = context =>
                {

                }
            };

            using (var sr = new StreamReader(Path.Combine(_hostingEnvironment.WebRootPath, "stadiums.dat")))
            using (var reader = new CsvReader(sr, configuration))
            {
                FeatureCollection featureCollection = new FeatureCollection();
                while (reader.Read())
                {
                    string name = reader.GetField<string>(1);
                    string city = reader.GetField<string>(2);
                    double latitude = reader.GetField<double>(3);
                    double longitude = reader.GetField<double>(4);

                    featureCollection.Features.Add(new Feature(
                        new Point(new Position(latitude, longitude)),
                        new Dictionary<string, object>
                        {
                            { "name", name},
                            { "city", city}
                        }));
                }
                return new JsonResult(featureCollection);
            }            
        }
    }
}

