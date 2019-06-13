using System;
using System.Collections.Generic;
using System.Text;
using Bogus;

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
		public DateTime DeletedDate { get; set; }
	}

	public class Activity
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
		public DateTime DeletedDate { get; set; }
	}

	public class Organization
	{
		public Guid GUID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int EmployerIdentificationNumber { get; set; }
		public int NumberOfEmployees { get; set; }
		public string TelephoneNumber { get; set; }
		public string EmailAddress { get; set; }
		public string WebsiteURL { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastUpdatedDate { get; set; }
		public DateTime DeletedDate { get; set; }
	}

	class FakeDataHelper
	{
		
	}
}
