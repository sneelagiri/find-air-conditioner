using FluentAssertions;
using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Application.Messaging;
using FindAirConditioners.Application.Persistence;
using FindAirConditioners.Application.Services;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.IntegrationTests;

public sealed class PipelineIntegrationTests
{
    [Fact]
    public async Task Collection_then_analysis_pipeline_persists_a_result()
    {
        var queue = new InMemoryQueue<CollectionJob>();
        var analysisQueue = new InMemoryQueue<AnalysisJob>();
        var repository = new InMemoryAirConditionerRepository();
        var collectionService = new AirConditionerCollectionService(queue, repository);
        var analysisService = new AirConditionerAnalysisService(repository);
        var catalog = new SeededAirConditionerCatalog();

        var searchId = await collectionService.QueueCollectionAsync(new AirConditionerSearchRequest(2000m));
        var collectJob = await queue.DequeueAsync(TimeSpan.FromSeconds(1));
        collectJob.Should().NotBeNull();

        await repository.SaveListingsAsync(searchId, catalog.GetListings());
        await analysisQueue.EnqueueAsync(new AnalysisJob(searchId, DateTimeOffset.UtcNow));

        var analysisJob = await analysisQueue.DequeueAsync(TimeSpan.FromSeconds(1));
        analysisJob.Should().NotBeNull();

        var result = await analysisService.AnalyzeAsync(searchId);

        result.SearchId.Should().Be(searchId);
        result.Status.Should().Be("Completed");
        (await repository.GetResultAsync(searchId)).Should().NotBeNull();
    }
}
