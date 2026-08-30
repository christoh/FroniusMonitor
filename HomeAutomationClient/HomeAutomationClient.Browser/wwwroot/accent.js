// The accent color of the OS is not available to a web page through any JavaScript API: the title bar that most
// browsers tint with it belongs to the browser, not to the document. The only channel is the CSS system color
// 'AccentColor' of CSS Color Level 4, which Firefox and Safari resolve to the real value, while Chromium (as of
// this writing) does not know the keyword at all. Hence the feature detection: where it is missing, we return null
// and the app keeps the accent color of its theme.
export function getAccentColor() {
    if (!globalThis.CSS?.supports('color', 'AccentColor')) {
        return null;
    }

    const probe = document.createElement('div');
    probe.style.cssText = 'color: AccentColor; position: absolute; visibility: hidden';
    document.body.appendChild(probe);
    const color = getComputedStyle(probe).color;
    probe.remove();

    // Every engine that knows the keyword reports 'rgb(r, g, b)' or 'rgba(r, g, b, a)'. Anything else (a color()
    // function with floating point components, for instance) would need different math, so we rather give up.
    if (!color.startsWith('rgb')) {
        return null;
    }

    const components = color.match(/[\d.]+/g);

    if (!components || components.length < 3) {
        return null;
    }

    const hex = component => Math.min(255, Math.max(0, Math.round(Number(component)))).toString(16).padStart(2, '0');

    // '#AARRGGBB', which is what HaColor.Parse expects. The accent color is always opaque.
    return '#ff' + hex(components[0]) + hex(components[1]) + hex(components[2]);
}
