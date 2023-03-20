
# Flights_Project
2
The project aims to fetch the results of changing flights schedule.
3
​
4
Database design:
5
Starting with the Database design we have 3 tables which are same as the CSV data in the problem. Looking at the Data it is found that the Route Id can be different for an origin destination pair. This causes problem in comparing the data for two different weeks. A Flight can have a corresponding flight in previous week within a time tolerance 30 minutes +/- to compare if it is in the same route of origin and destination we need to check data from the Routes table.
6
This causes a high time complexity as we have to compare the origin destination pair to check if the route is same. To overcome this problem we introduce a new column in the database named segment_id. A segment is same for two rows if the origin-destination pair is same. This greatly reduces the time complexity of querying the data from the database.
I have added this column during insertion. I created a hashmap with a pair of origin-destination as the key and segment_id as value. So while inserting routes table in roughly O(1) we can find the segment for any row and insert the row.

7
As we need to find the segments in which an airline is intrested in, segment_id can also be inserted in the subscriptions table.
8
​
9
I created another table named Segments which holds the segments data. Both the Routes and Subscriptions table refer to the Segments table for foreign key segment_id.
10
​
11
Inserting Data:
12
To insert data run the command in root of project : dotnet migrate {pathtofolder}
13
The pathtofolder is the path to folder containing all three CSV files.
14
​
15
The MigrationUtils.cs file holds the function to migrate the data from csv to postgres database.
16
It first creates the tables and then insert the data in batches. As data is very large for flights.csv we insert data in a batch of 10000 rows at once.
17
​The MigrationUtils.cs file also calculates and adds the segment_id column to routes and subscriptions table.


18
Calculating the change:
19
The problem with calculation is that we need to look 7 days back and 7 days forward and see if any flight of same agency exists with the same time (30 minutes +/- tolerance).
20
This task will be compute intensive if for every flight we traverse to find flights 7 days forward and backward. I created an Array of dictionary of lists to solve this. 
21
The Array will have a dictionary at every index. The index of the array represent the day between the dates provided as input. The dictionary stores the flights in a list for every hour in that day. Example : arrayofdict[2][8] stores the list of flights for 8th hour for 2nd day.
22
Once the arrayOfDict is filled we loop through the sorted flights data again to check 7 days back and forward in the same hour (+/- 1 hour for 30 mins tolerance).
23
​
24
Once the calculation is done we store the result in the results.csv file.
25
 
