# MySqlExpress
MySqlExpress simplifies the implementation of MySQL in C#.

This class library aims to encourage rapid application development with MySQL.

Github: [https://github.com/adriancs2/MySqlExpress](https://github.com/adriancs2/MySqlExpress)

Nuget: [https://www.nuget.org/packages/MySqlExpress](https://www.nuget.org/packages/MySqlExpress)

PM> NuGet\Install-Package MySqlExpress

Download **MySqlExpress Helper**: [https://github.com/adriancs2/MySqlExpress/releases](https://github.com/adriancs2/MySqlExpress/releases)

![MySqlExpress Demo](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/screenshot05.png)

## Introduction

MySqlExpress consists of 2 parts.

The **first part** is the C# class library of MySqlExpress. It introduces some "shortcuts" as to simplify the execution of tasks related to MySQL.

To begin with, download the source code and add the class file "MySqlExpress.cs" into your project,

or add the referrence of the project of MySqlExpress into your project,

or install the Nuget package of MySqlExpress into your project.

The **second part** is a software called **"MySqlExpress Helper.exe"**. The main function of this software is to generate C# class objects, which will be explained in details below. I'll refer this small program as the **"Helper App"** for the rest of this article.

Download **MySqlExpress Helper**: [https://github.com/adriancs2/MySqlExpress/releases](https://github.com/adriancs2/MySqlExpress/releases)

MySqlExpress is built on top of MySqlConnector (MIT) library. If you wish to use another connector or provider, you can download the source code and compile it with your favorite connector.

## Before Start

As usual, to begin coding with MySQL, first add the following using statement to allow the usage of MySqlconnector (MIT) library.
```csharp
using MySqlConnector;
```
In this article, let's assume that we store the MySQL connection string as a static field. For example:
```csharp
public class config
{
    public static string ConnString = 
        "server=localhost;user=root;pwd=1234;database=test;";
}
```
Hence, we can obtain the connection string anywhere in the project as below:

```csharp
config.ConnString
```

Here is the standard MySQL connection code block:
```csharp
using (MySqlConnection conn = new MySqlConnection(config.ConnString))
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;
        conn.Open();

        // execute queries

        conn.Close();
    }
}
```
Declare a new MySqlExpress object to start using:
```csharp
using (MySqlConnection conn = new MySqlConnection(config.ConnString))
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;
        conn.Open();

        MySqlExpress m = new MySqlExpress(cmd);

        // perform queries

        conn.Close();
    }
}
```
The standard MySQL connection code block shown above can be saved into Visual Studio toolbox bar. So, next time, whenever you need this code block, you can drag and drop from the toolbox bar.

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/02.png)

Now the code block is saved at the toolbox.

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/03.png)

Next time, whenever you need the code block, just drag it from the toolbox into the text editor.

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/05.png)

## Let's Start - Using MySqlExpress

1. Start Transaction, Commit, Rollback
2. Getting Rows of Objects from MySQL Table
3. Getting a Customized Object Structure
4. Getting a single value (ExecuteScalar<T>)
5. Save (v.17) - Saving Objects
6. Insert Row (Save Data)
7. Update Row (Update Data)
8. Insert Update
9. Generate Like String
10. Execute a SQL statement
  
###  1. Start Transaction, Commit and Rollback
```csharp
using (MySqlConnection conn = new MySqlConnection(config.ConnString))
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;
        conn.Open();

        MySqlExpress m = new MySqlExpress(cmd);

        try
        {
            m.StartTransaction();

            // perform lots of queries
            // action success

            m.Commit();
        }
        catch
        {
            // Error occur

            m.Rollback();
        }

        conn.Close();
    }
}
```
There are a few benefits of using TRANSACTION in MySQL.

On a 7200 rpm HDD hard drive, the maximum I/O writes (numbers of SQL queries) that MySQL can executes is around 100 to 120 times.

Read more about MySQL Disk I/O Capacity at: [https://dev.mysql.com/doc/refman/5.7/en/innodb-configuring-io-capacity.html](https://dev.mysql.com/doc/refman/5.7/en/innodb-configuring-io-capacity.html)

If you perform 1000 queries, they will be executed one by one, which will take around 4~7 seconds on HDD hard drive to complete.

By using TRANSACTION + COMMIT, all 1000 queries will all be executed at once. This saves a lots of disk operation time.

Sometimes, there are chains of operations which involves multiple tables and rows. Without transaction, if there is any bad thing or error occurs in the middle of the process, the whole operation will be terminated half way, resulting partial or incomplete data saving. Which would be a problematic to fix the data. Hence, TRANSACTION can prevent such thing happens. With transaction, the whole chain of operation will be cancelled.

ROLLBACK, means cancel. Discard all queries that sent during the TRANSACTION period.

Read more about transaction at [here](https://www.mysqltutorial.org/mysql-transaction.aspx)

### 2. Getting Rows of Objects from MySQL Table

Assume that, we have a MySQL table like this:

```sql
CREATE TABLE `player` (
`id` int unsigned NOT NULL AUTO_INCREMENT,
`code` varchar(10),
`name` varchar(300),
`date_register` datetime,
`tel` varchar(100),
`email` varchar(100),
`status` int unsigned,
PRIMARY KEY (`id`));
```
First, creates a new class.
  
```csharp
public class obPlayer
{
   
}
```

The name of class. If the MySQL table's name is "player", you can name the class as "obPlayer".

"ob" means "object".

"obPlayer", an object of "Player".

But, anyway, you can name the class anything according to your personal flavor, of course.

Next, create the class object's fields or properties:

There are 3 modes of creating the fields or properties:

1. Private Fields + Public Properties
2. Public Properties
3. Public Fields

Run the **Helper** app.

**First Mode: Private Fields + Public Properties**
   
![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/f03.png)

Paste the text into the class:
    
```csharp
public class obPlayer
{
    int id = 0;
    string code = "";
    string name = "";
    DateTime date_register = DateTime.MinValue;
    string tel = "";
    string email = "";
    int status = 0;

    public int Id { get { return id; } set { id = value; } }
    public string Code { get { return code; } set { code = value; } }
    public string Name { get { return name; } set { name = value; } }
    public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
    public string Tel { get { return tel; } set { tel = value; } }
    public string Email { get { return email; } set { email = value; } }
    public int Status { get { return status; } set { status = value; } }
}
```

The purpose using this combination (private fields + public properties):

Private fields are used to match the columns' name of MySQL table and map the data.

Public properties are used to convert the naming of the fields into C# Coding Naming Convertions, which is PascalCase:

Read more about [C# Coding Naming Conventions](https://github.com/ktaranov/naming-convention/blob/master/C%23%20Coding%20Standards%20and%20Naming%20Conventions.md)

The MySQL column's naming conventions uses lower case and underscore to separate words.

Read more about [MySQL Naming Conventions](https://medium.com/@centizennationwide/mysql-naming-conventions-e3a6f6219efe)

The symbol of "_" (underscore) is considered less typing friendly than using just latin characters.

Therefore, converting the field name to PacalCase will align with the C# naming conventions and also increase the convenient and speed of coding.
    
**Second Mode: Public Properties**

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/g01.png)

Then, paste all the copied text into the class:
```csharp
public class obPlayer
{
    public int id { get; set; }
    public string code { get; set; }
    public string name { get; set; }
    public DateTime date_register { get; set; }
    public string tel { get; set; }
    public string email { get; set; }
    public int status { get; set; }
}
```

**Third Mode: Public Fields**

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/f02.png)

```csharp
public class obPlayer
{
    public int id = 0;
    public string code = "";
    public string name = "";
    public DateTime date_register = DateTime.MinValue;
    public string tel = "";
    public string email = "";
    public int status = 0;
}
```

**Getting a single row of "Player" object**

Here's the code of getting a single row of "Player" object.
```csharp
int id = 1;

// declare the object
obPlayer p = null;

using (MySqlConnection conn = new MySqlConnection(config.ConnString))
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;
        conn.Open();

        MySqlExpress m = new MySqlExpress(cmd);

        // parameterize the values
        Dictionary<string, object> dicParam = new Dictionary<string, object>();
        dicParam["@vid"] = id;

        p = m.GetObject<obPlayer>($"select * from player where id=@vid;", dicParam);

        conn.Close();
    }
}
  ```
Getting a list of objects (get multiple rows from a MySQL table):
```csharp
// declare the object list
List<obPlayer> lst = null;

using (MySqlConnection conn = new MySqlConnection(config.ConnString))
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;
        conn.Open();

        MySqlExpress m = new MySqlExpress(cmd);

        // parameterize the values
        Dictionary<string, object> dicParam = new Dictionary<string, object>();
        dicParam["@vname"] = "%adam%";

        lst = m.GetObjectList<obPlayer>($"select * from player where name like @vname;", dicParam);

        conn.Close();
    }
}
```

### 3. Getting a Customized Object Structure
One of the typical example is multiple SQL JOIN statement. For example:
```sql
select a.*, b.`year`, c.name 'teamname', c.code 'teamcode', c.id 'teamid'
from player a
inner join player_team b on a.id=b.player_id
inner join team c on b.team_id=c.id;
```
  
The output table structure is customized.

To create a non-standardized table's object structure, open the Helper program. Key in the customized SQL JOIN statement.

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/g03.png)

Create the custom class object:

```csharp
public class obPlayerTeam
{
   
}
```
Then, paste the copied text into the new custom object:

```csharp
public class obPlayerTeam
{
    int id = 0;
    string code = "";
    string name = "";
    DateTime date_register = DateTime.MinValue;
    string tel = "";
    string email = "";
    int status = 0;
    int year = 0;
    string teamname = "";
    string teamcode = "";
    int teamid = 0;

    public int Id { get { return id; } set { id = value; } }
    public string Code { get { return code; } set { code = value; } }
    public string Name { get { return name; } set { name = value; } }
    public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
    public string Tel { get { return tel; } set { tel = value; } }
    public string Email { get { return email; } set { email = value; } }
    public int Status { get { return status; } set { status = value; } }
    public int Year { get { return year; } set { year = value; } }
    public string Teamname { get { return teamname; } set { teamname = value; } }
    public string Teamcode { get { return teamcode; } set { teamcode = value; } }
    public int Teamid { get { return teamid; } set { teamid = value; } }
}
```

  Getting the customized table object:
```csharp
// declare the object
List<obPlayerTeam> lst = null;

// parameterized the value
Dictionary<string, object> dicParam = new Dictionary<string, object>();
dicParam["@vname"] = "%adam%";

using (MySqlConnection conn = new MySqlConnection(config.ConnString))
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;
        conn.Open();

        MySqlExpress m = new MySqlExpress(cmd);

        lst = m.GetObjectList<obPlayerTeam>(@"
            select a.*, b.`year`, c.name 'teamname', c.code 'teamcode', c.id 'teamid'
            from player a inner join player_team b on a.id=b.player_id
            inner join team c on b.team_id=c.id
            where a.name like @vname;", dicParam);

        conn.Close();
    }
}
```
  
### 4. Getting a single value (ExecuteScalar<T>)
```csharp
// int
int count = m.ExecuteScalar<int>("select count(*) from player;");

// datetime
DateTime d = m.ExecuteScalar<DateTime>("select date_register from player where id=2;");

// string
string name = m.ExecuteScalar<string>("select name from player where id=1;");
```

Getting single value with parameters
  
```csharp
// parameters
Dictionary<string, object> dicParam1 = new Dictionary<string, object>();
dicParam1["@vname"] = "%adam%";

Dictionary<string, object> dicParam2 = new Dictionary<string, object>();
dicParam2["@vid"] = 1;

// int
int count = m.ExecuteScalar<int>("select count(*) from player where name like @vname;", dicParam1);

// datetime
DateTime d = m.ExecuteScalar<DateTime>("select date_register from player where id=@vid;", dicParam2);

// string
string name = m.ExecuteScalar<string>("select name from player where id=@vid;", dicParam2);

```
  
### 5. Save (v1.7) - Saving Objects

A combination of "INSERT" and "UPDATE".
This method will first attempt to perform an INSERT. If the primary key of the data has already existed in MySQL table, then it will perform an UPDATE.

```csharp
// Syntax
m.Save(tablename, class);
m.SaveList(tablename, List<class>);

// Example:

// Saving single object
m.Save("player", player);

// Saving list of objects
m.SaveList("player", lstPlayer);
```
  
### 6. Insert Row (Save Data)

Performs INSERT by using dictionary.

> **Note:**
> **The dictionary values will be inserted as parameterized values**
    
![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/g04.png)
  
The field "id" is a primary key, auto-increment field. Therefore, we don't need to insert data for this field.

Delete the following line from the block:
    
```csharp
dic["id"] =
```

Fill in data into the dictionary:
```csharp
Dictionary<string, object> dic = new Dictionary<string, object>();

dic["code"] = "AA001";
dic["name"] = "John Smith";
dic["date_register"] = DateTime.Now;
dic["tel"] = "1298343223";
dic["email"] = "john_smith@mail.com";
dic["status"] = 1;

m.Insert("player", dic);
```
  
Run the following code to obtain new inserted ID:
    
```csharp
m.LastInsertId
```
    
Obtain the LAST INSERT ID:
  
```csharp
Dictionary<string, object> dic = new Dictionary<string, object>();

dic["code"] = "AA001";
dic["name"] = "John Smith";
dic["date_register"] = DateTime.Now;
dic["tel"] = "1298343223";
dic["email"] = "john_smith@mail.com";
dic["status"] = 1;

m.Insert("player", dic);

int newid = m.LastInsertId;
```

### 7 Update Row (Update Data)

Performs UPDATE by using dictionary.
  
> **Note:**
> **The dictionary values will be inserted as parameterized values**

For updating table that has one primary key. The parameters:
```csharp
m.Update(tablename, dictionary, primary key column name, id);
```
Generate the dictionary entries from the Helper App.
    
Remove the "id" dictionary entry:
    
```csharp
dic["id"] =
```
    
Paste it at the code block, fill the value and execute the Update command:
  
```csharp
Dictionary<string, object> dic = new Dictionary<string, object>();

dic["code"] = "AA001";
dic["name"] = "John Smith";
dic["date_register"] = DateTime.Now;
dic["tel"] = "1298343223";
dic["email"] = "john_smith@mail.com";
dic["status"] = 1;

m.Update("player", dic, "id", 1);
```
  
For updating table that has multiple primary keys or multiple reference column. The parameters:
```csharp
m.Update(tablename, dictionary data, dictionary reference data);
```
Example:
```csharp
// data
Dictionary<string, object> dic = new Dictionary<string, object>();
dic["code"] = "AA001";
dic["name"] = "John Smith";
dic["date_register"] = DateTime.Now;
dic["tel"] = "1298343223";
dic["email"] = "john_smith@mail.com";
dic["status"] = 1;

// update condition / referrence column data
Dictionary<string, object> dicCond = new Dictionary<string, object>();
dicCond["year"] = 2022;
dicCond["team_id"] = 1;

m.Update("player_team", dic, dicCond);
```
  
### 8. Insert Update

Performs INSERT & UPDATE by using dictionary.
  
> **Note:**
> **The dictionary values will be inserted as parameterized values**

This is especially useful when the table has multiple primary keys and no auto-increment field.

Insert > if the primary keys are not existed

Update it > if the primary keys existed.

First, generate the dictionary entries:

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/g05.png)

Next, generate the update column list:

![](https://raw.githubusercontent.com/adriancs2/MySqlExpress/main/wiki/g06.png)
  
  Paste it at the code block and runs the Insert Update method:
```csharp
List<string> lstUpdateCol = new List<string>();

lstUpdateCol.Add("team_id");
lstUpdateCol.Add("score");
lstUpdateCol.Add("level");
lstUpdateCol.Add("status");

using (MySqlConnection conn = new MySqlConnection(config.ConnString))
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;
        conn.Open();

        MySqlExpress m = new MySqlExpress(cmd);

        // data
        Dictionary<string, object> dic = new Dictionary<string, object>();

        dic["year"] = 2022;
        dic["player_id"] = 1;
        dic["team_id"] = 1;
        dic["score"] = 10m;
        dic["level"] = 1;
        dic["status"] = 1;

        m.InsertUpdate("player_team", dic, lstUpdateCol);

        conn.Close();
    }
}
```
  
### 9. Generate Like String

```csharp
string name = "James O'Brien";

// parameters
Dictionary<string, object> dicParam = new Dictionary<string, object>();
dicParam["@vname"] = m.GetLikeString(name);

List<obPlayer> lst = m.GetObjectList<obPlayer>("select * from player where name like @vname;", dicParam);
```
  
### 10. Execute a Single SQL statement
 
```csharp
Dictionary<string, object> dicParam = new Dictionary<string, object>();
dicParam["@vName"] = "James O'Brien";
dicParam["@vCode"] = "AA001";
m.Execute("delete from player where name=@vName or code=@vCode;", dicParam);
```
  
Happy coding.
