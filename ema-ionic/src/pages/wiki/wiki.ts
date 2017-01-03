import { SyncState } from '../../library/sync-state';
import { MyApp } from '../../app/app.component';
import { Settings } from '../../library/settings';
import { DropboxSyncService } from '../../library/dropbox-sync-service';
import { WikiPageService } from '../../library/wiki-page-service';
import { LoggingService } from '../../library/logging-service';
import { EditPage } from '../edit/edit';
import { Stack } from '../../library/stack';
import { DomSanitizer } from '@angular/platform-browser';
import { WikiFile } from '../../library/wiki-file';
import { Component, ElementRef, Renderer, SecurityContext } from '@angular/core';
import { Loading, LoadingController, Modal, ModalController, NavController, ToastController } from 'ionic-angular';
import { DropboxAuthService } from "../../library/dropbox-auth.service";
import { IDropboxAuth } from "../../library/idropbox-auth";
declare function require(name: string);

@Component({
  selector: 'page-wiki',
  templateUrl: 'wiki.html'
})
export class WikiPage {
  html = "";
  canGoBack = false;
  pageTitle: string;
  isHome: boolean;

  isSyncing: boolean;
  syncProgress: string;
  lastSync: Date;

  styleGrey: boolean;
  searchTerm: string;
  showSearch: boolean;
  canEdit: boolean;

  private pageStack = new Stack<WikiFile>();
  private loading: Loading;
  private serializeError = require("serialize-error");
  private homePageName = "Home";
  private editModal: Modal;
  private syncInterval: number;

  constructor(
    private navCtrl: NavController,
    private dropboxAuthService: DropboxAuthService,
    private dropboxSyncService: DropboxSyncService,
    private loadingController: LoadingController,
    private modalController: ModalController,
    private wikiPageService: WikiPageService,
    private toastController: ToastController,
    private loggingService: LoggingService,
    private settings: Settings,
    domSanitizer: DomSanitizer,
    elementRef: ElementRef,
    renderer: Renderer) {

    this.searchTerm = "";
    this.pageTitle = this.settings.getLastPageName() || this.homePageName;
    this.isHome = true;

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

    this.applySettings();
  }

  ionViewDidLoad() {
    //go to the last visited page, or "Home"
    var firstPage = this.homePageName;
    if (this.settings.getRestoreLast()) {
      firstPage = this.settings.getLastPageName() || this.homePageName;
    }
    this.gotoPage(firstPage);
  }

  ionViewWillEnter() {
    //re-evaluate settings
    this.applySettings();
    //(re)start auto-sync
    this.planAutoSync()
  }

  ionViewDidLeave() {
    clearInterval(this.syncInterval);
  }

  private applySettings() {
    this.styleGrey = this.settings.getStyle() === "Grey";
    this.showSearch = this.settings.getShowSearch();
  }

  private planAutoSync() {
    let isHandlingInterval = false;
    this.syncInterval = setInterval(() => {
      if (isHandlingInterval) {
        //after a while inactivity, the browser apparently will fire the interval for all "forgotten" periods
        return;
      }
      isHandlingInterval = true;
      var syncMinutes = this.settings.getSyncMinutes();
      var shouldSync = true;
      if (this.lastSync) {
        var diff = new Date().getTime() - this.lastSync.getTime();
        var diffMinutes = diff / 60000

        shouldSync = diffMinutes >= syncMinutes;
      }

      if (shouldSync) {
        if (this.dropboxAuthService.hasAuthenticatedWithDropbox()) {
          this.sync();
        }
      }
      isHandlingInterval = false;
    }, 10000);
  }

