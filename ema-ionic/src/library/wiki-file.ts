export class WikiFile {
    parsed: string;
    isSearchResults: boolean;
    constructor(public pageName: string, public contents: string) {
    }
}