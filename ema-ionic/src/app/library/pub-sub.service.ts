export class EmaSubscription {
    constructor(private action: () => void, public scope: string, private service: PubSubService) {
    }

    unsubscribe() {
        this.service.unsubscribe(this);
    }

    call() {
        this.action();
    }
}

export class PubSubService {

    private subs: EmaSubscription[] = [];

    publish(scope: string) {
        this.subs.filter(x => x.scope === scope)
            .forEach(sub => {
                sub.call();
            });
    }

    subscribe(scope: string, action: () => void): EmaSubscription {
        const sub = new EmaSubscription(action, scope, this);
        this.subs.push(sub);
        return sub;
    }

    unsubscribe(sub: EmaSubscription) {
        this.subs = this.subs.filter(s => s !== sub);
    }
}
