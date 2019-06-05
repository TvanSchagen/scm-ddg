# DummyDataGenerator for Supply Chain Data

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
Database | Support
--- | ---
MySQL | 90%
Neo4j | 90%
SQL Server | 0%
PostgreSQL | 0%
... | ...