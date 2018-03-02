import { Component } from '@angular/core';
import { Observable } from 'rxjs/Observable';
import { HubConnection } from '@aspnet/signalr';

@Component({
    selector: 'home',
    templateUrl: './home.component.html'
})
export class HomeComponent {
    private _hubConnection: HubConnection;
    messages: string[] = [];
    ngOnInit() {
        this._hubConnection = new HubConnection("/progress");
        this._hubConnection.on('progressUpdated', (userId: string, jobId: string, message: string) => {
            this.messages.push('User: ' + userId + ' Job: ' + jobId + ' Message: ' + message);
        });
        this._hubConnection.start()
            .then(() => {
                console.log('Hub connection started');
            })
            .catch(err => {
                console.log('Error while establishing connection');
            });
    }
}
