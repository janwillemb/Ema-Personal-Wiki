import { WikiFile } from './wiki-file';
import { StoredFile } from './stored-file';
import { TagIndexService } from './tag-index.service';
import { Settings } from './settings';
import { Queue } from './queue';
import { LoggingService } from './logging-service';
import { Injectable } from '@angular/core';
import * as marked from 'marked';
import * as XRegExp from 'xregexp';


@Injectable()
export class MarkdownerService {

    constructor(
        private settings: Settings,
        private tagIndexService: TagIndexService,
        private loggingService: LoggingService) {

        this.initializeRegex();
    }

    private wikiWordsRegex;
    private htmlTagsRegex = XRegExp('\<a\s.+?\<\/a\>|\<[^\>]+\>', 'g');
    private usedCurly: boolean;

    static createWikiLink(wikiword: string) {
        return `<a href="ema:${encodeURI(wikiword)}">${wikiword}</a>`;
    }

    private initializeRegex() {
        this.wikiWordsRegex = XRegExp(
            '(~)?' +         // remember the previous character if it is the ignore marker
            '(?:' +          // #start the 'or' group
            '(?<wikiword1>' +  // #bare wikiword group
            '\\p{Lu}' +        // #start with uppercase letter
            '\\p{Ll}+' +       // #one or more lowercase letters
            '\\p{Lu}' +        // #one uppercase letter
            '\\w*)' +          // #and zero or more arbitrary characters in the same word
            (this.settings.getUseCurly() ? // START legacy option curly brackets
                '|' +              // #or
                '\\{' +           // #start with a curly bracket
                '(?<wikiword2>' + // #curly bracket wikiword group
                '[^\{\\}]+' +     // #anything inbetween that is not curly bracket
                ')\\}'            // #end with curly br
                : '') +               // END legacy option curly brackets
            '|' +              //  #or
            '\\[\\[' +         //  #double square bracket
            '(?<wikiword3>' + //   #square bracket wikiword group
            '[^\\]]+' +        //  #anything inbetween not being a (single) square bracket
            ')\\]\\]' +         // #end with double square bracket
            ')'        //          #close the 'or' group
            , 'gx'); // match multiple times

        this.usedCurly = this.settings.getUseCurly();
    }

    process(storedFile: StoredFile): Promise<WikiFile> {
        const emaPlaceholder = '<ema-placeholder>';

        return new Promise((resolve, reject) => {
            let result: WikiFile;
            try {
                if (!storedFile.contents) {
                    result = new WikiFile('', '');
                    result.tags = [];
                    resolve(result);
                    return;
                }

                // first remove the tags from the content
                const tagsFromContent = this.tagIndexService.separateTagsFromContent(storedFile.contents.toString());
                const tags = tagsFromContent.tags;

                // do markdown
                const markedDown = marked(tagsFromContent.strippedContent);

                // hide html from the resulting text, because the HTML is highly likely
                // to contain text that will be recognized as wikiwords (links for example)
                const queue = new Queue<string>();
                const protectedMarkedDown = markedDown.replace(this.htmlTagsRegex, match => {
                    queue.enqueue(match);
                    return emaPlaceholder;
                });

                const wikiworded = this.wikiword(protectedMarkedDown);

                // restore HTML tags
                const processed = wikiworded.replace(this.htmlTagsRegex, match => {
                    if (match === emaPlaceholder) {
                        return queue.dequeue();
                    }
                    return match; // new wikiworded link
                });

                result = new WikiFile(storedFile.contents.toString(), processed);
                result.tags = tags;
                resolve(result);
            } catch (err) {
                this.loggingService.log('Error markdowning ' + name, err);
                result = new WikiFile(storedFile.contents.toString(),
                    '<strong>File could not be parsed</strong><pre>' + storedFile.contents + '</pre>');
                resolve(result);
            }
        });
    }

    private wikiword(markdown: string): string {
        if (this.settings.getUseCurly() !== this.usedCurly) {
            this.initializeRegex();
        }
        return markdown.replace(this.wikiWordsRegex, this.replacer);
    }

    private replacer(): string {
        const marker = arguments[1];
        if (marker) {
            // prevent any wikiwordrecognizing: return everything after the marker
            return (arguments[0] as string).substring(1);
        }
        const wikiwordByCase = arguments[2] as string;
        const wikiwordByCurly = arguments[3] as string;
        const wikiwordByDoubleSquare = arguments[4] as string;
        const wikiword = wikiwordByCase || wikiwordByCurly || wikiwordByDoubleSquare;

        const wikiLink = MarkdownerService.createWikiLink(wikiword);

        return wikiLink;
    }

}
