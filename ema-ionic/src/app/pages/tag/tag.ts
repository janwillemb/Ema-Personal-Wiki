import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { Settings } from '../../library/settings';
import { WikiPageService } from '../../library/wiki-page-service';
import { TagIndexService } from '../../library/tag-index.service';
import { Component, OnInit } from '@angular/core';
import { NavController, NavParams } from '@ionic/angular';
import { LoggingService } from 'src/app/library/logging-service';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'page-tag',
    templateUrl: 'tag.html',
    styleUrls: ['tag.scss'],
})
export class TagPage implements OnInit {

    tag: string;
    pageNames: string[];
    fontPctStyle: SafeStyle;

    constructor(
        private navController: NavController,
        private route: ActivatedRoute,
        private tagIndexService: TagIndexService,
        private settings: Settings,
        private sanitizer: DomSanitizer,
        private loggingService: LoggingService,
        private wikiPageService: WikiPageService) {
    }

    async ngOnInit() {
        this.fontPctStyle = this.sanitizer.bypassSecurityTrustStyle('font-size: ' + this.settings.getFontSize() + '%');

        this.tag = this.route.snapshot.paramMap.get('tag');

        try {
            const fileNames = await this.tagIndexService.getFilesForTag(this.tag);
            this.pageNames = fileNames.map(x => this.wikiPageService.getPageNameFromFile(x));
        } catch (error) {
            this.loggingService.log('error opening tag ' + this.tag, error);
        }
    }

    goto(page: string) {
        this.wikiPageService.requestedNextPage = page;
        this.navController.pop();
    }
}
