import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { AppService } from '../../shared/app.service';

@Component({
    selector: 'dd-steps',
    templateUrl: './steps.component.html',
    styleUrls: ['./steps.component.css']
})
export class StepsComponent implements OnChanges {

    @Input() isUploading: boolean;

    @Input() activeStep: string;

    @Input() restartLink: string;

    constructor(private router: Router, private ihs: AppService) {
        this.isUploading = false;
        this.activeStep = '';
    }

    ngOnChanges(changes: {[propKey: string]: SimpleChange}) {
        // console.log(`isUploading: ${this.isUploading}`);
        // console.log(`activeStep: ${this.activeStep}`);
    }

    restart() {
        if (this.ihs.ownerId && this.ihs.repoId) {
            let restartUrl = `/${this.ihs.ownerId}/${this.ihs.repoId}/import`;
            if (this.ihs.schemaId) {
                restartUrl = restartUrl + '/' + this.ihs.schemaId;
            }
            if (IN_DEBUG) {
                console.log('restart (choose new CSV)', restartUrl);
            }
            this.router.navigate([restartUrl]);
        }
        // if owner/repo are not set then a file has not yet been chosen, so no need to redirect
    }
}
