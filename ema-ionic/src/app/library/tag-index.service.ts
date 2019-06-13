import { Storage } from '@ionic/storage';
import { WikiStorage } from './wiki-storage';
import { Injectable } from '@angular/core';

@Injectable()
export class TagIndexService {
    private tagsRegex = /^[Tt]ags\:(.+)$/m;
    private storageTagFilesPrefix = 'tagFiles.';
    private storageFileTagsPrefix = 'fileTags.';

    constructor(
        private wikiStorage: WikiStorage,
        private storage: Storage
    ) {

    }

    separateTagsFromContent(content: string) {
        content = content || '';
        const m = this.tagsRegex.exec(content);
        let tags = [];
        if (m) {
            const allTags = m[1];
            tags = allTags.split(/[;,\s\|]/g).map(x => x.trim()).filter(x => !!x);
        }

        return {
            strippedContent: content.replace(this.tagsRegex, ''),
            tags
        };
    }

    getFilesForTag(tag: string): Promise<string[]> {
        return this.storage.get(this.storageTagFilesPrefix + tag)
            .then(files => files || []);
    }

    getTagsForFile(fileName: string): Promise<string[]> {
        return this.storage.get(this.storageFileTagsPrefix + fileName)
            .then(tags => tags || []);
    }

    afterSaveFile(fileName: string, content: string): Promise<any> {
        const separatedTags = this.separateTagsFromContent(content);
        return this.getTagsForFile(fileName).then(prevTags => {
            const deletedTags = prevTags.filter(x => separatedTags.tags.indexOf(x) === -1);
            const addedTags = separatedTags.tags.filter(x => prevTags.indexOf(x) === -1);

            const delPromises = deletedTags.map(tag => this.getFilesForTag(tag)
                .then(files => this.storage.set(this.storageTagFilesPrefix + tag, files.filter(f => f !== fileName))));
            const addPromises = addedTags.map(tag => this.getFilesForTag(tag)
                .then(files => this.storage.set(this.storageTagFilesPrefix + tag, files.concat([fileName]))));

            let filePromise: Promise<any>;
            if (separatedTags.tags.length === 0) {
                filePromise = this.storage.remove(this.storageFileTagsPrefix + fileName);
            } else {
                filePromise = this.storage.set(this.storageFileTagsPrefix + fileName, separatedTags.tags);
            }

            return Promise.all(addPromises.concat(delPromises).concat([filePromise]));
        });
    }

    buildInitialIndex(): Promise<any> {
        return this.storage.get('tagIndexBuilt')
            .then(value => value ? Promise.resolve() : this.buildIndex());
    }

    buildIndex(): Promise<any> {
        return this.wikiStorage.listFiles()
            .then(fileNames => {
                fileNames = fileNames.filter(x => x.endsWith('.txt'));
                const promises = fileNames.map(name => this.wikiStorage.getFileContents(name)
                    .then(storedFile => {
                        const tagsFromContent = this.separateTagsFromContent(storedFile.contents.toString());
                        return {
                            fileName: storedFile.fileName,
                            tags: tagsFromContent.tags
                        };
                    })
                );
                return Promise.all(promises);
            })
            .then(fileTags => {
                if (fileTags && fileTags.length) {
                    const uniqueTags = fileTags
                        .map(x => x.tags)
                        .reduce((a, b) => a.concat(b)) // "selectMany"
                        .filter((tag, index, array) => array.indexOf(tag) === index); // distinct

                    const tagFiles = uniqueTags.map(tag => {
                        return {
                            tag,
                            fileNames: fileTags.filter(x => x.tags.indexOf(tag) > -1).map(x => x.fileName)
                        };
                    });

                    const setTfPromises = tagFiles.map(x => this.storage.set(this.storageTagFilesPrefix + x.tag, x.fileNames));
                    const setFtPromises = fileTags.map(x => this.storage.set(this.storageFileTagsPrefix + x.fileName, x.tags));

                    this.storage.set('tagIndexBuilt', true);

                    return Promise.all(setTfPromises.concat(setFtPromises));
                }
            });
    }
}
