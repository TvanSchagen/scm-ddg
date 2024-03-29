﻿using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using Bogus.Extensions.UnitedStates;

namespace DummyDataGenerator.Utils
{

	public class Product
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string EANCode { get; set; }
		public string Category { get; set; }
		public string SubCategory { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
	}

	public class Activity
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
	}

	public class Organization
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string EmployerIdentificationNumber { get; set; }
		public int NumberOfEmployees { get; set; }
		public string EmailAddress { get; set; }
		public string WebsiteURL { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
	}

	public class Location
	{
		public Guid GUID { get; set; }
		public string Country { get; set; }
		public string PostalCode { get; set; }
		public string Province { get; set; }
		public string City { get; set; }
		public string Street { get; set; }
		public double Latitude { get; set; }
		public double Longtitude { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
	}

	public class Ingredient
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
	}

	public class RawMaterial
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
	}

	public class Packaging
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
	}

	class FakeDataHelper
	{
		// we define a seed to use for each generated list, to make sure the data we generate is the same each time, useful for benchmarking purposes
		const int SEED = 1234;

		public static List<Organization> GenerateOrganizations(int number)
		{
			Randomizer.Seed = new Random(SEED);
			var organization = new Faker<Organization>()
				.StrictMode(false)
				.CustomInstantiator(o => new Organization())
				.Rules((o, a) =>
				{
					a.GUID = o.Random.Guid();
					a.Name = o.Company.CompanyName();
					a.Description = o.PickRandom(new string[] { o.Lorem.Paragraph(), null });
					a.EmployerIdentificationNumber = o.Company.Ein();
					a.NumberOfEmployees = o.Random.Number(1, 10000);
					a.EmailAddress = o.Internet.Email();
					a.WebsiteURL = o.PickRandom(new string[] { o.Internet.Url(), null });
					a.CreatedDate = o.Date.Past(5);
					a.LastUpdatedDate = o.Date.Recent(90);
				});

			var organizations = organization.Generate(number);
			return organizations;
		}

		public static List<Product> GenerateProducts(int number)
		{
			Randomizer.Seed = new Random(SEED);
			var product = new Faker<Product>()
				.StrictMode(false)
				.CustomInstantiator(o => new Product())
				.Rules((o, a) =>
				{
					a.GUID = o.Random.Guid();
					a.Name = o.Commerce.ProductName();
					a.EANCode = o.Commerce.Ean13();
					a.Category = o.Commerce.Categories(1)[0];
					a.SubCategory = o.Commerce.Categories(1)[0];
					a.Description = o.PickRandom(new string[] { o.Lorem.Paragraph(), null });
					a.CreatedDate = o.Date.Past(5);
					a.LastUpdatedDate = o.Date.Recent(90);
				});

			var products = product.Generate(number);
			return products;
		}

		public static List<Activity> GenerateActivities(int number)
		{
			Randomizer.Seed = new Random(SEED);
			var activity = new Faker<Activity>()
				.StrictMode(false)
				.CustomInstantiator(o => new Activity())
				.Rules((o, a) =>
				{
					a.GUID = o.Random.Guid();
					a.Name = o.Hacker.IngVerb();
					a.Description = o.PickRandom(new string[] { o.Lorem.Paragraph(), null });
					a.CreatedDate = o.Date.Past(5);
					a.LastUpdatedDate = o.Date.Recent(90);
				});

			var activities = activity.Generate(number);
			return activities;
		}

		public static List<Location> GenerateLocations(int number)
		{
			Randomizer.Seed = new Random(SEED);
			var location = new Faker<Location>()
				.StrictMode(false)
				.CustomInstantiator(o => new Location())
				.Rules((o, a) =>
				{
					a.GUID = o.Random.Guid();
					a.Country = o.Address.Country();
					a.PostalCode = o.Address.ZipCode();
					a.Province = o.Address.County();
					a.City = o.Address.City();
					a.Street = o.Address.StreetAddress();
					a.Longtitude = o.Address.Longitude();
					a.Latitude = o.Address.Latitude();
					a.CreatedDate = o.Date.Past(5);
					a.LastUpdatedDate = o.Date.Recent(90);

				});

			var locations = location.Generate(number);
			return locations;
		}

	}
}
