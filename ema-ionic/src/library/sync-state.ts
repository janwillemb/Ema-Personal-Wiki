import { StoredFile } from './stored-file';
import { Subject } from 'rxjs/Rx';
import { IDropboxEntry } from './idropbox-entry';
export class SyncState {
    localSyncInfo: IDropboxEntry[];
    allRemoteFiles: IDropboxEntry[];
    remoteChangedFiles: IDropboxEntry[];
    allLocalFiles: StoredFile[];
    localChangedFiles: StoredFile[];
    localDeletedFiles: IDropboxEntry[];
    promise: Promise<any>;
    progress = new Subject<SyncProgress>();

    private syncProgress = new SyncProgress();

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

export class SyncProgress {
    total: number;
    current: number;
    label: string;
}