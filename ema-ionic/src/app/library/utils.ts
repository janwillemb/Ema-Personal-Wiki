export class Utils {
    static serializeError(err: any): string {
        return JSON.stringify(err, Object.getOwnPropertyNames(err));
    }
}