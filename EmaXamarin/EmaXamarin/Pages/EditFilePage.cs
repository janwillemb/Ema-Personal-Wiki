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
            PersistedState.PageInEditMode = pageName;

            _editBox = new Editor
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Text = _originalText,
            };
            _editBox.TextChanged += (sender, args) => PersistedState.AutoSaveEditText = args.NewTextValue;

            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Children = {_editBox}
                }
            };

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Save",
                Icon = "ic_menu_save.png",
                Command = new Command(Save),
                Order = ToolbarItemOrder.Primary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Clear",
                Icon = "ic_menu_clear_playlist.png",
                Command = new Command(Clear)
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Delete",
                Icon = "ic_menu_delete.png",
                Command = new Command(Delete),
                Order = ToolbarItemOrder.Secondary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Cancel",
                Command = new Command(Cancel),
                Order = ToolbarItemOrder.Secondary
            });
        }

        private void Save()
        {
            _pageService.SavePage(_pageName, _editBox.Text);

            ClosePage();
        }

        private void Clear()
        {
            WhatToDoWithUnsavedChanges(Save, () => { _editBox.Text = string.Empty; });
        }

        private void Delete()
        {
            WhatToDoWithUnsavedChanges(Save, () =>
            {
                _pageService.Delete(_pageName);
                ClosePage();
            });
        }

        private void Cancel()
        {
            WhatToDoWithUnsavedChanges(Save, ClosePage);
        }

        private async void ClosePage()
        {
            PersistedState.PageInEditMode = string.Empty;
            PersistedState.AutoSaveEditText = string.Empty;
            await Navigation.PopAsync();
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
            _editBox.Focus();
        }

        protected override bool OnBackButtonPressed()
        {
            Cancel();
            return true;
        }
    }
}