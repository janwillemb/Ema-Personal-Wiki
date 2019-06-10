import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { Settings } from '../../library/settings';
import { WikiFile } from '../../library/wiki-file';
import { AlertController, NavController, NavParams, ModalController, Platform } from '@ionic/angular';
import { Component, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
    selector: 'page-edit',
    templateUrl: 'edit.html',
    styleUrls: ['edit.scss'],
})
export class EditPage {
    pageContent: string;
    pageTitle: string;
    orginalContent: string;
    fontPctStyle: SafeStyle;

    @ViewChild('pageContentTextArea') pageContentTextArea;
    private backbuttonSubscription: Subscription;

    constructor(
        navParams: NavParams,
        settings: Settings,
        private sanitizer: DomSanitizer,
        private alertController: AlertController,
        private modalController: ModalController,
        private platform: Platform) {

        const page: WikiFile = navParams.get('page');
        this.pageTitle = page.pageName;
        this.pageContent = page.contents;
        this.orginalContent = this.pageContent;

        this.fontPctStyle = this.sanitizer.bypassSecurityTrustStyle('font-size: ' + settings.getFontSize() + '%');
    }

    ionViewDidEnter() {
        this.pageContentTextArea.nativeElement.focus();
        this.backbuttonSubscription = this.platform.backButton.subscribe(() => this.onBackButton());
    }

    save() {
        this.close({ pageContent: this.pageContent });
    }

    private close(result: any) {
        this.modalController.dismiss(result);
        this.backbuttonSubscription.unsubscribe();
    }

    async delete() {
        const confirm = await this.alertController.create({
            header: 'Delete page',
            message: 'Do you really want to delete this page?',
            buttons: [{
                text: 'Yes',
                handler: () => {
                    this.close({ delete: true });
                }
            }, {
                text: 'No'
            }]
        });
        confirm.present();
    }

    onBackButton() {
        this.cancel();
    }

    async cancel() {
        if (this.orginalContent !== this.pageContent) {
            const confirm = await this.alertController.create({
                header: 'Unsaved changes',
                message: 'Discard unsaved changes?',
                buttons: [{
                    text: 'OK',
                    handler: () => {
                        this.close({ cancel: true });
                    }
                }, {
                    text: 'Cancel'
                }]
            });
            confirm.present();
        } else {
            this.close({ cancel: true });
        }
    }
}
