import { Storage } from '@ionic/storage';
import { WikiStorage } from './wiki-storage';
import { Injectable } from '@angular/core';

@Injectable()
export class TagIndexService {
    private tagsRegex = /^[Tt]ags\:(.+)$/m;
    private storageTagFilesPrefix = "tagFiles.";
    private storageFileTagsPrefix = "fileTags.";

    constructor(
        private wikiStorage: WikiStorage,
        private storage: Storage
    ) {

    }

    separateTagsFromContent(content: string) {
        content = content || "";
        var m = this.tagsRegex.exec(content);
        var tags = [];
        if (m) {
            var allTags = m[1];
            tags = allTags.split(/[;,\s\|]/g).map(x => x.trim()).filter(x => !!x);
        }

        return {
            strippedContent: content.replace(this.tagsRegex, ""),
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
        var separatedTags = this.separateTagsFromContent(content);
        return this.getTagsForFile(fileName).then(prevTags => {
            var deletedTags = prevTags.filter(x => separatedTags.tags.indexOf(x) === -1);
            var addedTags = separatedTags.tags.filter(x => prevTags.indexOf(x) === -1);

            var delPromises = deletedTags.map(tag => this.getFilesForTag(tag)
                .then(files => this.storage.set(this.storageTagFilesPrefix + tag, files.filter(f => f !== fileName))));
            var addPromises = addedTags.map(tag => this.getFilesForTag(tag)
                .then(files => this.storage.set(this.storageTagFilesPrefix + tag, files.concat([fileName]))));

            var filePromise: Promise<any>;
            if (separatedTags.tags.length === 0) {
                filePromise = this.storage.remove(this.storageFileTagsPrefix + fileName);
            } else {
                filePromise = this.storage.set(this.storageFileTagsPrefix + fileName, separatedTags.tags);
            }

            return Promise.all(addPromises.concat(delPromises).concat([filePromise]));
        });
    }

    buildInitialIndex(): Promise<any> {
        return this.storage.get("tagIndexBuilt")
            .then(value => value ? Promise.resolve() : this.buildIndex());
    }

    buildIndex(): Promise<any> {
        return this.wikiStorage.listFiles()
            .then(fileNames => {
                fileNames = fileNames.filter(x => x.endsWith(".txt"));
                var promises = fileNames.map(name => this.wikiStorage.getTextFileContents(name)
                    .then(storedFile => {
                        var tagsFromContent = this.separateTagsFromContent(storedFile.contents.toString());
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
                    var uniqueTags = fileTags
                        .map(x => x.tags)
                        .reduce((a, b) => a.concat(b)) //"selectMany"
                        .filter((tag, index, array) => array.indexOf(tag) === index); //distinct

                    var tagFiles = uniqueTags.map(tag => {
                        return {
                            tag,
                            fileNames: fileTags.filter(x => x.tags.indexOf(tag) > -1).map(x => x.fileName)
                        };
                    });

                    var setTfPromises = tagFiles.map(x => this.storage.set(this.storageTagFilesPrefix + x.tag, x.fileNames));
                    var setFtPromises = fileTags.map(x => this.storage.set(this.storageFileTagsPrefix + x.fileName, x.tags));

                    this.storage.set("tagIndexBuilt", true);

                    return Promise.all(setTfPromises.concat(setFtPromises));
                } 
            });
    }


}