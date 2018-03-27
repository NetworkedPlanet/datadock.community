import { Component, OnInit, ElementRef } from '@angular/core';
import { AppService } from '../../shared/app.service';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit  {

    error: string;

    constructor(private elementRef: ElementRef, private appService: AppService){
        this.error = '';
    }

    ngOnInit(): void {
        this.printEnvironmentVariables();
            // v0.1 set a dash view model and target repo simply to recreate the dashboard menu, this is unnecessary in v1.0 community edition
    }

    printEnvironmentVariables() {

        // set in webpack config

        if (IN_DEBUG) {

            // let env = JSON.stringify(process.env);
            // console.log(`process.env: ${env}`);

            console.log(`IN_DEBUG: ${IN_DEBUG}`);
            console.log(`VERSION: ${VERSION}`);
            console.log(`API_URL: ${API_URL}`);
        }
    }
}
