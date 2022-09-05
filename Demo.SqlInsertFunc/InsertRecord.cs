namespace Demo.SqlInsertFunc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        [Sql("experts.Expert", ConnectionStringSetting = "SqlConnectionString")] IAsyncCollector<OutputExpert> expertCollector,
        [Sql("experts.Expertise", ConnectionStringSetting = "SqlConnectionString")] IAsyncCollector<Expertise> expertiseCollector,
        [Sql("experts.InstitutionOrSector", ConnectionStringSetting = "SqlConnectionString")] IAsyncCollector<InstitutionOrSector> iosCollector,
        CancellationToken ct)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<InputExpert>(requestBody);
        
        var expertId = Guid.NewGuid();
        data.Id = expertId;
        var outputExpert = new OutputExpert
        {
            CreatedDate = data.CreatedDate,
            Email = data.Email,
            FullName = data.FullName,
            Id = expertId,
            InstitutionDetails = data.InstitutionDetails
        };

        await expertCollector.AddAsync(outputExpert, ct);
        await expertCollector.FlushAsync(ct);
        
        var expertiseTasks = data.Expertise.Select(async x =>
        {
            x.ExpertId = expertId;
            x.Id = Guid.NewGuid();
            await expertiseCollector.AddAsync(x, ct);
        });
        await Task.WhenAll(expertiseTasks);
        await expertiseCollector.FlushAsync(ct);

        var iosTasks = data.InstitutionOrSectors.Select(async x =>
        {
            x.ExpertId = expertId;
            x.Id = Guid.NewGuid();
            await iosCollector.AddAsync(x, ct);
        });
        await Task.WhenAll(iosTasks);
        await iosCollector.FlushAsync(ct);

        return (ActionResult)new CreatedResult(data.Id.ToString(), data);
    }
}

public class OutputExpert
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? InstitutionDetails { get; set; }
}

public class InputExpert
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