import { BlazorSizeMedia } from './blazorSizeMedia.js';
let instance = new BlazorSizeMedia();
// When MQL is created, it tracks a NET Ref 
export function addMediaQueryList(dotnet) {
    //console.log(`addMediaQueryList dotnet:`);
    //console.log(dotnet);
    instance.addMediaQueryList(dotnet);
}
// OnDispose this method cleans the MQL instance
export function removeMediaQueryList(dotnetMql) {
    instance.removeMediaQueryList(dotnetMql);
}
// When MediaQuery instance is created track media query in JavaScript
export function addMediaQueryToList(dotnetMql, mediaQuery) {
    //console.log(`addMediaQueryToList dotnet:`);
    //console.log(dotnetMql);
    return instance.addMediaQueryToList(dotnetMql, mediaQuery);
}
// When MediaQuery instance is disposed remove media query in JavaScript
export function removeMediaQuery(dotnetMql, mediaQuery) {
    //console.log(`removeMediaQuery dotnet`);
    //console.log(dotnetMql);
    instance.removeMediaQuery(dotnetMql, mediaQuery);
}
// Get media query value for cache after app loads
export function getMediaQueryArgs(mediaQuery) {
    return instance.getMediaQueryArgs(mediaQuery);
}
