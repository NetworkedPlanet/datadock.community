import { Component, OnInit, ElementRef, Inject } from '@angular/core';
import { AppService } from '../../shared/app.service';
import { ConfigurationService } from '../../shared/services/config.service';
import { Globals } from '../../globals';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit  {

    error: string;

    constructor(public configService: ConfigurationService, @Inject('BASE_URL') private originUrl: string, private globals: Globals){
        this.error = '';
    }

    ngOnInit(): void {
        console.log('initialising app component...');
        this.globals.config = this.configService.config;
        this.globals.apiUrl = this.originUrl + 'api/';
        this.printGlobals();
    }

    printGlobals() {
        if (this.configService.config.inDebug) {
            console.log(`BASE_URL: ${this.originUrl}`);
            console.log(`API_URL: ${this.globals.apiUrl}`);
            console.log(`IN_DEBUG: ${this.globals.config.inDebug}`);
        }
    }
}
