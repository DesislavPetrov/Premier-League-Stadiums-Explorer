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
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Common.Enums;
using GoogleApi.Entities.Places.Details.Request;
using GoogleApi.Entities.Places.Photos.Request;
using GoogleApi.Entities.Places.Search.NearBy.Request;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace PremierLeagueStadiumsExplorer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public string MapboxAccessToken { get; }
        public string GoogleApiKey { get; }

        public IndexModel(IConfiguration configuration, IHostingEnvironment  hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            MapboxAccessToken = configuration["Mapbox:AccessToken"];
            GoogleApiKey = configuration["google:ApiKey"];
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
                    string club = reader.GetField<string>(2);
                    double latitude = reader.GetField<double>(3);
                    double longitude = reader.GetField<double>(4);

                    featureCollection.Features.Add(new Feature(
                        new Point(new Position(latitude, longitude)),
                        new Dictionary<string, object>
                        {
                            { "name", name},
                            { "club", club}
                        }));
                }
                return new JsonResult(featureCollection);
            }            
        }

        public async Task<IActionResult> OnGetStadiumDetail (string name, double latitude, double longitude)
        {
            var stadiumDetail = new StadiumDetail();

            // Execute the search request 
            var searchResponse = await GooglePlaces.NearBySearch.QueryAsync(new PlacesNearBySearchRequest
            {
                Key = GoogleApiKey,
                Name = name,
                Location = new Location(latitude, longitude),
                Radius = 1000
            });

            // If we did not get a good response, or the list of results are empty then get out of here
            if (!searchResponse.Status.HasValue || searchResponse.Status.Value != Status.Ok || !searchResponse.Results.Any())
            {
                return new BadRequestResult();
            }

            // Get the first result
            var nearbyResult = searchResponse.Results.FirstOrDefault();
            string placeId = nearbyResult.PlaceId;
            string photoReference = nearbyResult.Photos?.FirstOrDefault()?.PhotoReference;
            string photoCredit = nearbyResult.Photos?.FirstOrDefault()?.HtmlAttributions.FirstOrDefault();

            //Execute the details request
            var detailsResponse = await GooglePlaces.Details.QueryAsync(new PlacesDetailsRequest
            {
                Key = GoogleApiKey,
                PlaceId = placeId
            });

            // If we did not get a good response then get out of here
            if (!detailsResponse.Status.HasValue || detailsResponse.Status.Value != Status.Ok)
                return new BadRequestResult();

            // Set the details
            var detailsResult = detailsResponse.Result;
            stadiumDetail.FormattedAddress = detailsResult.FormattedAddress;
            stadiumDetail.PhoneNumber = detailsResult.InternationalPhoneNumber;
            stadiumDetail.Website = detailsResult.Website;

            if (photoReference != null)
            {
                // Execute the photo request
                var photosResponse = await GooglePlaces.Photos.QueryAsync(new PlacesPhotosRequest
                {
                    Key = GoogleApiKey,
                    PhotoReference = photoReference,
                    MaxWidth = 400
                });

                if (photosResponse.PhotoBuffer != null)
                {
                    stadiumDetail.Photo = Convert.ToBase64String(photosResponse.PhotoBuffer);
                    stadiumDetail.PhotoCredit = photoCredit;
                }
            }

            return new JsonResult(stadiumDetail);
        }
    }
}

