import { RouterModule, Routes } from '@angular/router';
import { FileComponent } from './components/file/file.component';
import { MetadataComponent } from './components/metadata/metadata.component';
import { NotFoundComponent } from './components/not-found/not-found.component';

const routes: Routes = [
    { path: ':ownerId',
        children: [
            { path: ':repoId',
                children: [
                    { path: 'import', component: FileComponent },
                    { path: 'import/:schemaId', component: FileComponent },
                    { path: 'define', component: MetadataComponent }
                ]}
        ]},
    { path: '**', component: NotFoundComponent }
];

export const routing = RouterModule.forRoot(routes);
