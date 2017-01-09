import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { Settings } from '../../library/settings';
import { WikiPageService } from '../../library/wiki-page-service';
import { TagIndexService } from '../../library/tag-index.service';
import { Component } from '@angular/core';
import { NavController, NavParams } from 'ionic-angular';

@Component({
  selector: 'page-tag',
  templateUrl: 'tag.html'
})
export class TagPage {

  tag: string;
  pageNames: string[];
  styleGrey: boolean;
  private fontPctStyle: SafeStyle;

  constructor(
    navParams: NavParams,
    tagIndexService: TagIndexService,
    settings: Settings,
    sanitizer: DomSanitizer,
    private navCtrl: NavController,
    private wikiPageService: WikiPageService) {

    this.tag = navParams.get("tag");
    tagIndexService.getFilesForTag(this.tag)
      .then(fileNames => this.pageNames = fileNames.map(x => wikiPageService.getPageNameFromFile(x)));

    this.styleGrey = settings.getStyle() === "Grey";
    this.fontPctStyle = sanitizer.bypassSecurityTrustStyle("font-size: " + settings.getFontSize() + "%");
  }

  openPage(pageName: string) {
    this.wikiPageService.requestedPageName = pageName;
    this.navCtrl.pop();
  }

}
