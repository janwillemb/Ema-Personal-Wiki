import { WikiFile } from '../../library/wiki-file';
import { AlertController, NavParams, ViewController } from 'ionic-angular';
import { Component } from '@angular/core';

@Component({
  selector: 'page-edit',
  templateUrl: 'edit.html'
})
export class EditPage {
    pageContent: string;
    pageTitle: string;
    orginalContent: string;

    constructor(
        navParams: NavParams,
        private alertController: AlertController, 
        private viewController: ViewController) {

        var page: WikiFile = navParams.get("page");
        this.pageTitle = page.pageName;
        this.pageContent = page.contents;
        this.orginalContent = this.pageContent;
    }

    save() {
        this.viewController.dismiss({pageContent: this.pageContent});
    }
    
    cancel() {
        if (this.orginalContent !== this.pageContent) {
            let confirm = this.alertController.create({
                title: "Unsaved changes",
                message: "Discard unsaved changes?",
                buttons: [{
                    text: "OK",
                    handler: () => {
                        this.viewController.dismiss({cancel: true});
                    }
                }, {
                    text: "Cancel"
                }]
            });
            confirm.present();
        } else {
            this.viewController.dismiss({cancel: true});
        }
    }
}