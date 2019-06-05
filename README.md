# Supply Chain Management Dummy Data Generator

This tool generates a schema and fills it with supply chain data for several different databases to use for benchmarking or testing. 
Default data generation parameters and connection settings can be changed in the main program.

## How to use

When running for the first time, the program will generate a .env file in the main program directory, fill this file with the right connection properties and relaunch the program to be able to generate data.
Execute using Visual Studio 2017 or higher.

## Technologies used
- C# .NET Core 2.0
- MySQL Connector/NET 8.0.16 for MySQL 8.0.x
- Neo4j .NET driver 1.7 for Neo4j 3.5.x

### Current Database Support
Database | Type | Support
--- | --- | ---
MySQL | Relational | 90%
Neo4j | Graph |90%
SQL Server | Relational | 0%
PostgreSQL | Relational | 0%
Titan  | Graph | 0%
OrientDB | Document/Graph | 0%
MongoDB | Document | 0%
... | ... | ...

### To Do
- Expand the data models with more properties
- Add a fake data library to generate more realistic names and properties for entities
- Add support for other relational databases
- Add support for other NoSQL databases (key value store, column store, document store)
- Add more batch insert features to speed up generating the data