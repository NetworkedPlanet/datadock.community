/*
The form view model to edit the metadata of a spreadsheet ready for import

This is split into sections that will display different components for user interaction
 */

import { FormField } from './form-field/form-field';

export class MetadataViewModel {
    sections: ViewModelSection[];

    constructor() {
        this.sections = [];
    }

    getSection(name: string): ViewModelSection {
        if (this.sections) {
            let section = this.sections.filter(item => item.name === name)[0];
            return section;
        } else {
            return null;
        }
    }

}

export class ViewModelSection {

    name: string;
    displayName: string;
    displayType: string;

    fields: FormField[];
    fieldCollections: FieldCollection[];
    tableHeaders: string[];

    constructor() {
        this.fields = [];
        this.fieldCollections = [];
        this.tableHeaders = [];
    }
}

export class FieldCollection {
    index: number;
    name: string;
    fields: FormField[];
    disabled: boolean;
    columnNumber: number;

    constructor() {
        this.fields = [];
    }
}
