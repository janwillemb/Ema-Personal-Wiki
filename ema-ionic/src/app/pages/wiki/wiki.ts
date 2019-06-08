import { WikiStorage } from '../../library/wiki-storage';
import { SyncState } from '../../library/sync-state';
import { Settings } from '../../library/settings';
import { DropboxSyncService } from '../../library/dropbox-sync-service';
import { WikiPageService } from '../../library/wiki-page-service';
import { LoggingService } from '../../library/logging-service';
import { EditPage } from '../edit/edit';
import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { WikiFile } from '../../library/wiki-file';
import { Component, ElementRef, SecurityContext, OnInit, Renderer2 } from '@angular/core';
import { LoadingController, ModalController, NavController, ToastController, Platform } from '@ionic/angular';
import { DropboxAuthService } from '../../library/dropbox-auth.service';
import { IDropboxAuth } from '../../library/idropbox-auth';
import { ActivatedRoute, Router, } from '@angular/router';
import { Utils } from 'src/app/library/utils';
import { Stack } from 'src/app/library/stack';
import { AppComponent } from 'src/app/app.component';
import { PubSubService, EmaSubscription } from 'src/app/library/pub-sub.service';

@Component({
    selector: 'page-wiki',
    templateUrl: 'wiki.html',
    styleUrls: ['wiki.scss'],
})
export class WikiPage {

    html = '';
    tags: string[];
    pageTitle: string;
    isHome: boolean;
    isSearch: boolean;

    isSyncing: boolean;
    syncProgress: string;
    lastSync: Date;

    styleGrey = true;
    searchTerm: string;
    showSearch: boolean;
    canEdit: boolean;
    fileAccess: boolean;
    isInitializing: boolean;
    fontPctStyle: SafeStyle;

    private loading: any;
    private homePageName = 'Home';
    private syncInterval: any;
    private lastTap: Date;
    private currentPage: WikiFile;
    private initializingPromise: Promise<any>;
    private initialPageName: string;
    private subscriptions: EmaSubscription[] = [];
    private pageStack = new Stack<string>();
    private isEditing = false;
    private isWikiPageOnTop = true;

    constructor(
        private route: ActivatedRoute,
        private pubSubService: PubSubService,
        private sanitizer: DomSanitizer,
        private loadingController: LoadingController,
        private modalController: ModalController,
        private navController: NavController,
        private toastController: ToastController,
        private loggingService: LoggingService,
        private settings: Settings,
        private wikiPageService: WikiPageService,
        private dropboxAuthService: DropboxAuthService,
        private dropboxSyncService: DropboxSyncService,
        elementRef: ElementRef,
        renderer: Renderer2
    ) {
        this.searchTerm = '';
        this.pageTitle = '...';
        this.isHome = true;
        this.isInitializing = true;

        // react on ema: links
        renderer.listen(elementRef.nativeElement, 'click', event => {
            if (event.target.nodeName === 'A') {
                this.processLinkClick(event.target.href);
            }
            return true;
        });

        // allow ema: links
        const realSanitize = sanitizer.sanitize;
        sanitizer.sanitize = (context: SecurityContext, value: any): string => {
            const personalWikiDir = WikiStorage.storageDir + this.settings.getLocalWikiDirectory();
            let sanitized = realSanitize.apply(sanitizer, [context, value]);
            sanitized = sanitized.replace(/unsafe:ema:/g, 'ema:');
            sanitized = sanitized.replace(/unsafe:emafile:/g, personalWikiDir + '/');
            return sanitized;
        };

        setInterval(() => {
            this.isWikiPageOnTop = window.location.href.indexOf('/wiki/') !== -1;
        }, 100);

    }

