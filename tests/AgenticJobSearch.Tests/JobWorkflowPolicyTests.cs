using AgenticJobSearch.Application.Jobs;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Tests;

public sealed class JobWorkflowPolicyTests
{
    private readonly JobWorkflowPolicy policy = new();

    [Fact]
    public void Saved_job_can_move_to_applied()
    {
        var job = new Job();
        var request = Request("applied");

        Assert.Equal("applied", policy.ValidateTransition(job, request));
        Assert.Equal(ApplicationState.Submitted, policy.ApplicationStateFor("applied"));
    }

    [Fact]
    public void Applied_job_cannot_skip_to_offer()
    {
        var job = new Job { TrackingStage = "applied" };

        var failure = Assert.Throws<WorkflowFailure>(() => policy.ValidateTransition(job, Request("offer")));

        Assert.Equal(409, failure.StatusCode);
    }

    [Fact]
    public void Closed_application_cannot_keep_a_follow_up_date()
    {
        var job = new Job { TrackingStage = "applied" };
        var request = Request("rejected") with { NextFollowUp = new DateOnly(2026, 9, 30) };

        var failure = Assert.Throws<WorkflowFailure>(() => policy.ValidateTransition(job, request));

        Assert.Equal(400, failure.StatusCode);
    }

    private static UpdateJobWorkflowRequest Request(string status) =>
        new(status, new DateOnly(2026, 9, 22), null, null, null, null, null);
}
