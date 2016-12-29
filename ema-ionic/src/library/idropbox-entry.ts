export interface IDropboxEntry {
    isFile: boolean;
    name: string;
    deleted: boolean;
    rev: string;
    id: string;
    checksum: string;
}