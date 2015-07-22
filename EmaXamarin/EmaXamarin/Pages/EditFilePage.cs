using EmaXamarin.Api;
using Xamarin.Forms;

namespace EmaXamarin
{
    internal class EditFilePage : ContentPage
    {
        private readonly string _pageName;
        private readonly PageService _pageService;
        private readonly Editor _editBox;

        public EditFilePage(string pageName, PageService pageService)
        {
            _pageName = pageName;
            _pageService = pageService;

            _editBox = new Editor
            {
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            Content = new StackLayout
            {
                Children =
                {
                    _editBox
                }
            };

            _editBox.Text = pageService.GetTextOfPage(pageName);
        }

        protected override bool OnBackButtonPressed()
        {
            var result = base.OnBackButtonPressed();

            _pageService.SavePage(_pageName, _editBox.Text);

            return result;
        }
    }
}