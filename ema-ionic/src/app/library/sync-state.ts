import { StoredFile } from './stored-file';
import { Subject } from 'rxjs';
import { IDropboxEntry } from './idropbox-entry';
import { LoggingService } from './logging-service';

export class SyncProgress {
    total: number;
    current: number;
    label: string;
}

export class SyncState {
    localSyncInfo: IDropboxEntry[];
    allRemoteFiles: IDropboxEntry[];
    remoteChangedFiles: IDropboxEntry[];
    allLocalFiles: StoredFile[];
    localChangedFiles: StoredFile[];
    localDeletedFiles: IDropboxEntry[];
    failedFiles: string[] = [];
    promise: Promise<any>;
    progress = new Subject<SyncProgress>();

    syncProgress = new SyncProgress();

    constructor(private loggingService: LoggingService) {
    }

    setTotalSteps(total: number): void {
        this.syncProgress.total = total;
        this.syncProgress.current = 0;
        this.makingProgress();
    }

    makingProgress(label?: string): void {
        if (this.syncProgress.total) {
            this.syncProgress.current += 1;
        }
        if (label) {
            this.syncProgress.label = label;
        }
        this.progress.next(this.syncProgress);
    }
}
