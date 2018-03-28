import { DatatypeService } from './datatype.service';

describe('DatatypeService without the TestBed', () => {

    let service: DatatypeService;
    beforeEach(() => { service = new DatatypeService(); });

    it('#isEmpty should return true when empty', () => {
        let array = [];
        expect(service.isEmpty(array)).toBeTruthy();
    });

    it('#isEmpty should return false when array not empty', () => {
        let array = [1, 2, 3];
        expect(service.isEmpty(array)).toBeFalsy();
    });

    it('#isSet should return true when set', () => {
        expect(service.isSet('a string')).toBeTruthy();
    });

    it('#isSet should return false when undefined', () => {
        let g;
        expect(service.isSet(g)).toBeFalsy();
    });

    describe('#isArray', () => {
        it('#isArray should return true', () => {
            let array = [1, 2, 3];
            expect(service.isArray(array)).toBeTruthy();
        });

        it('#isArray returns true for empty array', () => {
            let array = [];
            expect(service.isArray(array)).toBeTruthy();
        });

        it('#isArray returns false on string', () => {
            expect(service.isArray('string')).toBeFalsy();
        });

        it('#isArray returns false on number', () => {
            expect(service.isArray(1)).toBeFalsy();
        });

        it('#isArray returns false on bool', () => {
            expect(service.isArray(false)).toBeFalsy();
        });
    });

    describe('#isInt', () => {
        it('#isInt', () => {
            expect(service.isInt('42')).toBeTruthy();
        });

        it('#isInt returns true on string zero', () => {
            expect(service.isInt('0')).toBeTruthy();
        });

        it('#isInt returns true on string number', () => {
            expect(service.isInt('2')).toBeTruthy();
        });

        it('#isInt returns true when spaces in number', () => {
            expect(service.isInt('2 000')).toBeTruthy();
        });

        it('#isInt returns false when undefined', () => {
            let g;
            expect(service.isInt(g)).toBeFalsy();
        });

        it('#isInt returns false when string', () => {
            expect(service.isInt('string')).toBeFalsy();
        });

        it('#isInt returns false when float', () => {
            expect(service.isInt('3.14')).toBeFalsy();
        });
    });

    describe('#isFloat', () => {
        it('#isFloat', () => {
            expect(service.isFloat('1.86')).toBeTruthy();
        });

        it('#isFloat returns true on string float', () => {
            expect(service.isFloat('13.98')).toBeTruthy();
        });

        it('#isFloat returns false on int', () => {
            expect(service.isFloat('10')).toBeFalsy();
        });

        it('#isFloat returns false on string', () => {
            expect(service.isFloat('string')).toBeFalsy();
        });

        it('#isFloat returns false on undefined', () => {
            let g;
            expect(service.isFloat(g)).toBeFalsy();
        });

        it('#isFloat returns false on bool', () => {
            expect(service.isFloat('false')).toBeFalsy();
        });

        it('#isFloat returns false on array', () => {
            expect(service.isFloat('[1, 2, 3]')).toBeFalsy();
        });

    });
    describe('#isString', () => {
        it('#isString', () => {
            expect(service.isString('string')).toBeTruthy();
        });

        it('#isString returns false on empty string', () => {
            expect(service.isString('')).toBeFalsy();
        });

        it('#isString returns false on undefined', () => {
            let u;
            expect(service.isString(u)).toBeFalsy();
        });

        it('#isString returns false on int', () => {
            let u;
            expect(service.isString('42')).toBeFalsy();
        });

        it('#isString returns false on float', () => {
            let u;
            expect(service.isString('2.89')).toBeFalsy();
        });

        it('#isString returns false on uri', () => {
            let u;
            expect(service.isString('http://www.google.com')).toBeFalsy();
        });
    });
    describe('#isUri', () => {
        it('#isUri', () => {
            expect(service.isUri('http://networkedplanet.com')).toBeTruthy();
        });

        it('#isUri returns true with querystring', () => {
            expect(service.isUri('http://networkedplanet.com?q=something&another=this')).toBeTruthy();
        });

        it('#isUri returns true with https', () => {
            expect(service.isUri('https://networkedplanet.com')).toBeTruthy();
        });

        it('#isUri returns true with ftp', () => {
            expect(service.isUri('ftp://networkedplanet.com')).toBeTruthy();
        });

        it('#isUri returns false with mailto', () => {
            expect(service.isUri('mailto:someone@networkedplanet.com')).toBeFalsy();
        });

        it('#isUri returns false without http', () => {
            expect(service.isUri('networkedplanet.com')).toBeFalsy();
        });

        it('#isUri returns false on string', () => {
            expect(service.isUri('bob')).toBeFalsy();
        });

        it('#isUri returns false on int', () => {
            expect(service.isUri('1')).toBeFalsy();
        });

        it('#isUri returns false on float', () => {
            expect(service.isUri('1.5')).toBeFalsy();
        });

        it('#isUri returns false on bool', () => {
            expect(service.isUri('true')).toBeFalsy();
        });
    });
    describe('#isBoolTrueFalse', () => {
        it('#isBool', () => {
            expect(service.isBoolTrueFalse('true')).toBeTruthy();
        });

        it('#isBool', () => {
            expect(service.isBoolTrueFalse('false')).toBeTruthy();
        });

        it('#isBool returns false on int', () => {
            expect(service.isBoolTrueFalse('2')).toBeFalsy();
        });

        it('#isBool returns false on string', () => {
            expect(service.isBoolTrueFalse('string')).toBeFalsy();
        });

        it('#isBool returns true on string bool', () => {
            expect(service.isBoolTrueFalse('true')).toBeTruthy();
        });
    });
    describe('#isBoolOneZero', () => {
        it('#isBoolOneZero returns true on 0', () => {
            expect(service.isBoolOneZero('0')).toBeTruthy();
        });

        it('#isBoolOneZero returns true on 1', () => {
            expect(service.isBoolOneZero('1')).toBeTruthy();
        });

        it('#isBoolOneZero returns false on int', () => {
            expect(service.isBoolOneZero('2')).toBeFalsy();
        });

        it('#isBoolOneZero returns false on string', () => {
            expect(service.isBoolOneZero('string')).toBeFalsy();
        });

        it('#isBoolOneZero returns true on string bool', () => {
            expect(service.isBoolTrueFalse('true')).toBeTruthy();
        });
    });
    describe('#isDate', () => {
        // the list of accepted date formats are defined in shared/date-formats.ts
        it('#isDate - 31/12/2017 - to be true', () => {
            expect(service.isDate('31/12/2017')).toBeTruthy();
        });

        it('#isDate - 31/12/17 - to be true', () => {
            expect(service.isDate('31/12/17')).toBeTruthy();
        });

        it('#isDate - 1/2/17 - to be true', () => {
            expect(service.isDate('1/2/17')).toBeTruthy();
        });

        it('#isDate - 1/2/2017 - to be true', () => {
            expect(service.isDate('1/2/2017')).toBeTruthy();
        });

        it('#isDate - 30/11/2899 - to be true', () => {
            expect(service.isDate('1/2/2899')).toBeTruthy();
        });

        it('#isDate - 12/31/2017 - to be true', () => {
            expect(service.isDate('12/31/2017')).toBeTruthy();
        });

        it('#isDate - 12/31/17 - to be true', () => {
            expect(service.isDate('12/31/17')).toBeTruthy();
        });

        it('#isDate - 2017-11-30 - to be true', () => {
            expect(service.isDate('2017-11-30')).toBeTruthy();
        });

        it('#isDate - 1st Jan 2017 - to be true', () => {
            expect(service.isDate('1st Jan 2017')).toBeTruthy();
        });

        it('#isDate - 31st December 2017 - to be true', () => {
            expect(service.isDate('31st December 2017')).toBeTruthy();
        });

        it('#isDate - 31st December 1066 - to be true', () => {
            expect(service.isDate('31st December 1066')).toBeTruthy();
        });

        it('#isDate - 1st Jan 17 - to be true', () => {
            expect(service.isDate('1st Jan 17')).toBeTruthy();
        });

        it('#isDate - invalid date 31/02/2017 - to be false', () => {
            expect(service.isDate('31/02/2017')).toBeFalsy();
        });

        it('#isDate - invalid date 02/31/2017 - to be false', () => {
            expect(service.isDate('02/31/2017')).toBeFalsy();
        });

        it('#isDate - invalid date 31st Feb 17 - to be false', () => {
            expect(service.isDate('31st Feb 17')).toBeFalsy();
        });

        it('#isDate - invalid date 1st Jan 999 - to be false', () => {
            expect(service.isDate('1st jan 999')).toBeFalsy();
        });

        it('#isDate - invalid date 31/02/20173 - to be false', () => {
            expect(service.isDate('31/02/20173')).toBeFalsy();
        });
    });
    describe('#detectDataType', () => {
        it('#detectDataType - strings', () => {
            let array = ['one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('string');
        });

        it('#detectDataType - ints', () => {
            let array = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('integer');
        });

        it('#detectDataType - not enough ints - should return string', () => {
            let array = ['1', '2', '3', '4', '5', '6', '7', '8', '9', 'ten'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('string');
        });

        it('#detectDataType - almost ints should return decimal', () => {
            let array = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10.23'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('decimal');
        });

        it('#detectDataType - floats', () => {
            let array = ['1.34', '2.45', '3.93', '4.73', '5.93', '6.08', '7.28', '8.09', '9.12', '10.0'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('decimal');
        });

        it('#detectDataType - bools', () => {
            let array = ['true', 'false', 'false', 'false', 'true', 'true', 'true', 'false', 'true', 'false'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('boolean');
        });

        it('#detectDataType - bools - case insensitive', () => {
            let array = ['true', 'FALSE', 'false', 'FALSE', 'true', 'TRUE', 'true', 'false', 'true', 'false'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('boolean');
        });

        it('#detectDataType - bools - 1/0', () => {
            let array = ['1', '0', '0', '0', '1', '1', '1', '0', '1', '0'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('boolean');
        });

        it('#detectDataType - almost bools, really ints - 1/0', () => {
            let array = ['1', '0', '0', '0', '1', '1', '1', '0', '1', '2'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('integer');
        });

        it('#detectDataType - links', () => {
            let array = ['http://google.com', 'https://networkedplanet.com', 'http://reddit.com',
                'http://twitter.com', 'https://facebook.com', 'https://thing.org.uk?q=something',
                'https://here.we.are', 'ftp://linky.it', 'https://netflix.co.uk', 'http://go.com'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('uri');
        });

        it('#detectDataType - not enough links', () => {
            let array = ['http://google.com', 'https://networkedplanet.com', 'http://reddit.com',
                'http://twitter.com', 'https://facebook.com', 'https://thing.org.uk?q=something',
                'https://here.we.are', 'linky.it', 'netflix.co.uk', 'go.com'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('string');
        });

        it('#detectDataType - dates', () => {
            let array = ['31/12/2017', '12/31/2017', '1/1/17', '1st Jan 2017', '5/5/1982', '08/08/1979',
                '30/11/82', '1971-07-17', '1066-12-02', '23rd March 1969'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('date');
        });

        it('#detectDataType - almost dates, should be datatime', () => {
            let array = ['31/12/2017', '12/31/2017', '1/1/17', '1st Jan 2017', '5/5/1982', '08/08/1979',
                '30/11/82', '1971-07-17', '1066-12-02', '2013-10-10T18:00:00Z'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('dateTime');
        });

        it('#detectDataType - datetimes', () => {
            let array = ['2002-10-10T12:00:00-05:00', '2002-10-10T17:00:00Z', '2002-10-10T17:00:00.123Z', '-0002-02-12T23:59:15',
                '2017-05-26T12:00:00-05:00', '2016-10-10T00:00:00Z', '2004-10-10T17:00:00.123Z', '-0536-02-12T00:59:15',
                '2012-01-31T12:00:00+07:00', '2013-10-10T18:00:00Z'];
            let response = service.detectDataType(array, 'test column');
            expect(response.datatype).toBe('dateTime');
        });

    });
});
