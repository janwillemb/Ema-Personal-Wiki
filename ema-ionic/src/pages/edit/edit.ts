import { ViewChild } from '@angular/core/src/metadata/di';
import { HelpPage } from '../help/help';
import { Settings } from '../../library/settings';
import { WikiFile } from '../../library/wiki-file';
import { AlertController, NavController, NavParams, ViewController } from 'ionic-angular';
import { Component } from '@angular/core';

@Component({
    selector: 'page-edit',
    templateUrl: 'edit.html'
})
export class EditPage {
    pageContent: string;
    pageTitle: string;
    orginalContent: string;
    styleGrey: boolean;

    @ViewChild("pageContentTextArea") pageContentTextArea;

    constructor(
        navParams: NavParams,
        settings: Settings,
        private alertController: AlertController,
        private navController: NavController,
        private viewController: ViewController) {

        var page: WikiFile = navParams.get("page");
        this.pageTitle = page.pageName;
        this.pageContent = page.contents;
        this.orginalContent = this.pageContent;

        this.styleGrey = settings.getStyle() === "Grey";
    }

    ionViewDidEnter() {
        this.pageContentTextArea.nativeElement.focus();
    }

    save() {
        this.viewController.dismiss({ pageContent: this.pageContent });
    }

    delete() {
        let confirm = this.alertController.create({
            title: "Delete page",
            message: "Do you really want to delete this page?",
            buttons: [{
                text: "Yes",
                handler: () => {
                    this.viewController.dismiss({ delete: true });
                }
            }, {
                text: "No"
            }]
        });
        confirm.present();
    }

    help() {
        this.navController.push(HelpPage);
    }

    onBackButton() {
        this.cancel();
    }
    cancel() {
        if (this.orginalContent !== this.pageContent) {
            let confirm = this.alertController.create({
                title: "Unsaved changes",
                message: "Discard unsaved changes?",
                buttons: [{
                    text: "OK",
                    handler: () => {
                        this.viewController.dismiss({ cancel: true });
                    }
                }, {
                    text: "Cancel"
                }]
            });
            confirm.present();
        } else {
            this.viewController.dismiss({ cancel: true });
        }
    }
}