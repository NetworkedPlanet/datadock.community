import { Globals } from '../globals';
export class CsvFile {
    filename: string;
    uriSafeFilename: string;
    headings: string[];
    size: number;
    lastModified: number;
    lastModifiedDate: string;

    aborted: boolean;
    cursor: number;
    delimiter: string;
    linebreak: string;
    truncated: boolean;

    data: any;
    file: any;

    // columns [{index: 0, name: '', title: ''}]
    public columnSet = [];

    constructor() {}

    /*
    processing done in method rather than constructor to make the class more testable
     */
    initialise(parseResult?: any, file?: any): void {
        if (file) {
            this.file = file;
            this.filename = file.name;
            this.uriSafeFilename = encodeURI(file.name);
            this.size = file.size;
            this.lastModified = file.lastModified;
            this.lastModifiedDate = file.lastModifiedDate;
        }

        if (parseResult) {
            let data = parseResult['data'];
            if (data) {
                this.data = data;
                this.headings = data[0];
            }
            this.populateColumnSetFromHeadings();

            let meta = parseResult['meta'];
            if (meta) {
                this.aborted = meta['aborted'];
                this.cursor = meta['cursor'];
                this.delimiter = meta['delimiter'];
                this.linebreak = meta['linebreak'];
                this.truncated = meta['truncated'];
            }
        }
    }


    private populateColumnSetFromHeadings() {
        // columns (index, name, title)
        if (this.headings) {
            let idx = 0;
            for (let heading of this.headings) {
                let title = heading.trim();
                let name = this.getColumnName(title);
                let colInfo = {index: idx, name: name, title: title};
                this.columnSet.push(colInfo);
                idx++;
            }
        }
    }

    private getColumnName(csvHeading: string) {
        let columnName = csvHeading.trim().replace(/[:/?#[\]@!$&'()*+,;= ]/g, '_').replace('__', '_').toLowerCase();
        return columnName;
    }

    // get top 10 rows of raw data (not including header row)
    public getDataSlice(): Array<any> {
        if (this.data) {
            return this.data.slice(1, 11);
        } else {
            return [];
        }
    }

    getColumnTitle(columnName: string): string {
        let title = '';
        for (let c of this.columnSet) {
            if (c.name === columnName) {
                title = c.title;
            }
        }
        return title;
    }
}
