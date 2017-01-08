import { Storage } from '@ionic/storage';
import { WikiStorage } from './wiki-storage';
import { Injectable } from '@angular/core';

@Injectable()
export class TagIndexService {
    private tagsRegex = /^[Tt]ags\:(.+)$/m;
    private storageTagFilesPrefix = "tagFiles.";

    constructor(
        private wikiStorage: WikiStorage,
        private storage: Storage
    ) {

    }

    separateTagsFromContent(content: string) {
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

    buildIndex(): Promise<any> {
        return this.wikiStorage.listFiles()
            .then(fileNames => {
                var promises = fileNames.map(name => this.wikiStorage.getFileContents(name)
                    .then(storedFile => {
                        var tagsFromContent = this.separateTagsFromContent(storedFile.contents);
                        return {
                            fileName: storedFile.fileName,
                            tags: tagsFromContent.tags
                        };
                    })
                );
                return Promise.all(promises);
            })
            .then(fileTags => {
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

                return Promise.all(setTfPromises);
            });
    }


}