    ionViewDidEnter() {

        let firstPage = this.wikiPageService.requestedNextPage || this.route.snapshot.paramMap.get('page');
        this.wikiPageService.requestedNextPage = null;

        if (this.pageStack.length > 1) {
            // apparently returning from another view.
            firstPage = this.pageStack.peek();
        }

        this.gotoPage(firstPage);

        const settingsPromise = this.settings.waitForInitialize().then(() => {
            this.applySettings();

            // go to the last visited page, or "Home"
            this.initialPageName = this.homePageName;
            if (this.settings.getRestoreLast()) {
                this.initialPageName = this.settings.getLastPageName() || this.homePageName;
            }

            // (re)start auto-sync
            this.planAutoSync();
        });

        const storagePromise = this.wikiPageService.checkStorage().then(() => {
            this.fileAccess = true;
        });

        this.initializingPromise = Promise.all([settingsPromise, storagePromise]);

        const backsub = this.pubSubService.subscribe('back', () => this.onBackButton());
        this.subscriptions.push(backsub);
        const syncsub = this.pubSubService.subscribe('sync', () => this.sync());
        this.subscriptions.push(syncsub);
    }

    private onBackButton() {
        if (this.isEditing) {
            return;
        }
        if (!this.isWikiPageOnTop) {
            return;
        }

        if (this.pageStack.length > 1) {
            this.goBack();
        } else {
            AppComponent.instance.minimize();
        }
    }

    private goBack() {
        if (this.pageStack.length > 1) {
            this.pageStack.pop(); // current page
            const pageName = this.pageStack.pop();
            if (pageName.startsWith('ema-search:')) {
                this.searchTerm = pageName.substr('ema-search:'.length);
                this.search();
            } else {
                this.gotoPage(pageName);
            }
        } else {
            this.goHome();
        }
    }

    ionViewDidLeave() {
        if (this.syncInterval) {
            clearInterval(this.syncInterval);
        }

        this.subscriptions.forEach(x => x.unsubscribe());
        this.subscriptions = [];
    }

    private async doSearch(term: string) {
        await this.showLoading('Searching...');
        try {
            const result = await this.wikiPageService.getSearchResultsFor(term);
            this.pageStack.push('ema-search:' + term);
            this.showPage(result);
        } catch (err) {
            this.log('Error while searching', err);
        }

        this.hideLoading();
    }

    private applySettings() {
        this.styleGrey = this.settings.getStyle() === 'Blue';
        this.showSearch = this.settings.getShowSearch();
        this.fontPctStyle = this.sanitizer.bypassSecurityTrustStyle('font-size: ' + this.settings.getFontSize() + '%');
    }

    private planAutoSync() {
        let isHandlingInterval = false;

        if (!this.settings.getAutoSync()) {
            return;
        }

        const doAutoSync = async () => {
            if (isHandlingInterval) {
                // after a while inactivity, the browser apparently will fire the interval for all "forgotten" periods
                return;
            }
            isHandlingInterval = true;
            const syncMinutes = this.settings.getSyncMinutes();
            let shouldSync = true;
            if (this.lastSync) {
                const diff = new Date().getTime() - this.lastSync.getTime();
                const diffMinutes = diff / 60000;

                shouldSync = diffMinutes >= syncMinutes;
            }

            if (shouldSync) {
                if (this.dropboxAuthService.hasAuthenticatedWithDropbox()) {
                    await this.sync();
                }
            }
            isHandlingInterval = false;
        };

        this.syncInterval = setInterval(doAutoSync, 10000);
    }

    get canGoBack(): boolean {
        return this.pageStack.length > 1;
    }

    private navigateTo(pageName: string) {
        this.gotoPage(pageName);
    }

    private processLinkClick(href: string) {
        href = decodeURI(href);
        const emaLinkRegex = /^ema:(.*)$/;
        const m = emaLinkRegex.exec(href);
        if (m) {
            const pageName = m[1];
            this.navigateTo(pageName);
        } else {
            window.open(href, '_system');
            this.showToast('Opening external link...');
        }
    }

    private async showLoading(text: string): Promise<any> {
        if (!this.loading) {
            this.loading = await this.loadingController.create({
                message: text
            });

            return this.loading.present();
        }
        return Promise.resolve();
    }

    goHome(): void {
        this.pageStack.clear();
        this.navigateTo(this.homePageName);
    }

    private async gotoPage(pageName: string): Promise<any> {

        await this.showLoading('loading page');

        try {
            await this.initializingPromise;
            const isDefault = pageName === '_default';
            pageName = isDefault ? this.initialPageName : pageName;

            if (this.pageStack.peek() !== pageName) {
                this.pageStack.push(pageName);
            }

            const file = await this.wikiPageService.getPage(pageName);
            this.showPage(file);

        } catch (err) {
            this.log('Error loading page ' + pageName, err);
        }
        this.hideLoading();
    }

