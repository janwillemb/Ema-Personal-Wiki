import { TagIndexService } from './tag-index.service';
import { SearchResult } from './search-result';
import { MarkdownerService } from './markdowner.service';
import { LoggingService } from './logging-service';
import { WikiStorage } from './wiki-storage';
import { WikiFile } from './wiki-file';
import { StoredFile } from './stored-file';
import { Injectable, Injector } from '@angular/core';
import * as XRegExp from 'xregexp';

@Injectable()
export class WikiPageService {
    requestedNextPage: string;

    constructor(
        private wikiStorage: WikiStorage,
        private loggingService: LoggingService,
        private tagIndexService: TagIndexService,
        private markdownerService: MarkdownerService) {
    }

    checkStorage(): Promise<boolean> {
        return this.wikiStorage.checkStorage();
    }

    getPage(name: string): Promise<WikiFile> {
        return this.wikiStorage.getFileContents(this.getPageFileName(name))
            .catch(err => {
                this.loggingService.log('Error getting ' + name + '.txt from store', err);
                return new StoredFile('', '');
            })
            // convert the markdown to html
            .then(storedFile => this.markdownerService.process(storedFile))
            .then(wikiFile => {
                wikiFile.pageName = name;
                return wikiFile;
            });
    }

    savePage(page: WikiFile): Promise<any> {
        const fileName = this.getPageFileName(page.pageName);
        return this.wikiStorage.save(fileName, page.contents)
            .then(() => this.tagIndexService.afterSaveFile(fileName, page.contents));
    }

    deletePage(page: WikiFile): Promise<any> {
        const fileName = this.getPageFileName(page.pageName);
        return this.wikiStorage.delete(fileName)
            .then(() => this.tagIndexService.afterSaveFile(fileName, ''));
    }

    /**
     * returns a virtual wiki page with search results 
     */
    getSearchResultsFor(query: string): Promise<WikiFile> {
        const regex = XRegExp(query, 'gi');

        return this.wikiStorage.listFiles()
            .then(files => Promise.all(files.filter(f => f.endsWith('.txt')).map(f => this.search(f, regex))))
            .then((results: SearchResult[]) => results
                .filter(x => x.relevance > 0)
                .sort((a: SearchResult, b: SearchResult) => {
                    if (a.relevance > b.relevance) {
                        return -1;
                    }
                    if (a.relevance === b.relevance) {
                        return 0;
                    }
                    return 1;
                }))
            .then((results: SearchResult[]) => {
                const divs = results.map(x => {
                    const pageLink = MarkdownerService.createWikiLink(x.pageName);
                    return '<div class=\'search-result\'><div class=\'search-result-title\'>' + pageLink + '</div>' +
                        '<div class=\'search-result-snippet\'>...' + x.snippet + '...</div></div>';
                });

                const page = new WikiFile('', divs.join('<hr/>'));
                page.pageName = query;

                if (divs.length === 0) {
                    page.parsed = '(no results found)';
                }

                page.isSearchResults = true;

                return page;
            });
    }

    private search(file: string, regex: any): Promise<SearchResult> {
        const maxHits = 10;

        return this.wikiStorage.getFileContents(file)
            .then(storedFile => {
                const pageName = this.getPageNameFromFile(storedFile.fileName);
                const result = new SearchResult(pageName);

                let m = regex.exec(pageName);
                if (m) {
                    result.relevance += 1;
                }

                let firstIndex = -1;
                // tslint:disable-next-line: no-conditional-assignment
                while ((m = regex.exec(storedFile.contents.toString())) !== null) {
                    firstIndex = m.index;
                    result.relevance += 1;

                    if (result.relevance === maxHits) {
                        break;
                    }
                }

                if (result.relevance) {
                    if (firstIndex > -1) {
                        result.snippet = storedFile.contents.toString().substring(
                            Math.max(0, firstIndex - 20),
                            Math.min(storedFile.contents.toString().length - 1, firstIndex + 25));

                        let hits = 0;
                        result.snippet = result.snippet.replace(regex, match => {
                            hits += 1;
                            if (hits > maxHits) {
                                return match;
                            }
                            return '<strong class=\'highlight\'>' + match + '</strong>';
                        });
                    } else {
                        result.snippet = '(only page title matches)';
                    }
                }

                return result;
            });
    }

    private getPageFileName(pageName: string): string {
        const invalidCharsRegex = XRegExp('[^\\w\\-\\.]', 'gx');
        return pageName.replace(invalidCharsRegex, '_') + '.txt';
    }

    getPageNameFromFile(fileName: string): string {
        const page = fileName;
        return page.substring(0, page.length - '.txt'.length);
    }
}
