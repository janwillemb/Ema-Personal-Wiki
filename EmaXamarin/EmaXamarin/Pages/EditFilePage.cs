using System;
using EmaXamarin.Api;
using Xamarin.Forms;

namespace EmaXamarin.Pages
{
    public class EditFilePage : ContentPage
    {
        private readonly string _pageName;
        private readonly PageService _pageService;
        private readonly Editor _editBox;
        private readonly string _originalText;

        public EditFilePage(string pageName, PageService pageService)
        {
            _pageName = pageName;
            _pageService = pageService;
            _originalText = pageService.GetTextOfPage(pageName);

            _editBox = new Editor
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Text = _originalText
            };
            _editBox.TextChanged += (sender, args) => PersistedState.AutoSaveEditText = args.NewTextValue;

            Content = new StackLayout
            {
                Children =
                {
                    _editBox
                }
            };

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Cancel",
                Command = new Command(Cancel)
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Clear",
                Command = new Command(Clear)
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Save",
                Command = new Command(Save),
                Order = ToolbarItemOrder.Primary
            });
        }

        private async void Save()
        {
            _pageService.SavePage(_pageName, _editBox.Text);

            PersistedState.AutoSaveEditText = string.Empty;
            await Navigation.PopAsync();
        }

        private void Clear()
        {
            WhatToDoWithUnsavedChanges(Save, () => { _editBox.Text = string.Empty; });
        }

        private void Cancel()
        {
            WhatToDoWithUnsavedChanges(Save, async () =>
            {
                PersistedState.AutoSaveEditText = string.Empty;
                await Navigation.PopAsync();
            });
        }

        private async void WhatToDoWithUnsavedChanges(Action saveAction, Action ignoreAction)
        {
            var action = "Ignore";
            if (_originalText != _editBox.Text)
            {
                action = await DisplayActionSheet("You have unsaved changes", "Keep editing", null, "Ignore", "Save");
            }
            if (action == "Save")
            {
                saveAction();
            }
            else if (action == "Keep editing")
            {
                //do nothing
            }
            else
            {
                ignoreAction();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!string.IsNullOrEmpty(PersistedState.AutoSaveEditText))
            {
                _editBox.Text = PersistedState.AutoSaveEditText;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Cancel();
            return true;
        }
    }
}