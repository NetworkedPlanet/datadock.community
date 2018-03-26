import { FormControl, ValidationErrors, AbstractControl } from '@angular/forms';

export class CustomValidators {

    static cannotContainSpace(control: AbstractControl): ValidationErrors | null {
        if (control.value.indexOf(' ') >= 0) {
            return {cannotContainSpace : true};
        } else {
            return null;
        }
    }

}
