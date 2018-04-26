import { Injectable } from '@angular/core';
import { Globals } from '../globals';

@Injectable()
export class SchemaHelperService {

    private schemaId: string;
    private schemaJson: any;
    private metadataJson: any;

    public hasSchema: boolean;
    public schemaTitle: string;

    constructor(private globals: Globals ) {
        this.schemaId = '';
        this.schemaTitle = '';
        this.hasSchema = false;
    }

    public setSchema(ownerId: string, schemaId: string, schemaInfo: any) {
        if (this.globals.config.inDebug) {
            console.log('setSchema', ownerId, schemaId, schemaInfo["schema"]);
        }
        if (ownerId && schemaId && schemaInfo) {
            this.schemaId = schemaId;
            this.schemaJson = JSON.parse(schemaInfo["schema"]);
            this.hasSchema = true;
            if (this.schemaJson) {
                this.schemaTitle = this.schemaJson['dc:title'];
                this.metadataJson = this.schemaJson['metadata'];
                console.log('schemaTitle', this.schemaTitle);
                console.log('metadataJson', this.metadataJson);
            }
        } else {
            // one or more missing id
        }

    }

    public getSchema() {
        return this.schemaJson;
    }

    public getMetadata() {
        return this.metadataJson;
    }

    public getMetadataIdentifier(aboutUrlPrefix: string, dft: string) {
        if (this.hasSchema && this.metadataJson) {
            let templateIdentifier = this.metadataJson['aboutUrl'];
            let templatePrefix = this.metadataJson['url'];
            if (templateIdentifier && templatePrefix) {
                let templateAboutUrl = templatePrefix.replace('id/dataset/', 'id/resource/');
                // swap template prefix to current prefix
                let identifier = templateIdentifier.replace(templateAboutUrl, aboutUrlPrefix);
                return identifier;
            }
        }
        return dft;
    }

    public getMetadataIdentifierColumnName() {
        if (this.hasSchema && this.metadataJson) {
            let identifier = this.metadataJson['aboutUrl'];
            if (identifier) {
                try {
                    // get chars between { and }
                    let col = identifier.substring(identifier.lastIndexOf('{') + 1, identifier.lastIndexOf('}'));
                    if (col !== '_row') {
                        // ignore _row as that is not a col
                        return col;
                    }
                } catch (error) {
                    return '';
                }
            }
        }
        // return blank for errors or _row, as it will fall back to _row
        return '';
    }

    public getMetadataTitle(): string {
        if (this.hasSchema && this.metadataJson) {
            let title = this.metadataJson['dc:title'];
            return title;
        }
        return '';
    }

    public getMetadataDescription(): string {
        console.log('getMetadataDescription', this.hasSchema, this.metadataJson);
        if (this.hasSchema && this.metadataJson) {
            let desc = this.metadataJson['dc:description'];
            console.log(desc);
            return desc;
        }
        return '';
    }

    public getMetadataLicenseUri(): string {
        if (this.hasSchema && this.metadataJson) {
            let licenseUri = this.metadataJson['dc:license'];
            return licenseUri;
        }
        return '';
    }

    public getMetadataTags(): string[] {
        if (this.hasSchema && this.metadataJson) {
            let tags = this.metadataJson['dcat:keyword'];
            return tags;
        }
        return [];
    }

    public getMetadataColumnTemplate(columnName: string): any {
        if (this.hasSchema && this.metadataJson) {
            let tableSchema = this.metadataJson['tableSchema'];
            if (tableSchema) {
                let metadataColumns = tableSchema['columns']; // array
                if (metadataColumns) {
                    for (let i = 0; i < metadataColumns.length; i++) {
                        if (metadataColumns[i].name === columnName) {
                            if (this.globals.config.inDebug) {
                                 console.log('template found for column.', metadataColumns[i])
                            }
                            return metadataColumns[i];
                        }
                    }
                }
            }
            if (this.globals.config.inDebug) {
                console.log('Unable to find column in template.', columnName);
            }
        }
        return {};
    }

    public getColumnTitle(template: any, dft: string): string {
        if (template) {
            let titles = template['titles'];
            if (titles) {
                // first item of array
                return titles[0];
            }
        }
        return dft;
    }

    public getColumnPropertyUrl(template: any, dft: string): string {
        if (template) {
            let propUrl = template['propertyUrl'];
            if (propUrl) {
                return propUrl;
            }
        }
        return dft;
    }

    public getColumnDatatype(template: any): string {
        if (template) {
            return template['datatype'];
        }
        return '';
    }

    public getColumnSuppressed(template: any): boolean {
        if (template) {
            return template['suppressOutput'];
        }
        return false;
    }
}
