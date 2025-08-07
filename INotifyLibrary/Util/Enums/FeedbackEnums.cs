namespace INotifyLibrary.Util.Enums
{
    /// <summary>
    /// Categories for user feedback
    /// </summary>
    public enum FeedbackCategory
    {
        BugReport = 0,
        FeatureRequest = 1,
        General = 2,
        Performance = 3,
        UI_UX = 4,
        Documentation = 5
    }

    /// <summary>
    /// Status of feedback submission
    /// </summary>
    public enum FeedbackStatus
    {
        Submitted = 0,
        InReview = 1,
        Resolved = 2,
        Dismissed = 3
    }
}