import { Injectable } from '@angular/core';
import * as moment from 'moment/moment';
import { DATE_FORMATS } from './date-formats';
import { DATETIME_FORMATS } from './datetime-formats';

/*
Helper service - pass in an array of strings and the service will determine
if it think it's a string / uri / integers / decimals / dates / datetimes / boolean
 */
@Injectable()
export class DatatypeService {

    // based on sending 10 items through
    detectDataType(input: string[], columnName: string): any {
        // console.log('detect datatype', input, columnName);
        let sniffed = '';
        let message = '';
        if (input && input.length > 0) {

            let sizeOfArray = input.length;
            const bar = this.getBar(sizeOfArray);

            let blanks = this.countBlanks(input);
            /*
             WEIGHTING
             ---------
             string = 1
             decimal = 2
             boolean = 3  // if 1/0 only return boolean if all results are boolean
             integer = 4
             datetime = 5
             date = 6
             uri = 7
             */
            let sniffer = [];
            let snString = ['string', 1],
                snDecimal = ['decimal', 2],
                snBoolean_10 = ['boolean_10', 3],
                snInteger = ['integer', 4],
                snBoolean = ['boolean', 5],
                snDatetime = ['dateTime', 10],
                snDate = ['date', 11],
                snUri = ['uri', 15],
                snBlank = ['blank', 100];

            for (let i = 0; i < input.length; i++) {
                let stringValue = input[i];
                if (stringValue === '') {
                    sniffer.push(snBlank);
                } else {
                    if (this.isBoolTrueFalse(stringValue)) {
                        sniffer.push(snBoolean);
                    }
                    if (this.isBoolOneZero(stringValue)) {
                        // TODO this might be just an int - test
                        sniffer.push(snBoolean_10);
                    }
                    if (this.isFloat(stringValue)) {
                        sniffer.push(snDecimal);
                    }
                    if (this.isInt(stringValue)) {
                        sniffer.push(snInteger);
                    }
                    if (this.isUri(stringValue)) {
                        sniffer.push(snUri);
                    }
                    if (this.isDate(stringValue)) {
                        sniffer.push(snDate);
                    }
                    if (this.isDateTime(stringValue)) {
                        sniffer.push(snDatetime);
                    }
                    if (this.isString(stringValue)) {
                        sniffer.push(snString);
                    }
                }
            }
            let snifferSorted = sniffer.sort(function (a, b) {
                return a[1] - b[1];
            });
            sniffed = snifferSorted[0][0];
            // console.log(snifferSorted);
            if (sniffed === 'boolean_10') {
                // check how many found as this will also match on int
                let boolsFound = this.countHits(sniffer, 'boolean_10');
                if (boolsFound < (input.length - blanks)) {
                    // go to next non boolean sniffed result
                    sniffed = this.nextSniffedDatatype(sniffer, 'boolean_10');
                } else {
                    sniffed = 'boolean';
                }
            }
        }
        if (sniffed === '' && message === '') {
            sniffed = 'string';
            message = 'Unable to detect datatype for column ' + columnName;
        }

        if (sniffed === 'blank') {
            sniffed = 'string';
            message = `Unable to detect datatype for column ${columnName} as there are too many blank cells.`;
        }

        if (sniffed === 'boolean_10') {
            // needs to match option value
            sniffed = 'boolean';
        }
        let response = {datatype: sniffed, message: message};
        return response;
    }

    countBlanks(input: string[]) {
        let count = 0;
        for (let i = 0; i < input.length ; i++) {
            let item = input[i];
            if (item === '') {
                count++;
            }
        }
        return count;
    }

    countHits(sniffer: any[], checkFor: string) {
        let count = 0;
        for (let i = 0; i < sniffer.length ; i++) {
            let sniffed = sniffer[i][0];
            if (sniffed === checkFor) {
                count++;
            }
        }
        return count;
    }

    nextSniffedDatatype(sniffer: any[], moveFrom: string) {
        for (let i = 0; i < sniffer.length ; i++) {
            let sniffed = sniffer[i][0];
            if (sniffed !== moveFrom) {
                return sniffed;
            }
        }
    }

    isSet(val) {
        return (val !== undefined) && (val !== null);
    }

