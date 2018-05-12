import { Component, Input } from '@angular/core';
import { FormControl } from '@angular/forms';
import { FormManager, ViewModelSection } from '../../shared';


@Component({
    selector: 'dd-advanced',
    templateUrl: 'advanced.component.html',
    styleUrls: ['advanced.component.css']
})
export class AdvancedComponent {

    @Input() sectionName: string;
    section: ViewModelSection;


    constructor(public fm: FormManager) {
        if (this.sectionName !== '') {
            this.section = fm.getSection(this.sectionName);
        }
    }
    getColumnTitle(columnName: string): string {
        return this.fm.getColumnTitle(columnName);
    }
}