    gotoTag(tag: string) {
        this.navController.navigateForward(['/tag/', tag]);
    }

    private showPage(page: WikiFile) {
        this.currentPage = page;
        this.pageTitle = page.pageName;
        this.html = page.parsed;

        this.isHome = page.pageName === this.homePageName;
        this.canEdit = !page.isSearchResults;
        this.isSearch = page.isSearchResults;
        if (page.tags && page.tags.length > 0) {
            this.tags = page.tags;
        } else {
            this.tags = null;
        }

        if (!page.isSearchResults) {
            // keep last page for next time the wiki is started
            this.settings.setLastPageName(page.pageName);
        }

        this.isInitializing = false;
    }

    private hideLoading() {
        if (this.loading) {
            this.loading.dismiss();
            this.loading = null;
        }
    }

    private log(what: string, err: any): void {
        what = what || '';
        if (typeof (what) !== 'string') {
            what = Utils.serializeError(what);
        }
        this.loggingService.log(what, err);
        this.showToast(what);
    }

    private async showToast(what: string) {
        const toast = await this.toastController.create({
            message: what,
            duration: 2000,
            position: 'bottom'
        });
        toast.present();
    }

    search() {
        if (this.searchTerm) {
            this.doSearch(this.searchTerm);
        }
    }

    onTap() {
        const tapTime = new Date();
        if (this.lastTap) {
            if (tapTime.getTime() - this.lastTap.getTime() < 500) {
                // double-tap
                this.edit();
            }
        }
        this.lastTap = tapTime;
    }

    async edit() {
        const currentPage = this.currentPage;
        this.isEditing = true;
        const editModal = await this.modalController.create({
            component: EditPage,
            componentProps: { page: currentPage },
            backdropDismiss: false
        });

        await editModal.present();
        const { data } = await editModal.onDidDismiss();

        if (!data.cancel) {
            if (data.delete) {
                try {
                    await this.showLoading('Deleting ' + currentPage.pageName);
                    await this.wikiPageService.deletePage(currentPage);
                    this.goBack();
                } catch (err) {
                    this.log('Error deleting page ' + currentPage.pageName, err);
                }
            } else {
                currentPage.contents = data.pageContent;
                await this.showLoading('Saving ' + currentPage.pageName);
                try {
                    await this.wikiPageService.savePage(currentPage);
                    await this.refresh();
                } catch (err) {
                    this.log('Error saving page ' + currentPage.pageName, err);
                }
            }
            this.hideLoading();
        }

        this.isEditing = false;
    }

    private refresh(): Promise<any> {
        if (this.currentPage.isSearchResults) {
            return Promise.resolve();
        }
        return this.gotoPage(this.currentPage.pageName);
    }

    public async sync() {
        if (this.isSyncing) {
            return;
        }
        this.syncProgress = '0%';
        this.isSyncing = true;
        this.lastSync = new Date();

        let subscription;
        let syncInfo: SyncState;
        await this.dropboxAuthService.getDropboxAuthentication()
            .then((auth: IDropboxAuth) => {
                this.loggingService.log('start sync');
                syncInfo = this.dropboxSyncService.syncFiles(auth);

                let syncLabel: string;
                subscription = syncInfo.progress.subscribe(next => {
                    if (next.label && next.label !== syncLabel) {
                        syncLabel = next.label;
                    }
                    if (next.total) {
                        const pct = Math.min(Math.round(next.current * 100 / next.total), 99);
                        this.syncProgress = pct + '%';
                    }
                });

                return syncInfo.promise;
            })
            .catch(err => this.log('Error while synchronizing', err))
            .then(() => {
                if (syncInfo && syncInfo.failedFiles.length > 0) {
                    this.log('Sync finished with errors', syncInfo.failedFiles);
                }
                if (subscription) {
                    subscription.unsubscribe();
                }
                this.isSyncing = false;
                this.refresh();
            });
    }
}

