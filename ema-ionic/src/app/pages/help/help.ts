import { NavController } from '@ionic/angular';
import { Component } from '@angular/core';

@Component({
    selector: 'page-help',
    templateUrl: 'help.html'
})
export class HelpPage {

    constructor(private navController: NavController) {
    }

    close() {
        this.navController.pop();
    }
}
