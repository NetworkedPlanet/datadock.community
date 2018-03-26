import { Validator } from './validator';
import { Tooltip } from './tooltip';
import { CustomValidator } from './custom-validator';

export class FormField {
    name: string;
    label: string;
    showLabel: boolean;
    defaultValue: string;
    validations: Validator[];
    customValidations: CustomValidator[];
    hidden: boolean;
    collectionName: string;
    tooltip: Tooltip;
    info: string;
    allowReset: boolean;
    disabled: boolean;

    constructor(options: {
        name?: string,
        label?: string,
        showLabel?: boolean,
        hidden?: boolean,
        collectionName?: string,
        defaultValue?: string,
        validations?: Validator[],
        customValidations?: CustomValidator[],
        tooltip?: Tooltip,
        info?: string,
        allowReset?: boolean,
        disabled?: boolean
    }) {
        if (!options.name) {throw new Error('`Name` is required for FormField object'); };
        this.name = options.name;
        this.label = options.label || '';
        this.showLabel = options.showLabel;
        this.defaultValue = options.defaultValue || '';
        this.validations = options.validations || [];
        this.customValidations = options.customValidations || [];
        this.tooltip = options.tooltip || null;
        this.hidden = options.hidden || false;
        this.collectionName = options.collectionName || '';
        this.info = options.info || '';
        this.allowReset = options.allowReset;
        this.disabled = options.disabled;
    }
}

export class TextFormField extends FormField {
    type = 'text';
    placeholder: string;

    constructor(options: {} = {}) {
        super(options);
        this.placeholder = options['placeholder'] || '';
    }
}

export class TagsFormField extends FormField {
    type = 'tags';
    placeholder: string;

    constructor(options: {} = {}) {
        super(options);
        this.placeholder = options['placeholder'] || '';
    }
}

export class TextAreaFormField extends FormField {
    type = 'textarea';
    placeholder: string;

    constructor(options: {} = {}) {
        super(options);
        this.placeholder = options['placeholder'] || '';
    }
}

export class HiddenFormField extends FormField {
    type = 'hidden';
     constructor(options: {} = {}) {
         super(options);
     }
}

export class SelectFormField extends FormField {
    type = 'select';
    options: string[];

    constructor(options: {} = {}) {
        super(options);
        this.options = options['options'] || [];
    }
}

export class NumberFormField extends FormField {
    type = 'number';
    placeholder: string;

    constructor(options: {} = {}) {
        super(options);
        this.placeholder = options['placeholder'] || '';
    }
}

export class RadioFormField extends FormField {
    type = 'radio';
    options: string[];

    constructor(options: {} = {}) {
        super(options);
        this.options = options['options'] || [];
    }
}

export class CheckboxFormField extends FormField {
    type = 'checkbox';
    checked: boolean;

    constructor(options: {} = {}) {
        super(options);
        this.checked = options['checked'] || false;
    }
}
