import { Injectable } from '@angular/core';
@Injectable()
export class LoggingService {
    private logLines: string[] = [];

    log(what: string, err?: any): void {
        if (err) {
            what += ": " + JSON.stringify(err);
        }
        this.logLines.push(what);
    }

    consumeLogLines(): string[] {
        let logLines = this.logLines;
        this.logLines = [];
        return logLines;
    }
}