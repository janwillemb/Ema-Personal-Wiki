import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouteReuseStrategy } from '@angular/router';

import { SplashScreen } from '@ionic-native/splash-screen/ngx';
import { StatusBar } from '@ionic-native/status-bar/ngx';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { IonicStorageModule } from '@ionic/storage';
import { SettingsPage } from './pages/settings/settings';
import { EditPage } from './pages/edit/edit';
import { HelpPage } from './pages/help/help';
import { TagPage } from './pages/tag/tag';
import { LogsPage } from './pages/logs/logs';
import { WikiPage } from './pages/wiki/wiki';
import { DropboxAuthService } from './library/dropbox-auth.service';
import { DropboxListFilesService } from './library/dropbox-list-files.service';
import { DropboxFileService } from './library/dropbox-get-file.service';
import { DropboxSyncService } from './library/dropbox-sync-service';
import { LoggingService } from './library/logging-service';
import { MarkdownerService } from './library/markdowner.service';
import { Settings } from './library/settings';
import { TagIndexService } from './library/tag-index.service';
import { WikiStorage } from './library/wiki-storage';
import { WikiPageService } from './library/wiki-page-service';
import { FormsModule } from '@angular/forms';
import { File } from '@ionic-native/file/ngx';
import { HttpClientModule } from '@angular/common/http';
import { Clipboard } from '@ionic-native/clipboard/ngx';
import { InAppBrowser } from '@ionic-native/in-app-browser/ngx';
import { AppMinimize } from '@ionic-native/app-minimize/ngx';
import { IonicModule, IonicRouteStrategy } from '@ionic/angular';
import { PubSubService } from './library/pub-sub.service';
import { AndroidPermissions } from '@ionic-native/android-permissions/ngx';
import { FileOpener } from '@ionic-native/file-opener/ngx';

@NgModule({
    declarations: [
        AppComponent,
        SettingsPage,
        EditPage,
        HelpPage,
        TagPage,
        LogsPage,
        WikiPage],
    entryComponents: [
        SettingsPage,
        LogsPage,
        HelpPage,
        TagPage,
        EditPage,
        WikiPage],
    imports: [
        BrowserModule,
        IonicModule.forRoot(),
        IonicStorageModule.forRoot(),
        AppRoutingModule,
        FormsModule,
        HttpClientModule
    ],
    providers: [
        AndroidPermissions,
        FileOpener,
        PubSubService,
        Clipboard,
        DropboxAuthService,
        DropboxListFilesService,
        DropboxFileService,
        InAppBrowser,
        AppMinimize,
        DropboxSyncService,
        LoggingService,
        MarkdownerService,
        Settings,
        TagIndexService,
        WikiStorage,
        WikiPageService,
        StatusBar,
        SplashScreen,
        File,
        { provide: RouteReuseStrategy, useClass: IonicRouteStrategy }
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