  private processLinkClick(href: string) {
    href = decodeURI(href);
    var emaLinkRegex = /^ema:(.*)$/;
    var m = emaLinkRegex.exec(href);
    if (m) {
      var pageName = m[1];
      this.gotoPage(pageName);
    } else {
      window.open(href, "_system");
      this.showToast("Opening external link...");
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

  goHome(): Promise<any> {
    return this.gotoPage(this.homePageName);
  }

  private gotoPage(pageName: string): Promise<any> {
    return this.wikiPageService.getPage(pageName)
      .then((file: WikiFile) => this.showPage(file))
      .catch(err => this.log("Error loading page " + pageName, err));
  }

  private showPage(page: WikiFile) {
    this.pageTitle = page.pageName;
    this.html = page.parsed;
    this.pageStack.push(page);
    this.canGoBack = this.pageStack.length > 1;
    this.isHome = page.pageName === this.homePageName;
    this.canEdit = !page.isSearchResults;

    if (!page.isSearchResults) {
      //keep last page for next time the wiki is started
      this.settings.setLastPageName(page.pageName);
    }
  }

  private hideLoading() {
    if (this.loading) {
      this.loading.dismiss();
      this.loading = null;
    }
  }

  private log(what: string, err: any): void {
    what = what || "";
    if (typeof (what) !== "string") {
      what = JSON.stringify(this.serializeError(what));
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

  search() {
    if (this.searchTerm) {
      this.showLoading("Searching...")
        .then(() => this.wikiPageService.getSearchResultsFor(this.searchTerm))
        .then((result: WikiFile) => this.showPage(result))
        .catch(err => this.log("Error while searching", err))
        .then(() => this.hideLoading());
    }
  }

  onBackButton() {
    if (this.editModal) {
      this.editModal.instance.onBackButton();
      return;
    }

    var currentPage = this.pageStack.pop(); //current page

    if (!this.canGoBack) {
      if (currentPage.pageName !== this.homePageName) {
        this.goHome();
      } else {
        MyApp.instance.exit();
      }
      return;
    }

    //go back within wiki
    if (this.pageStack.length > 0) {
      var previousPage = this.pageStack.pop();
      if (previousPage.isSearchResults) {
        this.showPage(previousPage);
      } else {
        this.gotoPage(previousPage.pageName);
      }
    } 
  }

  edit() {
    var currentPage = this.pageStack.peek();
    this.editModal = this.modalController.create(EditPage, {
      page: currentPage
    });
    this.editModal.onDidDismiss(data => {
      this.editModal = null;
      if (!data.cancel) {
        currentPage.contents = data.pageContent;
        this.showLoading("Saving " + currentPage.pageName)
          .then(() => this.wikiPageService.savePage(currentPage))
          .then(() => this.refresh())
          .catch(err => this.log("Error saving page " + currentPage.pageName, err))
          .then(() => this.hideLoading());
      }
    });
    this.editModal.present();
  }

  private refresh(): Promise<any> {
    var currentPage = this.pageStack.pop();
    return this.gotoPage(currentPage.pageName);
  }

  sync(): void {
    if (this.isSyncing) {
      return;
    }
    this.syncProgress = "0%";
    this.isSyncing = true;
    this.lastSync = new Date();

    var subscription;
    var syncInfo: SyncState;
    this.dropboxAuthService.getDropboxAuthentication()
      .then((auth: IDropboxAuth) => {
        syncInfo = this.dropboxSyncService.syncFiles(auth);

        var syncLabel: string;
        subscription = syncInfo.progress.subscribe(next => {
          if (next.label && next.label !== syncLabel) {
            //this.showToast(next.label);
            syncLabel = next.label;
          }
          if (next.total) {
            var pct = Math.min(Math.round(next.current * 100 / next.total), 99);
            this.syncProgress = pct + "%";
          }
        });

        return syncInfo.promise;
      })
      .catch(err => this.log("Error while synchronizing", err))
      .then(() => {
        if (syncInfo && syncInfo.failedFiles.length > 0) {
          this.log("Sync finished with errors", syncInfo.failedFiles);
        }
        if (subscription) {
          subscription.unsubscribe();
        }
        this.isSyncing = false;
        this.refresh();
      });
  }
}

