import { Globals } from '../globals';
export class DashboardModel {
    area: string;
    subtitle: string;
    userId: string;
    requestedOwnerId: string;
    ownerRepositoryIds: string[];
    requestedRepositoryId: string;
    ownerList: OwnerListModel;
    schemaId: string;
    schemaTitle: string;

    constructor(dashboardModelJson: string) {
        this.subtitle = 'Add data';
        this.ownerRepositoryIds = [];
        if (dashboardModelJson !== '') {
            let dashJson = JSON.parse(dashboardModelJson);
            this.area = dashJson['Area'];
            this.userId = dashJson['UserId'];
            this.requestedOwnerId = dashJson['RequestedOwnerId'];
            this.requestedRepositoryId = dashJson['RepositoryId'];
            this.ownerRepositoryIds = dashJson['OwnerRepositoryIds'];
            this.ownerList = new OwnerListModel(dashJson['OwnerSelectionList']);
            this.schemaId = dashJson['SchemaId'];
            this.schemaTitle = dashJson['SchemaTitle'];

            // check whether repoIdList contains the currently selected repo
            let repoInList = (this.ownerRepositoryIds.indexOf(this.requestedRepositoryId) > -1);
            if (!repoInList) {
                this.ownerRepositoryIds.unshift(this.requestedRepositoryId);
            }

            console.log('DashboardModel', this);
        }

    }
}

export class OwnerListModel {
    currentOwnerId: string;
    currentAvatarUrl: string;
    owners: OwnerModel[];

    constructor(ownerListJson: any) {
        this.owners = [];
        this.currentOwnerId = ownerListJson['CurrentOwnerId'];
        this.currentAvatarUrl = ownerListJson['CurrentOwnerAvatarUrl'];
        let ownerList = ownerListJson['Owners'];
        if (ownerList) {
            for (let i = 0; i < ownerList.length ; i++) {
                let owner = new OwnerModel(ownerList[i]);
                this.owners.push(owner);
            }
        }
    }
}

export class OwnerModel {
    ownerId: string;
    avatarUrl: string;

    constructor(ownerJson: any) {
        this.ownerId = ownerJson['OwnerId'];
        this.avatarUrl = ownerJson['AvatarUrl'];
    }
}

