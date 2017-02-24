export class StoredFile {
    checksum: string;
    constructor(public fileName: string, public contents: string | ArrayBuffer) {
    }
}