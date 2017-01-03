import { Injectable } from '@angular/core';
declare function require(name: string);

@Injectable()
export class LoggingService {
    private logLines: string[] = [];
    private serializeError = require("serialize-error");

    log(what: string, err?: any): void {
        if (err) {
            what += ": " + JSON.stringify(this.serializeError(err));
        }
        this.logLines.push(what);
    }

    consumeLogLines(): string[] {
        let logLines = this.logLines;
        this.logLines = [];
        return logLines;
    }
}