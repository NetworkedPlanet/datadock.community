import { Component, Input, forwardRef, Inject, OnInit, OnChanges, SimpleChanges, AfterViewInit } from '@angular/core';
import { FormManager } from '../form-manager';
import { FormField } from './form-field';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'dd-form-field',
  templateUrl: 'form-field.component.html',
  styleUrls: ['form-field.component.css']
})
export class FormFieldComponent implements OnInit {

    @Input() field;

    private fm: FormManager;

    constructor(@Inject(forwardRef(() => FormManager)) formManager) {
        this.fm = formManager;
    }

    ngOnInit(): void {
        let field = this.field[0];
        if (field) {
            let collectionName = field['collectionName'];
            let fieldName = field['name'];
            let suppressed = this.fm.isFieldCollectionSuppressed(collectionName);
            if (suppressed) {
                // do not disable if this is the suppress checkbox
                if (!fieldName.endsWith('_suppress')) {
                    let control = this.field[1];
                    if (control) {
                        control.disable();
                    }
                }
            }
        }
    }

    filterSelectOption(option: any) {
        if (option) {
            if (option['display']) {
                return true;
            }
        }
        return false;
    }

    resetField() {
        let f: FormField = this.field[0];
        let c: FormControl = this.field[1];
        c.patchValue(f.defaultValue);
    }

    checkboxChange(e) {
        let f: FormField = this.field[0];
        let c: FormControl = this.field[1];
        let suppress = c.value;
        let collectionName = f.collectionName;
        this.fm.toggleFieldCollection(collectionName, suppress);
    }

}
