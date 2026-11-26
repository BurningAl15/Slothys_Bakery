namespace FatCat.Feedback
{
    /// <summary>
    /// Optional face on the peeking character of FeedbackStamp (a component anywhere under it): told whether the
    /// answer was right so it can look happy or worried. The game provides it (Café: NarratorFace).
    /// </summary>
    public interface IFeedbackFace
    {
        void React(bool right);
    }
}
