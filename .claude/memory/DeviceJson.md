---
paths:
  - Fronius/Extensions/JsonExtensions.cs
  - Fronius/Extensions/WattPilotExtensions.cs
  - Fronius/Services/Gen24JsonService.cs
  - Fronius/Services/Gen24Service.cs
  - Fronius/Services/WattPilotService.cs
  - Fronius/Services/DigestAuthHttp.cs
  - Fronius/Models/Charging/**
  - Fronius/Models/Gen24/**
---

# Reading and writing device JSON

Everything a Gen24 inverter or a WattPilot says arrives as JSON and is read through `System.Text.Json.Nodes`
(`JsonNode` / `JsonObject` / `JsonArray`). This was Newtonsoft until 2026-09-08; the conversion is what most of
this document is about, because the two libraries differ in ways that fail **silently** rather than loudly.

`Newtonsoft.Json` is still a package reference of `Fronius`, so it is not gone from the repo: the WattPilot models
still carry its `JsonProperty` attributes (dead, see below), `WebConnection` and the browser `Cache` use it, and
`FroniusMonitor/Models/CarCharging/NativeFirmwareBootObject.cs` uses a `JObject` as a kernel parameter bag.

## A device is not consistent about types, so everything is read as text

An inverter writes the same channel as `5` in one firmware and `"5"` in the next. `Gen24JsonService` has always
dealt with that by reading every value as text and converting from there.

`JToken.Value<string>()` did that coercion. **`JsonNode.GetValue<string>()` does not** - it throws unless the
value already is a string. `JsonExtensions.AsString` is the replacement, and everything else in that file
(`AsDouble`, `AsInt32`, `AsUInt32`, `AsBoolean`) reads through it:

- a JSON string, number, or `true`/`false` all come back as text; an object or an array answers `null` where
  Newtonsoft threw
- a **whole** number keeps the digits it was written with, so a long serial number does not lose its last one
- anything else - a decimal, an exponent - is written out through a `double` first, because `1e3` is valid JSON
  and `Convert.ChangeType` will not read that text into an `int`. Newtonsoft behaved the same way, by holding
  whole numbers in a `long` and the rest in a `double`
- `AsBoolean` takes `1` and `0` as well as `true` and `false`, which is what an inverter writes for a channel flag

`HasValues()` judges whether a settings delta has anything in it. It is **not** quite Newtonsoft's `HasValues`:
there a scalar was false ("has no children"), here it is true, because the question is whether there is something
to send. `HasAnyValue()` is the recursive one, for a delta that is a tree of empty objects.

`HomeAutomationServerTests/UnitTests/JsonExtensionsTests.cs` feeds the same JSON to `JToken.Parse` and
`JsonNode.Parse` and asserts the two answer the same text. Newtonsoft is the oracle there, which is the only
honest way to claim the conversion changed nothing - keep it that way while the package is still referenced.

## What System.Text.Json cannot do that Newtonsoft could

- **`NaN`, `Infinity` and `-Infinity` cannot be parsed at all.** `JsonNode.Parse` throws, and
  `JsonSerializerOptions.NumberHandling = AllowNamedFloatingPointLiterals` does not help, because it applies to a
  `float`/`double` target and not to a `JsonNode`. Newtonsoft read them by default. Nothing in the repo is known
  to send them - the `AllowNamedFloatingPointLiterals` in `WebClientService`, `UpdateService`, `Program` and
  `SignalRRegistration` is the server-to-client channel, which serializes typed models and is untouched by this -
  but a device that ever writes one now fails the **whole** document instead of one channel. If that turns up,
  the fix is to sanitise the text before parsing; there is no option to switch on.
- `JToken.ToString()` was indented, `ToJsonString()` is compact. Only the size of a PUT body.

## The WattPilot wire format is stated by `WattPilotAttribute`, and nowhere else

This is the part that broke twice during the conversion, both times without an error message.

The charging models carry **two** sets of names on purpose:

| attribute | for | example on `WattPilotWifiInfo.IpV4AddressString` |
|---|---|---|
| `[WattPilot("ip")]` | the WebSocket protocol of the charger | `ip` |
| `[JsonPropertyName("ipV4Address")]` | the server-to-client channel | `ipV4Address` |
| `[JsonProperty("ip")]` (Newtonsoft) | **dead** - was the charger protocol | `ip` |

So a `JsonSerializer` call on one of these models produces the *client* names, or the C# names where there are
none. Both are wrong for the charger, and neither the charger nor the serializer complains:

- **Writing.** `WattPilotExtensions.ToWattPilotJson` writes a value, and `ToWattPilotObject` writes a nested one
  from the `WattPilotAttribute`s - never through `JsonSerializer`. An attribute with an `Index` names one slot of
  an array the charger *sends* (the `f` capabilities array), so it is not something to write back and is skipped.
- **Reading.** `SetWattPilotValue` handles a `JsonObject`, and a list of them, through the same attributes before
  it ever offers the value to `JsonSerializer.Deserialize`. Left to the serializer, a `{ "amp": 16, ... }` is
  taken happily and answers an object with **every property at its default** - the charger appears to report
  zeroes, and there is no exception anywhere.
- **A `byte[]` is base 64 to System.Text.Json**, in both directions. The charger writes the phase map as an array
  of numbers, so `SetWattPilotValue` reads that shape itself and `ToWattPilotJson` writes it.
- The **text fallback** at the end of `SetWattPilotValue` is not optional: `Deserialize` refuses a JSON string
  where a number is wanted, and a number where a flag is wanted. A flag goes through `AsBoolean` because
  `Convert.ToBoolean` refuses `"1"`.

`HomeAutomationServerTests/UnitTests/WattPilotJsonTests.cs` pins all of that, including that a written object
reads back through the reader unchanged. If you touch either side, that round trip is the test that matters.

## Writing to an inverter

`Gen24JsonService.GetUpdateToken` builds the delta and `ToJsonValue` turns one value into a `JsonNode` through an
explicit type switch. Deliberately not `JsonValue.Create<object>`, which would serialize the runtime type by
reflection - trimmed away on iOS, and quietly wrong for a type it does not know. A type nobody has thought about
throws, so a property added later says so on the first write rather than through an inverter refusing a payload.

`ReadSingleProperty` returns `object?` and not `dynamic` - see the rule in `CLAUDE.md`: dynamic dispatch does not
exist on iOS. The Joule and percent scaling it ends with therefore says which type it works in.
