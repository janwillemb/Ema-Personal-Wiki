import { SettingsPage } from '../pages/settings/settings';
import { DropboxSyncService } from '../library/dropbox-sync-service';
import { WikiPageService } from '../library/wiki-page-service';
import { LogsPage } from '../pages/logs/logs';
import { LoggingService } from '../library/logging-service';
import { WikiStorage } from '../library/wiki-storage';
import { EditPage } from '../pages/edit/edit';
import { MarkdownerService } from '../library/markdowner.service';
import { Settings } from '../library/settings';
import { DropboxFileService } from '../library/dropbox-get-file.service';
import { DropboxListFilesService } from '../library/dropbox-list-files.service';
import { DropboxAuthService } from "../library/dropbox-auth.service";
import { NgModule, ErrorHandler } from '@angular/core';
import { IonicApp, IonicModule, IonicErrorHandler } from 'ionic-angular';
import { Storage } from '@ionic/storage';
import { WikiPage } from '../pages/wiki/wiki';
import { MyApp } from './app.component';

@NgModule({
  declarations: [
    MyApp,
    SettingsPage,
    EditPage,
    LogsPage,
    WikiPage
  ],
  imports: [
    IonicModule.forRoot(MyApp)
  ],
  bootstrap: [IonicApp],
  entryComponents: [
    MyApp,
    SettingsPage,
    LogsPage,
    EditPage,
    WikiPage
  ],
  providers: [
    DropboxAuthService,
    DropboxListFilesService,
    DropboxFileService,
    DropboxSyncService,
    LoggingService,
    MarkdownerService,
    Settings,
    Storage,
    WikiStorage,
    WikiPageService,
    {provide: ErrorHandler, useClass: IonicErrorHandler},
  ]
})
export class AppModule {}
