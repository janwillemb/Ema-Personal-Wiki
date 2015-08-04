namespace EmaXamarin.Api
{
    public interface ISyncProgress
    {
        void OnSyncStart();
        void ReportProgress(int totalSteps, int currentStep, string label);
        void OnSyncFinished();
    }
}