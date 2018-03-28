export class MockSchemaHelperService {

    public hasSchema: boolean;
    public schemaTitle: string;

    public setSchema(ownerId: string, schemaId: string, schemaInfo: any) {
    }
    public getSchema() {}

    public getMetadataIdentifier(aboutUrlPrefix: string, dft: string) {}

    public getMetadataIdentifierColumnName() {}

    public getMetadataTitle(): string {
        return '';
    }

    public getMetadataDescription(): string {
        return '';
    }

    public getMetadataLicenseUri(): string {
        return '';
    }

    public getMetadataTags(): string[] {
        return [];
    }

    public getMetadataColumnTemplate(columnName: string): any {}

    public getColumnTitle(template: any, dft: string): string {
        return '';
    }

    public getColumnPropertyUrl(template: any, dft: string): string {
        return '';
    }

    public getColumnDatatype(template: any): string {
        return '';
    }

    public getColumnSuppressed(template: any): boolean {
        return false;
    }
}
