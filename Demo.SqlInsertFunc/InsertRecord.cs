using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Demo.SqlInsertFunc;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public static class InsertRecord
{
    [FunctionName("InsertRecord")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log,
        [Sql("demo.Expert", ConnectionStringSetting = "SqlConnectionString")] IAsyncCollector<Expert> expertCollector,
        [Sql("demo.Expertise", ConnectionStringSetting = "SqlConnectionString")] IAsyncCollector<Expertise> expertiseCollector,
        [Sql("demo.InstitutionOrSector", ConnectionStringSetting = "SqlConnectionString")] IAsyncCollector<InstitutionOrSector> iosCollector,
        CancellationToken ct)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<Expert>(requestBody);
        
        var expertId = Guid.NewGuid();
        data.Id = expertId;

        await expertCollector.AddAsync(data, ct);
        await expertCollector.FlushAsync(ct);
        
        var expertiseTasks = data.Expertise.Select(async x =>
        {
            x.ExpertId = expertId;
            await expertiseCollector.AddAsync(x, ct);
        });
        await Task.WhenAll(expertiseTasks);
        await expertiseCollector.FlushAsync(ct);

        var iosTasks = data.InstitutionOrSectors.Select(async x =>
        {
            x.ExpertId = expertId;
            await iosCollector.AddAsync(x, ct);
        });
        await Task.WhenAll(iosTasks);
        await iosCollector.FlushAsync(ct);

        return (ActionResult)new CreatedResult(data.Id.ToString(), data);
    }
}

public class Expert
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? InstitutionDetails { get; set; }
    
    public IEnumerable<Expertise> Expertise { get; set; }
        = Enumerable.Empty<Expertise>();
    public IEnumerable<InstitutionOrSector> InstitutionOrSectors { get; set; } 
        = Enumerable.Empty<InstitutionOrSector>();
}

public class Expertise
{
    public Guid Id { get; set; }
    public Guid ExpertId { get; set; }
    public int OccupationalMapId { get; set; }
    public int LevelId { get; set; }
}

public class InstitutionOrSector
{
    public Guid Id { get; set; }
    public Guid ExpertId { get; set; }
    public int InstitutionOrSectorId { get; set; }
}