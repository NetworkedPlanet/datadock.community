export class CustomValidator {
    public message: string;
    constructor(message: string) {
        this.message = message;
    }
}

export class NoSpaceValidator extends CustomValidator {
    public type: string = 'cannotContainSpace';

    constructor(public message: string) {
        super(message);
    }
}
