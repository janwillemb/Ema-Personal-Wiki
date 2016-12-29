import { LoggingService } from './logging-service';
import { Injectable } from '@angular/core';
declare function require(name: string);

@Injectable()
export class MarkdownerService {
    marked = require("marked");
    xRegEx = require("xregexp");

    private wikiWordsRegex = this.xRegEx(
            "(~)?" +         //remember the previous character if it is the ignore marker
            "(?:" +          //#start the 'or' group 
                "(?<wikiword1>" +  //#bare wikiword group
                "\\p{Lu}" +        //#start with uppercase letter
                "\\p{Ll}+" +       //#one or more lowercase letters 
                "\\p{Lu}" +        //#one uppercase letter 
                "\\w*)" +          //#and zero or more arbitrary characters in the same word
            "|" +              //#or
                "\\{" +           //#start with a curly bracket
                "(?<wikiword2>" + // #curly bracket wikiword group
                "[^\{\\}]+" +     //#anything inbetween that is not curly bracket
                ")\\}" +           //#end with curly br
            "|" +              //#or
                "\\[\\[" +         //#double square bracket
                "(?<wikiword3>" + // #square bracket wikiword group
                "[^\\]]+" +        // #anything inbetween not being a (single) square bracket
                ")\\]\\]" +         //#end with double square bracket
            ")"        //#close the 'or' group 
    , "g"); //match multiple times 

    constructor(private loggingService: LoggingService) {
    }

    process(name: string, markdown: string): Promise<string> {
        return new Promise((resolve, reject) => {
            try {
                var preprocessed = this.preprocess(markdown);
                var processed = this.marked(preprocessed);
                resolve(processed);
            } catch (err) {
                this.loggingService.log("Error markdowning " + name, err);
                resolve("<strong>File could not be parsed</strong><pre>" + markdown + "</pre>");
            }
        });
    }

    private preprocess(markdown: string): string {
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

        return `<a href="ema:${wikiword}">${wikiword}</a>`;
    }

}