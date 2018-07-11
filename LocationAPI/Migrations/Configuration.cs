namespace LocationAPI.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using LocationAPI.Models; 

    internal sealed class Configuration : DbMigrationsConfiguration<LocationAPI.Models.LocationAPIContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(LocationAPI.Models.LocationAPIContext context)
        {

            context.Locations.AddOrUpdate(x => x.Id,
                           new Location() { Id = 1, Name = "Hermosa Beach", Zipcode = "90254", State = "California", Country = "United States" },
                           new Location() { Id = 1, Name = "North Chesterfield", Zipcode = "23236", State = "Virginia", Country = "United States" },
                           new Location() { Id = 1, Name = "Wilmington", Zipcode = "19808", State = "Delaware", Country = "United States" },
                           new Location() { Id = 1, Name = "Hoboken", Zipcode = "07030", State = "New Jersey", Country = "United States" },
                           new Location() { Id = 1, Name = "Sparta", Zipcode = "07871", State = "New Jersey", Country = "United States" },
                           new Location() { Id = 1, Name = "Singapore", Zipcode = "439298", Country = "Singapore" }
                           );
        }
    }
}
