import { NgModule, APP_INITIALIZER } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
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
import { TabsComponent } from './components/tabs/tabs.component';
import { SchemaHelperService } from './shared/schema-helper.service';
import { SchemaService } from './shared/schema.service';
import { CsvFile } from './shared/csv-file';
import { DatatypeService } from './shared/datatype.service';
import { ApiService } from './shared/api.service';
import { UserService } from './shared/user.service';
import { FormManager } from './shared/form-manager';
import { ViewModelHelperService } from './shared/viewmodel-helper.service';
import { AppService } from './shared/app.service';
import { ConfigurationService } from './shared/services/config.service';
import { Globals } from './globals';
import { routing } from './app.routing';


@NgModule({
    imports: [
        CommonModule,
        HttpModule,
        FormsModule,
        ReactiveFormsModule,
        PapaParseModule,
        TagInputModule,
        routing
    ],
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
        NotFoundComponent,
        StepsComponent
    ],
    providers: [
        Globals,
        AppService,
        ViewModelHelperService,
        FormManager,
        UserService,
        ApiService,
        DatatypeService,
        CsvFile,
        SchemaService,
        SchemaHelperService,
        ConfigurationService,
        {
            // Here we request that configuration loading be done at app-
            // initialization time (prior to rendering)
            provide: APP_INITIALIZER,
            useFactory: (configService: ConfigurationService) =>
                () => configService.loadConfigurationData(),
            deps: [ConfigurationService],
            multi: true
        }
    ]
})
export class AppModuleShared {
}
