import { NavController } from '@ionic/angular';
import { Component } from '@angular/core';
import { Settings } from 'src/app/library/settings';

@Component({
    selector: 'page-help',
    templateUrl: 'help.html'
})
export class HelpPage {

    styleGrey: boolean;

    constructor(private navController: NavController, private settings: Settings) {
        this.styleGrey = settings.getStyle() === 'Blue';
    }

    close() {
        this.navController.pop();
    }
}
