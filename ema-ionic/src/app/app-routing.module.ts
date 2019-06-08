import { NgModule } from '@angular/core';
import { PreloadAllModules, Routes, RouterModule } from '@angular/router';
import { HelpPage } from './pages/help/help';
import { WikiPage } from './pages/wiki/wiki';
import { TagPage } from './pages/tag/tag';
import { SettingsPage } from './pages/settings/settings';
import { LogsPage } from './pages/logs/logs';

const routes: Routes = [
    { path: '', redirectTo: 'wiki/_default', pathMatch: 'full' },
    { path: 'help', component: HelpPage },
    { path: 'settings', component: SettingsPage },
    { path: 'logs', component: LogsPage },
    { path: 'wiki/:page', component: WikiPage },
    { path: 'tag/:tag', component: TagPage },
];

@NgModule({
    imports: [
        RouterModule.forRoot(routes, { preloadingStrategy: PreloadAllModules })
    ],
    exports: [RouterModule]
})
export class AppRoutingModule { }