    isEmpty(obj) {
        if (obj.length && obj.length > 0) {
            return false;
        }

        for (let key in obj) {
            if (this.hasOwnProperty.call(obj, key)) {
                return false;
            }
        }
        return true;
    }



    isArray(val) {
        if (val instanceof Array) {
            return 'array';
        } else {
            return false;
        }
    }

    isInt(val: string) {
        if (val) {
            let valNoWhiteSpace = val.replace(/ /g, '');
            let nan = isNaN(Number(valNoWhiteSpace));
            let blank = valNoWhiteSpace === '';

            // let intRegex = /^[-+]?\d+$/;
            if (!nan && !blank) {
                if (Number.isInteger(Number(valNoWhiteSpace))) {
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    isFloat(val: string, separator = '.') {
        if (val) {
            let valNoWhiteSpace = val.replace(/ /g, '');
            let nan = isNaN(Number(valNoWhiteSpace));
            let blank = valNoWhiteSpace === '';
            if (!nan && !blank) {
                if (Number.isInteger(Number(valNoWhiteSpace))) {
                    // but does it have the separator in the original string? (10000.00 is returning an int
                    if (valNoWhiteSpace.indexOf(separator) > 0) {
                        return true;
                    }
                    return false;
                } else {
                    return true;
                }
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    isString(val: string) {
        if (val && !this.isInt(val) && !this.isFloat(val) && !this.isUri(val)
            && !this.isBoolTrueFalse(val) && !this.isDate(val) && !this.isDateTime(val) && !this.isArray(val)) {
            return true;
        } else {
            return false;
        }
    }

    isUri(val: string) {
        let uriRegex = /^(https?)|(ftp):\/\/.+$/;
        if (uriRegex.test(val)) {
            return true;
        } else {
            return false;
        }
    }

    isDate(val: string) {
        if (moment(val, DATE_FORMATS, true).isValid()) {
            return true;
        } else {
            return false;
        }
    }

    isDateTime(val: string) {
        if (moment(val, DATETIME_FORMATS, true).isValid()) {
            return true;
        } else {
            return false;
        }
    }

    isBoolTrueFalse(val: string) {
        let valNoWhiteSpace = val.replace(/ /g, '');
        let trueFalseRegex = /^(true|false)$/i;
        if (trueFalseRegex.test(valNoWhiteSpace)) {
            return true;
        } else {
            return false;
        }
    }

    isBoolOneZero(val: string) {
        let valNoWhiteSpace = val.replace(/ /g, '');
        let oneZeroRegex = /^(1|0)$/;
        if (oneZeroRegex.test(valNoWhiteSpace)) {
            return true;
        } else {
            return false;
        }
    }

    private getBar(sizeOfArray: number) {
        if (sizeOfArray < 10 ) {
            return sizeOfArray;
        } else {
            if (sizeOfArray === 10) {
                return 8;
            } else {
                return sizeOfArray - 2;
            }
        }
    }

    detectBool(vals: any[]) {
        return false;
    }

    __getClass = function(val) {
        return Object.prototype.toString.call(val)
            .match(/^\[object\s(.*)\]$/)[1];
    };

    getType(val): string {
        if (val instanceof Array) {
            return 'array';
        }
        let isSet = this.isSet(val);
        if (!isSet) {
            return 'not set';
        }
        if (typeof val !== 'string') {
            val = val.toString();
        }
        let nan = isNaN(Number(val));
        let blank = val === '';
        let boolRegex = /^(true|false)|(1|0)$/;

        if (boolRegex.test(val)) {
            return 'boolean';
        } else {
            if (!nan && !blank) {
                if (Number.isInteger(Number(val))) {
                    return 'integer';
                } else {
                    return 'float';
                }
            } else {
                if (moment(val, DATE_FORMATS, true).isValid()) {
                    return 'date';
                } else {
                    if (moment(val, DATETIME_FORMATS, true).isValid()) {
                        return 'datetime';
                    } else {
                        /*
                        if (this.simpleUriChecker(val)) {
                            return 'uri';
                        } else {
                            return 'string';
                        }
                         */
                    }
                }
            }
        }
    }
}
