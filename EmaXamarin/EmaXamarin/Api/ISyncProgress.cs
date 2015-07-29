namespace EmaXamarin.Api
{
    public interface ISyncProgress
    {
        void ReportProgress(int totalSteps, int currentStep, string label);
    }
}