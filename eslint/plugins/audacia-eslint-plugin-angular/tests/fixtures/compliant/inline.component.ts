import { Component } from '@angular/core';

@Component({
    selector: 'app-inline',
    template: `<button type="button" (click)="save()" (keydown.enter)="save()">Save</button>`,
    standalone: true,
})
export class InlineComponent {}
