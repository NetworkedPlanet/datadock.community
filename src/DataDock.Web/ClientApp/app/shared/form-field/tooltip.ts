/*
 Params:
 - text: text of tooltip
 - placement: tooltip positioning instruction, supported positions: 'top', 'bottom', 'left', 'right'
 - animation: if `false` fade tooltip animation will be disabled
 - enable: if `false` tooltip is disabled and will not be shown
 - isOpen: if `true` tooltip is currently visible
 */
export class Tooltip {

    constructor(public text: string, public placement = 'top', public animation = true,
				public enable = true, public isOpen = false) {
    	if (!this.text) {
    		this.enable = false;
		}
	}
}
