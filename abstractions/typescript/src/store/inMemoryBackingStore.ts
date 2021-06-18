import { BackingStore } from "./backingStore";
import _ from "lodash";
import { v4 as uuidv4 } from 'uuid';

type storeEntryWrapper = {changed: boolean, value: unknown};
type subscriptionCallback = (key: string, previousValue: unknown, newValue: unknown) => void;
type storeEntry = {key: string, value: unknown};

export class InMemoryBackingStore implements BackingStore {
    public get<T>(key: string): T | undefined {
        const wrapper = this.store.get(key);
        if(wrapper && 
            (this.returnOnlyChangedValues && wrapper.changed || 
                !this.returnOnlyChangedValues)) {
            return wrapper.value as T;
        }
        return undefined;
    }
    public set<T>(key: string, value: T): void {
        var oldValueWrapper = this.store.get(key);
        var oldValue = oldValueWrapper?.value;
        if(oldValueWrapper) {
            oldValueWrapper.value = value;
            oldValueWrapper.changed = this.initializationCompleted;
        } else {
            this.store.set(key, {
                changed: this.initializationCompleted,
                value: value
            });
        }
        this.subscriptions.forEach(sub => {
            sub(key, oldValue, value);
        });
    }
    public enumerate(): storeEntry[] {
        return _.map(this.returnOnlyChangedValues ? 
                        _.filter(this.store, (_, k) => this.store.get(k)?.changed ?? false) : 
                        this.store, 
            (_, k) => { return { key: k, value: this.store.get(k)?.value}});
    }
    public subscribe(callback: subscriptionCallback, subscriptionId?: string | undefined): string {
        if(!callback)
            throw new Error("callback cannot be undefined");
        subscriptionId = subscriptionId ?? uuidv4();
        this.subscriptions.set(subscriptionId, callback);
        return subscriptionId;
    }
    public unsubscribe(subscriptionId: string): void {
        this.subscriptions.delete(subscriptionId);
    }
    public clear(): void {
        this.store.clear();
    }
    private readonly subscriptions: Map<string, subscriptionCallback> = new Map<string, subscriptionCallback>();
    private readonly store: Map<string, storeEntryWrapper> = new Map<string, storeEntryWrapper>();
    public returnOnlyChangedValues: boolean = false;
    private _initializationCompleted: boolean = true;
    public set initializationCompleted(value: boolean) {
        this._initializationCompleted = value;
        this.store.forEach((v) => {
            v.changed = !value;
        });
    }
    public get initializationCompleted() {
        return this._initializationCompleted;
    }
}