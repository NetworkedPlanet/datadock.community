import { MetadataViewModel } from '../../shared/metadata-viewmodel';

export class MockFormFieldService {
    getMetadataViewModel(prefix: string, filename: string, columnHeadings: string[], rowDataSample: Array<any>): MetadataViewModel {
        let mvm = new MetadataViewModel();
        return mvm;
    }
}
