import { Injectable } from '@angular/core';
import { Configuration } from './shared/models/configuration';

@Injectable()
export class Globals {
    apiUrl: string;
    config: Configuration;
}