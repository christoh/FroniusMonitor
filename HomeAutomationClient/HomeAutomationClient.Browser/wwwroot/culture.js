// The languages the user configured in the browser, most preferred first, for example
// ["de-CH", "de", "en-US", "en"]. Only navigator.languages knows the whole list; navigator.language is the
// first of them and serves as the fallback for the rare browser that does not have the list.
export function getUserLanguages() {
    const languages = globalThis.navigator?.languages;

    const list = languages?.length > 0
        ? Array.from(languages)
        : [globalThis.navigator?.language];

    return list.filter(language => typeof language === 'string' && language.length > 0);
}

// The cultures this build actually ships a satellite assembly for, in the exact spelling the loader expects.
// It matches its argument against the keys of this list with ===, so "de-CH" would silently load nothing where
// the build calls the culture "de-ch" - which it does, because the file is named Resources.de-ch.resx. Asking
// the build removes both that trap and the need to keep a second list of languages by hand.
export function getSupportedCultures() {
    const satellites = globalThis.getDotnetRuntime?.(0)?.getConfig?.()?.resources?.satelliteResources;
    return satellites ? Object.keys(satellites) : [];
}
