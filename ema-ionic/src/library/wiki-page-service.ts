import { MarkdownerService } from './markdowner.service';
import { LoggingService } from './logging-service';
import { WikiStorage } from './wiki-storage';
import { WikiFile } from './wiki-file';
import { StoredFile } from './stored-file';
import { Injectable } from '@angular/core';

@Injectable()
export class WikiPageService {
    constructor(
        private wikiStorage: WikiStorage, 
        private loggingService: LoggingService, 
        private markdownerService: MarkdownerService) {
    }

    getPage(name: string): Promise<WikiFile> {
        return this.wikiStorage.getFileContents(this.getPageFileName(name))
            .catch(err => {
                this.loggingService.log("Error getting " + name + ".txt from store", err);
                return new StoredFile("", "");
            })
            //make it a wiki file 
            .then(file => new WikiFile(name, file.contents || "(empty page)"))
            //convert  the markdown to html
            .then((file: WikiFile) =>
                this.markdownerService.process(name, file.contents).then((parsed: string) => {
                    file.parsed = parsed;
                    return file;
                }));
    }

    savePage(page: WikiFile): Promise<any> {
        return this.wikiStorage.save(this.getPageFileName(page.pageName), page.contents);
    }

    private getPageFileName(pageName: string): string {
        return encodeURIComponent(pageName + ".txt");
    }
}