using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using MoreLinq;
using Newtonsoft.Json;

namespace Demo.SqlInsertFunc.Tests;

public class UnitTest1
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IAsyncCollector<OutputExpert>> _expertCollectorMock;
    private readonly Mock<IAsyncCollector<Expertise>> _expertiseCollectorMock;
    private readonly Mock<IAsyncCollector<InstitutionOrSector>> _iosCollectorMock;

    public UnitTest1()
    {
        _loggerMock = new Mock<ILogger>();
        _expertCollectorMock = new Mock<IAsyncCollector<OutputExpert>>();
        _expertiseCollectorMock = new Mock<IAsyncCollector<Expertise>>();
        _iosCollectorMock = new Mock<IAsyncCollector<InstitutionOrSector>>();
    }

    [Fact]
    public async Task ShouldGenerateIds()
    {
        var input = GenerateInputExpert();
        var req = CreateMockRequest(input);
        var cts = new CancellationTokenSource();

        var outputExperts = new List<OutputExpert>();
        var outputExpertise = new List<Expertise>();
        var outputIos = new List<InstitutionOrSector>();

        _expertCollectorMock.Setup(x => x.AddAsync(It.IsAny<OutputExpert>(), It.IsAny<CancellationToken>()))
            .Callback((OutputExpert o, CancellationToken _) => outputExperts.Add(o));
        _expertiseCollectorMock.Setup(x => x.AddAsync(It.IsAny<Expertise>(), It.IsAny<CancellationToken>()))
            .Callback((Expertise o, CancellationToken _) => outputExpertise.Add(o));
        _iosCollectorMock.Setup(x => x.AddAsync(It.IsAny<InstitutionOrSector>(), It.IsAny<CancellationToken>()))
            .Callback((InstitutionOrSector o, CancellationToken _) => outputIos.Add(o));

        await InsertRecord.RunAsync(req, _loggerMock.Object, _expertCollectorMock.Object,
            _expertiseCollectorMock.Object, _iosCollectorMock.Object, cts.Token);

        var emptyGuid = Guid.Empty; 
        Assert.Single(outputExperts);
        Assert.NotEmpty(outputExpertise);
        Assert.NotEmpty(outputIos);
        var expertId = outputExperts.First().Id;
        Assert.All(outputExperts, e => Assert.NotEqual(emptyGuid, e.Id));
        Assert.All(outputExpertise, e =>
        {
            Assert.NotEqual(emptyGuid, e.Id);
            Assert.Equal(expertId, e.ExpertId);
        });
        Assert.All(outputIos, e =>
        {
            Assert.NotEqual(emptyGuid, e.Id);
            Assert.Equal(expertId, e.ExpertId);
        });
    }
    
    [Fact]
    public async Task ShouldGenerateCreatedDate()
    {
        var input = GenerateInputExpert();
        var req = CreateMockRequest(input);
        var cts = new CancellationTokenSource();

        var outputExperts = new List<OutputExpert>();

        _expertCollectorMock.Setup(x => x.AddAsync(It.IsAny<OutputExpert>(), It.IsAny<CancellationToken>()))
            .Callback((OutputExpert o, CancellationToken _) => outputExperts.Add(o));

        await InsertRecord.RunAsync(req, _loggerMock.Object, _expertCollectorMock.Object,
            _expertiseCollectorMock.Object, _iosCollectorMock.Object, cts.Token);

        DateTime defaultDateTime = default; 
        Assert.Single(outputExperts);
        Assert.NotEqual(defaultDateTime, outputExperts.First().CreatedDate);
    }

    [Fact]
    public async Task ShouldOutputExpertDetailsEquivalentToInput()
    {
        var input = GenerateInputExpert();
        var req = CreateMockRequest(input);
        var cts = new CancellationTokenSource();

        var outputExperts = new List<OutputExpert>();
        var outputExpertise = new List<Expertise>();
        var outputIos = new List<InstitutionOrSector>();

        _expertCollectorMock.Setup(x => x.AddAsync(It.IsAny<OutputExpert>(), It.IsAny<CancellationToken>()))
            .Callback((OutputExpert o, CancellationToken _) => outputExperts.Add(o));
        _expertiseCollectorMock.Setup(x => x.AddAsync(It.IsAny<Expertise>(), It.IsAny<CancellationToken>()))
            .Callback((Expertise o, CancellationToken _) => outputExpertise.Add(o));
        _iosCollectorMock.Setup(x => x.AddAsync(It.IsAny<InstitutionOrSector>(), It.IsAny<CancellationToken>()))
            .Callback((InstitutionOrSector o, CancellationToken _) => outputIos.Add(o));

        await InsertRecord.RunAsync(req, _loggerMock.Object, _expertCollectorMock.Object,
            _expertiseCollectorMock.Object, _iosCollectorMock.Object, cts.Token);

        Assert.Single(outputExperts);
        Assert.True(ExpertEquals(input, outputExperts.First(), outputExpertise, outputIos));
    }

    private static InputExpert GenerateInputExpert()
    {
        var counts = new[] { 1, 2, 3, 4, 5 };
        var countExpertise = counts[Faker.RandomNumber.Next(0, counts.Length - 1)];
        var countIos = counts[Faker.RandomNumber.Next(0, counts.Length - 1)];
        
        return new InputExpert
        {
            FullName = $"{Faker.Name.First()} {Faker.Name.Last()}",
            Email = Faker.Internet.Email(),
            InstitutionDetails = Faker.Company.BS(),
            Expertise = Enumerable.Range(0, countExpertise).Select(_ => new Expertise
            {
                LevelId = Faker.RandomNumber.Next(),
                OccupationalMapId = Faker.RandomNumber.Next()
            }).ToArray(),
            InstitutionOrSectors = Enumerable.Range(0, countIos).Select(_ => new InstitutionOrSector
            {
                InstitutionOrSectorId = Faker.RandomNumber.Next()
            }).ToArray()
        };
    }
    
    private static HttpRequest CreateMockRequest(InputExpert body)
    {
        var reqMock = new Mock<HttpRequest>();
        var s = new MemoryStream();
        var writer = new StreamWriter(s);
        writer.Write(JsonConvert.SerializeObject(body));
        writer.Flush();
        s.Position = 0;
        reqMock.Setup(m => m.Body).Returns(s);
        return reqMock.Object;
    }

    private bool ExpertEquals(
        InputExpert source,
        OutputExpert targetExpert,
        IReadOnlyCollection<Expertise> targetExpertise,
        IReadOnlyCollection<InstitutionOrSector> targetIos)
    {
        var sourceExpertiseList = source.Expertise.ToList();
        var sourceIosList = source.InstitutionOrSectors.ToList();
        return string.Equals(source.Email, targetExpert.Email)
               && string.Equals(source.FullName, targetExpert.FullName)
               && string.Equals(source.InstitutionDetails, targetExpert.InstitutionDetails)
               && sourceExpertiseList.Count == targetExpertise.Count
               && sourceExpertiseList.All(x => targetExpertise.Any(
                   y => y.LevelId == x.LevelId && y.OccupationalMapId == x.OccupationalMapId))
               && sourceIosList.Count == targetIos.Count
               && sourceIosList.All(x => targetIos.Any(
                   y => y.InstitutionOrSectorId == x.InstitutionOrSectorId));
    }
}