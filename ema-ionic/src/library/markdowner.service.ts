import { WikiFile } from './wiki-file';
import { StoredFile } from './stored-file';
import { TagIndexService } from './tag-index.service';
import { Settings } from './settings';
import { Queue } from './queue';
import { LoggingService } from './logging-service';
import { Injectable } from '@angular/core';
declare function require(name: string);

@Injectable()
export class MarkdownerService {
    marked = require("marked");
    xRegEx = require("xregexp");

    private wikiWordsRegex;
    private htmlTagsRegex = this.xRegEx("\<a\s.+?\<\/a\>|\<[^\>]+\>", "g");
    private usedCurly: boolean;

    constructor(
        private loggingService: LoggingService,
        private tagIndexService: TagIndexService,
        private settings: Settings) {

        this.initializeRegex();
    }

    private initializeRegex() {
        this.wikiWordsRegex = this.xRegEx(
            "(~)?" +         //remember the previous character if it is the ignore marker
            "(?:" +          //#start the 'or' group 
            "(?<wikiword1>" +  //#bare wikiword group
            "\\p{Lu}" +        //#start with uppercase letter
            "\\p{Ll}+" +       //#one or more lowercase letters 
            "\\p{Lu}" +        //#one uppercase letter 
            "\\w*)" +          //#and zero or more arbitrary characters in the same word
            (this.settings.getUseCurly() ? //START legacy option curly brackets 
                "|" +              //#or
                "\\{" +           //#start with a curly bracket
                "(?<wikiword2>" + // #curly bracket wikiword group
                "[^\{\\}]+" +     //#anything inbetween that is not curly bracket
                ")\\}"            //#end with curly br
                : "") +               //END legacy option curly brackets
            "|" +              //#or
            "\\[\\[" +         //#double square bracket
            "(?<wikiword3>" + // #square bracket wikiword group
            "[^\\]]+" +        // #anything inbetween not being a (single) square bracket
            ")\\]\\]" +         //#end with double square bracket
            ")"        //#close the 'or' group 
            , "gx"); //match multiple times 

        this.usedCurly = this.settings.getUseCurly();
    }

    process(storedFile: StoredFile): Promise<WikiFile> {
        const emaPlaceholder = "<ema-placeholder>";

        return new Promise((resolve, reject) => {
            try {
                if (!storedFile.contents) {
                    const result = new WikiFile("", "");
                    result.tags = [];
                    resolve(result);
                    return;
                }

                //first remove the tags from the content
                var tagsFromContent = this.tagIndexService.separateTagsFromContent(storedFile.contents.toString());
                var tags = tagsFromContent.tags;

                //do markdown
                var markedDown = this.marked(tagsFromContent.strippedContent);

                //hide html from the resulting text, because the HTML is highly likely
                //to contain text that will be recognized as wikiwords (links for example)
                var queue = new Queue<string>();
                var protectedMarkedDown = markedDown.replace(this.htmlTagsRegex, match => {
                    queue.enqueue(match);
                    return emaPlaceholder;
                });

                var wikiworded = this.wikiword(protectedMarkedDown);

                //restore HTML tags
                var processed = wikiworded.replace(this.htmlTagsRegex, match => {
                    if (match === emaPlaceholder) {
                        return queue.dequeue();
                    }
                    return match; //new wikiworded link
                });

                var result = new WikiFile(storedFile.contents.toString(), processed);
                result.tags = tags;
                resolve(result);
            } catch (err) {
                this.loggingService.log("Error markdowning " + name, err);
                var result = new WikiFile(storedFile.contents.toString(), "<strong>File could not be parsed</strong><pre>" + storedFile.contents + "</pre>");
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
        var marker = arguments[1];
        if (marker) {
            //prevent any wikiwordrecognizing: return everything after the marker
            return (<string>arguments[0]).substring(1);
        }
        var wikiwordByCase = <string>arguments[2];
        var wikiwordByCurly = <string>arguments[3];
        var wikiwordByDoubleSquare = <string>arguments[4];
        var wikiword = wikiwordByCase || wikiwordByCurly || wikiwordByDoubleSquare;

        var wikiLink = MarkdownerService.createWikiLink(wikiword);

        return wikiLink;
    }

    static createWikiLink(wikiword: string) {
        return `<a href="ema:${encodeURI(wikiword)}">${wikiword}</a>`
    }

}