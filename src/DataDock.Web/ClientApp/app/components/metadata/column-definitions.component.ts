import { Component, Input } from '@angular/core';
import { FormManager, ViewModelSection } from '../shared';

@Component({
    selector: 'dd-column-definitions',
    templateUrl: 'column-definitions.component.html',
    styleUrls: ['column-definitions.component.css']
})
export class ColumnDefinitionsComponent {

    @Input() sectionName: string;
    section: ViewModelSection;

    constructor(private fm: FormManager) {
        if (this.sectionName !== '') {
            this.section = fm.getSection(this.sectionName);
        }
    }
}
