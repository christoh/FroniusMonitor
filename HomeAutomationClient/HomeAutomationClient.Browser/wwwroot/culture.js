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
