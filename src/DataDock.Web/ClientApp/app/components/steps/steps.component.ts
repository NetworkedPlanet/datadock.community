import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';

@Component({
    selector: 'dd-steps',
    templateUrl: './steps.component.html',
})
export class StepsComponent implements OnChanges {

    @Input() isUploading: boolean;

    @Input() activeStep: string;

    @Input() restartLink: string;

}
