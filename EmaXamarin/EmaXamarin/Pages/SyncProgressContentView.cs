using System;
using EmaXamarin.Api;
using Xamarin.Forms;

namespace EmaXamarin.Pages
{
    public class SyncProgressContentView : ContentView, ISyncProgress
    {
        private readonly ProgressBar _progressbar;
        private readonly Label _progressLabel;

        public SyncProgressContentView()
        {
            _progressbar = new ProgressBar();
            _progressLabel = new Label();

            Content = new StackLayout
            {
                Children =
                {
                    _progressLabel,
                    _progressbar,
                }
            };
        }

        public void OnSyncStart()
        {
            _progressbar.Progress = 0;
            IsVisible = true;
        }

        public async void ReportProgress(int totalSteps, int currentStep, string label)
        {
            var fraction = (double) currentStep/Math.Max(1, totalSteps);
            double progress = Math.Min(1, Math.Max(0, fraction));

            _progressLabel.Text = label;
            await _progressbar.ProgressTo(progress, 100, Easing.Linear);
        }

        public void OnSyncFinished()
        {
            IsVisible = false;
        }
    }
}