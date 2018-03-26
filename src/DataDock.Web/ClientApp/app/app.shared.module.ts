import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
import { RouterModule } from '@angular/router';
import { PapaParseModule } from 'ngx-papaparse';
import { AppComponent } from './components/app/app.component';
import { FileComponent } from './components/file/file.component';
import { StepsComponent } from './components/steps/steps.component';
import { TagInputModule } from 'ngx-chips';
import { CallbackPipe } from './pipes/callback.pipe';
import { MetadataComponent } from './components/metadata/metadata.component';
import { DatasetComponent } from './components/metadata/dataset.component';
import { ColumnDefinitionsComponent } from './components/metadata/column-definitions.component';
import { AdvancedComponent } from './components/metadata/advanced.component';
import { PreviewComponent } from './components/metadata/preview.component';
import { FormFieldComponent } from './shared/form-field/form-field.component';
import { DeveloperComponent } from './components/developer/developer.component';
import { NotFoundComponent } from './components/not-found/not-found.component';
import { BrowserWarningsComponent } from './components/browser-warnings/browser-warnings.component';
import { TabsComponent } from './components/tabs/tabs.component';
import { SchemaHelperService } from './shared/schema-helper.service';
import { SchemaService } from './shared/schema.service';
import { CsvFile } from './shared/csv-file';
import { DatatypeService } from './shared/datatype.service';
import { ApiService } from './shared/api.service';
import { UserService } from './shared/user.service';
import { FormManager } from './shared/form-manager';
import { ViewModelHelperService } from './shared/viewmodel-helper.service';
import { ImportHelperService } from './shared/import-helper.service';


@NgModule({
    declarations: [
        AppComponent,
        CallbackPipe,
        FileComponent,
        TabsComponent,
        MetadataComponent,
        DatasetComponent,
        ColumnDefinitionsComponent,
        AdvancedComponent,
        PreviewComponent,
        FormFieldComponent,
        DeveloperComponent,
        BrowserWarningsComponent,
        NotFoundComponent,
        StepsComponent
    ],
    imports: [
        CommonModule,
        HttpModule,
        FormsModule,
        PapaParseModule,
        TagInputModule,
        RouterModule.forRoot([
            {
                path: ':ownerId',
                children: [
                    {
                        path: ':repoId',
                        children: [
                            { path: 'import', component: FileComponent },
                            { path: 'import/:schemaId', component: FileComponent },
                            { path: 'define', component: FileComponent }
                        ]
                    }
                ]
            },
            { path: '**', component: FileComponent }
        ])
    ],
    providers: [
        ImportHelperService,
        ViewModelHelperService,
        FormManager,
        UserService,
        ApiService,
        DatatypeService,
        CsvFile,
        SchemaService,
        SchemaHelperService
    ]
})
export class AppModuleShared {
}
