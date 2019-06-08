export class WikiFile {
    isSearchResults: boolean;
    tags: string[];
    pageName: string;
    constructor(public contents: string, public parsed: string) {
    }
}
