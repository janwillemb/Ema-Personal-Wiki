export class Stack<T> {

    private store: T[] = [];

    push(val: T) {
        this.store.push(val);
    }
    pop(): T {
        return this.store.pop();
    }
    peek(): T {
        return this.store[this.store.length - 1];
    }
    clear() {
        this.store = [];
    }
    get length(): number {
        return this.store.length;
    }
}