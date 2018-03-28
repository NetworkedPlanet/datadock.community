import { FormGroup, FormBuilder, FormControl } from '@angular/forms';
import { ViewModelSection, MetadataViewModel } from '../../shared';

export class MockFormManager {


    public warnings: Array<string> = [];

    public metadataViewModel: MetadataViewModel;
    public mainForm: FormGroup;


    constructor() {
        console.warn('MockFormManager');
        this.warnings = [];
    }


    buildForm(): void {
        // do nothing
    }

    sendData() {
        // do nothing
    }

    exportMetadata(): any {
        return {};
    }


    getViewModelTableHeaders(sectionName: string): string[] {
        return [];
    }

    getSection(sectionName: string): ViewModelSection {
        let vms = new ViewModelSection();
        vms.name = 'Test Section';
        return vms;
    }

    getField(name: string): Array<any> {
        let searchResults = [];
        return searchResults;
    }

    getColumnsList(section: string): Array<any> {
        let list = [];
        return list;
    }

    getCollectionControls(sectionName: string, columnName: string): Array<any> {
        let controls = [];
        return controls;
    }

    getControls(sectionName: string): Array<any> {
        let controls = [];
        return controls;
    }
}
