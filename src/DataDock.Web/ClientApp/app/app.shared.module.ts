import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
import { RouterModule } from '@angular/router';
import { PapaParseModule } from 'ngx-papaparse';
import { AppComponent } from './components/app/app.component';
import { FileComponent } from './components/file/file.component';


@NgModule({
    declarations: [
        AppComponent,
        FileComponent
    ],
    imports: [
        CommonModule,
        HttpModule,
        FormsModule,
        PapaParseModule,
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
    ]
})
export class AppModuleShared {
}
