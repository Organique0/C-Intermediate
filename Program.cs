using AutoMapper;
using Dapper;
using Intermediate.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client.Extensibility;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.Json;

#region JSON
IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
DataContextDapper dapper = new(config);

string computersJson = File.ReadAllText("Computers.json");

//using System.Text.Json
//convert to camelCase
JsonSerializerOptions options = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
IEnumerable<Computer>? computersSystem = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<Computer>>(computersJson, options);


//using Newtonsoft.Json
//convert to cameCase
JsonSerializerSettings settings = new()
{
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};
IEnumerable<Computer>? computersNewtonsoft = JsonConvert.DeserializeObject<IEnumerable<Computer>>(computersJson);


if (computersNewtonsoft != null)
{
    foreach (Computer computer in computersNewtonsoft)
    {
        string? Date = computer.ReleaseDate?.Date.ToString("yyyy-MM-dd");

        string sql = @"INSERT INTO TutorialAppSchema.Computer (
            Motherboard,
            HasWifi,
            HasLTE,
            ReleaseDate,
            Price,
            VideoCard
        ) VALUES (
            '" + EscapeSingleQuote(computer.Motherboard!)
            + "','" + computer.HasWifi
            + "','" + computer.HasLTE
            + "','" + Date
            + "','" + computer.Price
        + "','" + EscapeSingleQuote(computer.VideoCard!)
        + "')";

        //dapper.ExecuteSql(sql);
    }
}

string computersCopyNewtonsoft = JsonConvert.SerializeObject(computersNewtonsoft, settings);
File.WriteAllText("computersCopyNewtonsoft.txt", computersCopyNewtonsoft);

string computersCopySystem = System.Text.Json.JsonSerializer.Serialize(computersSystem, options);
File.WriteAllText("computersCopySystem.txt", computersCopySystem);

static string EscapeSingleQuote(string input)
{
    string output = input.Replace("'", "''");
    return output;
}


#endregion

#region ModelMapping
string computersSnakeJson = File.ReadAllText("ComputersSnake.json");

//remaping names using mapper
Mapper mapper = new(new MapperConfiguration((cfg) =>
{
    cfg.CreateMap<ComputerSnake, Computer>()
    .ForMember(destination => destination.ComputerId,
        options => options.MapFrom(source => source.computer_id))
    .ForMember(destination => destination.Motherboard,
        options => options.MapFrom(source => source.motherboard))
    .ForMember(destination => destination.HasWifi,
        options => options.MapFrom(source => source.has_wifi))
    .ForMember(destination => destination.HasLTE,
        options => options.MapFrom(source => source.has_lte))
    .ForMember(destination => destination.ReleaseDate,
        options => options.MapFrom(source => source.release_date))
    .ForMember(destination => destination.VideoCard,
        options => options.MapFrom(source => source.video_card))
    .ForMember(destination => destination.CPUCores,
        options => options.MapFrom(source => source.cpu_cores))
    .ForMember(destination => destination.Price,
        options => options.MapFrom(source => source.price));
}));

//logging names with mapper
IEnumerable<ComputerSnake>? computersSystemUsingMapper = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<ComputerSnake>>(computersSnakeJson);

if (computersJson != null)
{
    IEnumerable<Computer> computerResult = mapper.Map<IEnumerable<Computer>>(computersSystemUsingMapper);

    foreach (Computer computer in computerResult)
    {
        Console.WriteLine(computer.Motherboard);
    }


    //logging names using json property name matching
    IEnumerable<Computer>? computersSystemJsonPropertyMapping = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<Computer>>(computersSnakeJson);

    if (computersSystemJsonPropertyMapping != null)
    {
        foreach (Computer computer in computersSystemJsonPropertyMapping)
        {
            Console.WriteLine(computer.Motherboard);
        }
    }
}
#endregion

#region entity&mapper

//initialize entity framework
//insert config in class
DataContextEF entityFramework = new(config);

Computer myComputer = new()
{
    Motherboard = "Some Motherboard",
    HasWifi = true,
    HasLTE = false,
    ReleaseDate = DateTime.Now,
    Price = 999.99m,
    VideoCard = "Some Video Card"
};


//does the same insert thing as dapper code but with EntityFW
//inserts one myComputer instance into a table
//entityFramework.Add(myComputer);
//entityFramework.SaveChanges();



string ReleaseDate = myComputer.ReleaseDate.Value.ToString("yyyy-MM-dd");
string Price = myComputer.Price.ToString("0.00", CultureInfo.InvariantCulture);

string sql2 = @"INSERT INTO TutorialAppSchema.Computer (
    Motherboard,
    HasWifi,
    HasLTE,
    ReleaseDate,
    Price,
    VideoCard
) VALUES (
    '" + myComputer.Motherboard
    + "','" + myComputer.HasWifi
    + "','" + myComputer.HasLTE
    + "','" + ReleaseDate
    + "','" + Price
+ "','" + myComputer.VideoCard
+ "')\n";



//log sql2 statement in a file
//rewrites file each time
//File.WriteAllText("log.txt", sql2);

//append to text file
/* using StreamWriter openFile = new("log.txt", append: true);
openFile.WriteLine(sql2);
openFile.Close();
string fileText = File.ReadAllText("log.txt"); */

/* Console.WriteLine(sql2);
int result = dapper.ExecuteSqlWithRowCount(sql2);
bool result = dapper.ExecuteSql(sql2);
Console.WriteLine(result); */

//log current date
DateTime rightNow = dapper.LoadDataSingle<DateTime>("SELECT GETDATE()");
//Console.WriteLine(rightNow);

//get data using sql and dapper
string sqlSelect = @"SELECT 
    ComputerId,
    Motherboard,
    HasWifi,
    HasLTE,
    ReleaseDate,
    Price,
    VideoCard
FROM TutorialAppSchema.Computer";

//log data using Dapper
IEnumerable<Computer> computers = dapper.LoadData<Computer>(sqlSelect);
foreach (Computer singleComputer in computers)
{
    //Console.Write($"{singleComputer.ComputerId},{singleComputer.Motherboard}, {singleComputer.HasWifi}, {singleComputer.HasLTE}, {singleComputer.ReleaseDate}, {singleComputer.Price}, {singleComputer.VideoCard} \n");

}
//get data using Entity
IEnumerable<Computer>? computersEF = entityFramework.Computer?.ToList<Computer>();
if (computersEF != null)
{
    foreach (Computer singleComputer in computersEF)
    {
        //Console.Write($"{singleComputer.ComputerId},{singleComputer.Motherboard}, {singleComputer.HasWifi}, {singleComputer.HasLTE}, {singleComputer.ReleaseDate}, {singleComputer.Price}, {singleComputer.VideoCard} \n");

    }
}


#endregion


