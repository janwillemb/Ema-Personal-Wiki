import { Injectable } from '@angular/core';
import { Utils } from './utils';

@Injectable()
export class LoggingService {
    private logLines: string[] = [];

    log(what: string, err?: any): void {
        if (err) {
            what += ': ' + Utils.serializeError(err);
        }
        this.logLines.push(what);
    }

    consumeLogLines(): string[] {
        const logLines = this.logLines;
        this.logLines = [];
        return logLines;
    }
}