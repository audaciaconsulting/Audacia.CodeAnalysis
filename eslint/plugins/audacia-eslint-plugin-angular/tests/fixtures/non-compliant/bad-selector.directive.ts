import { Directive } from '@angular/core';

@Directive({
    selector: '[xyzHighlight]',
    standalone: true,
})
export class BadSelectorDirective {}
