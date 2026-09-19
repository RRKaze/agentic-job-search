namespace AgenticJobSearch.Domain;

public enum JobLifecycleState
{
    Discovered = 0,
    Analyzed = 1,
    Rejected = 2,
    ReadyToApply = 3,
    ApplicationStarted = 4,
    Applied = 5
}

public enum EligibilityDecision
{
    Ineligible = 0,
    NeedsReview = 1,
    Eligible = 2
}

public enum JobWorkMode
{
    Unknown = 0,
    Remote = 1,
    Hybrid = 2,
    Onsite = 3
}

public enum ApplicationState
{
    NotStarted = 0,
    Drafting = 1,
    Submitted = 2,
    Rejected = 3,
    Screen = 4,
    Interview = 5,
    Offer = 6,
    Withdrawn = 7
}

public enum FeedbackType
{
    RecruiterScreen = 0,
    Interview = 1,
    Rejection = 2,
    Offer = 3,
    ManualNote = 4
}
