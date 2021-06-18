import { Parsable } from "./parsable";

export interface ParseNode {
    getStringValue(): string;
    getChildNode(identifier: string): ParseNode;
    getBooleanValue(): boolean;
    getNumberValue(): number;
    getGuidValue(): string; //TODO https://www.npmjs.com/package/guid-typescript
    getDateValue(): Date;
    getCollectionOfPrimitiveValues<T>(): T[] | undefined;
    getCollectionOfObjectValues<T extends Parsable>(type: new() => T): T[] | undefined;
    getObjectValue<T extends Parsable>(type: new() => T): T;
    getEnumValues<T>(type: any): T[];
    getEnumValue<T>(type: any): T | undefined;
    onBeforeAssignFieldValues: ((value: Parsable) => void) | undefined;
    onAfterAssignFieldValues: ((value: Parsable) => void) | undefined;
}