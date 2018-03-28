import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';


@Component({
    selector: 'dd-tabs',
    templateUrl: './tabs.component.html',
    styleUrls: ['./tabs.component.css']
})
export class TabsComponent implements OnInit {

    @Input()
    activeTab: string;

    @Output()
    tabChange: EventEmitter<any> = new EventEmitter();

    constructor() {
    }

    ngOnInit(): void {
        this.changeTab('dataset-info');
    }

    changeTab(activeTab: string) {
        this.activeTab = activeTab;
        this.tabChange.emit(this.activeTab);
    }

}
