import { DropboxSyncService } from '../../library/dropbox-sync-service';
import { WikiPageService } from '../../library/wiki-page-service';
import { LoggingService } from '../../library/logging-service';
import { EditPage } from '../edit/edit';
import { Stack } from '../../library/stack';
import { DomSanitizer } from '@angular/platform-browser';
import { WikiFile } from '../../library/wiki-file';
import { Component, ElementRef, OnInit, Renderer, SecurityContext } from '@angular/core';
import { Loading, LoadingController, ModalController, NavController, ToastController } from 'ionic-angular';
import { DropboxAuthService } from "../../library/dropbox-auth.service";
import { IDropboxAuth } from "../../library/idropbox-auth";

@Component({
  selector: 'page-wiki',
  templateUrl: 'wiki.html'
})
export class WikiPage implements OnInit {
  html = "";
  canGoBack = false;
  pageTitle = "Home";
  isSyncing: boolean;
  syncProgress: string;

  private auth: IDropboxAuth;
  private pageStack = new Stack<WikiFile>();
  private loading: Loading;

  constructor(
    public navCtrl: NavController,
    private dropboxAuthService: DropboxAuthService,
    private dropboxSyncService: DropboxSyncService,
    private loadingController: LoadingController,
    private modalController: ModalController,
    private wikiPageService: WikiPageService,
    private toastController: ToastController,
    private loggingService: LoggingService,
    domSanitizer: DomSanitizer,
    elementRef: ElementRef,
    renderer: Renderer) {

    //allow ema: links
    var realSanitize = domSanitizer.sanitize;
    domSanitizer.sanitize = (context: SecurityContext, value: any): string => {
      var sanitized = realSanitize.apply(domSanitizer, [context, value]);
      sanitized = sanitized.replace(/unsafe:ema:/g, "ema:");
      return sanitized;
    };

    //react on ema: links
    renderer.listen(elementRef.nativeElement, "click", event => {
      if (event.target.nodeName === "A") {
        this.processLinkClick(event.target.href);
      }
      return true;
    });

  }

  ngOnInit() {
    this.dropboxAuthService.getDropboxAuthentication()
      .then((auth: IDropboxAuth) => {
        this.auth = auth;
      })
      .catch(error => this.log("Error initializing Dropbox",error))
      .then(() => this.gotoPage("Home"));
  }

  private processLinkClick(href: string) {
    var emaLinkRegex = /^ema:(.*)$/;
    var m = emaLinkRegex.exec(href);
    if (m) {
      var pageName = m[1];
      this.gotoPage(pageName);
    }
  }

  private showLoading(text: string): Promise<any> {
    if (!this.loading) {
      this.loading = this.loadingController.create({
        content: text
      });

      return this.loading.present();
    }
    return Promise.resolve();
  }

  private gotoPage(pageName: string): Promise<any> {

    this.pageTitle = pageName;
    return this.wikiPageService.getPage(pageName)
      .then((file: WikiFile) => {
        this.html = file.parsed;
        this.pageStack.push(file);
        this.canGoBack = this.pageStack.length > 1;
      })
      .catch(err => this.log("Error loading page " + pageName, err));
  }

  private hideLoading() {
    if (this.loading) {
      this.loading.dismiss();
      this.loading = null;
    }
  }

  private log(what: string, err: any): void {
    what = what || "";
    if (typeof(what) !== "string") {
      what = JSON.stringify(what);
    }
    this.showToast(what);
    console.log(what);
    this.loggingService.log(what, err);
  }

  private showToast(what: string) {
    var toast = this.toastController.create({
      message: what,
      duration: 2000,
      position: "bottom"
    });
    toast.present();
  }

  onBackButton() {
    this.pageStack.pop(); //current page
    let previousPage: string;
    if (this.pageStack.length > 0) {
      previousPage = this.pageStack.pop().pageName;
    } else {
      previousPage = "Home";
    }

    this.gotoPage(previousPage);
  }

  edit() {
    var currentPage = this.pageStack.peek();
    var editModal = this.modalController.create(EditPage, {
      page: currentPage
    });
    editModal.onDidDismiss(data => {
      if (!data.cancel) {
        currentPage.contents = data.pageContent;
        this.showLoading("Saving " + currentPage.pageName)
          .then(() => this.wikiPageService.savePage(currentPage))
          .then(() => this.refresh())
          .catch(err => this.log("Error saving page " + currentPage.pageName, err))
          .then(() => this.hideLoading());
      }
    });
    editModal.present();
  }

  private refresh(): Promise<any> {
    var currentPage = this.pageStack.pop();
    return this.gotoPage(currentPage.pageName);
  }

  sync(): void {
    this.syncProgress = "..%";
    this.isSyncing = true;

    var syncInfo = this.dropboxSyncService.syncFiles(this.auth);

    var syncLabel: string;
    var subscription = syncInfo.progress.subscribe(next => {
      if (next.label && next.label !== syncLabel) {
        this.showToast(next.label);
        syncLabel = next.label;
      }
      if (next.total) {
        var pct = Math.min(Math.round(next.current * 100 / next.total), 99);
        this.syncProgress = pct + "%";
      }
    });

    syncInfo.promise
      .catch(err => this.log("Error while synchronizing", err))
      .then(() => {
        subscription.unsubscribe();
        this.isSyncing = false;
        this.refresh();
      });
  }
}

