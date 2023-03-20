
# Flights_Project

The project aims to fetch the results of changing flights schedule.

- Database design (data types, keys, indexes)

We have 3 tables which are same as the CSV data in the problem. Looking at the Data it is found that the Route Id can be different for an origin destination pair. This causes problem in comparing the data for two different weeks. A Flight can have a corresponding flight in previous week within a time tolerance 30 minutes +/- to compare if it is in the same route of origin and destination we need to check data from the Routes table. This causes a high time complexity as we have to compare the origin destination pair to check if the route is same. To overcome this problem we introduce a new column in the database named segment_id. A segment is same for two rows if the origin-destination pair is same. This greatly reduces the time complexity of querying the data from the database.

As we need to find the segments in which an airline is intrested in, segment_id can also be inserted in the subscriptions table.

I created another table named Segments which holds the segments data. Both the Routes and Subscriptions table refer to the Segments table for foreign key segment_id.

Databases with key and types : 
 Table_Name	  Field_Name	         Data_Type	Primary_Key
 Routes	      route_id	           INTEGER	  Yes
              origin_city_id	     INTEGER	
              destination_city_id	INTEGER	
              departure_date	     TIMESTAMP	
              segment_id(FK)      INTEGER	
 Flights	     flight_id	          INTEGER	  Yes
              route_id(FK)        INTEGER	
              departure_time	     TIMESTAMP	
              arrival_time	       TIMESTAMP	
              airline_id         	INTEGER	
Segments	     segment_id	         INTEGER	  Yes
              origin_city_id	     INTEGER	
              destination_city_id	INTEGER	
Subscriptions	agency_id	          INTEGER	
              origin_city_id	     INTEGER	
              destination_city_id	INTEGER	
              segment_id(FK)      INTEGER	

- Overall structure of the application (layers, data flow, dependencies, (de)coupling)
To access data from database I have implemented an entity for all the tables. Each table also has a repository to be accessed by other classes in the application. There is a database context class implemented to be used by the repositories of all the entities.
The application has 2 functionalities :
1.  To Calculate the change in flight schedule
For this we have a CalculateChangeResults Service with an interface.
The class CalculateChangeResultsService requires 2 arguments in constructor (FlightRepository and SubscriptionsRepository) . We use dependency injection provided by .net for creating an instance of the class. All the repositories are added to the dependency injection container.

2.To migrate data from csv to postgres database.
For this we have the MigrationUtils file which gets the connection string from configuration and performs the necessary database query to insert data.

- Data access layer implementation
Data access layer is implemented using Interface for all the repostories. The repositories have all the required methods to query database.
There are 3 repositories : FlightsRepository, SubscriptionsRepository, RoutesRepository.

- Change detection algorithm implementation

The problem with calculation is that we need to look 7 days back and 7 days forward and see if any flight of same agency exists with the same time (30 minutes +/- tolerance).

This task will be compute intensive if for every flight we traverse to find flights 7 days forward and backward. I created an Array of dictionary of lists to solve this. 

The Array will have a dictionary at every index. The index of the array represent the day between the dates provided as input. The dictionary stores the flights in a list for every hour in that day. Example : arrayofdict[2][8] stores the list of flights for 8th hour for 2nd day.

Once the arrayOfDict is filled we loop through the sorted flights data again to check 7 days back and forward in the same hour (+/- 1 hour for 30 mins tolerance).

Once the calculation is done we store the result in the results.csv file.

- Data structures used

For adding the segment_id column during insertion. I created a hashmap with a pair of origin-destination as the key and segment_id as value. So while inserting routes table in roughly O(1) we can find the segment for any row and insert the row.

- Optimizations applied


To optimize the insert of data to tables. I have added the code to insert data in bulk. The MigrationUtils class inserts data with a batch of 10000 for flights database as data is very large.

The MigrationUtils.cs file also calculates and adds the segment_id column to routes and subscriptions table. Calculation is done using a HashTable.

Inserting Data:

To insert data run the command in root of project : dotnet migrate {pathtofolder}

The pathtofolder is the path to folder containing all three CSV files.

The MigrationUtils.cs file holds the function to migrate the data from csv to postgres database.

It first creates the tables and then insert the data in batches. As data is very large in flights.csv we insert data in a batch of 10000 rows at once.


Calculating the change:

We can calculate the result by running this command in root : donet run {startDate} {endDate} {agencyId}

