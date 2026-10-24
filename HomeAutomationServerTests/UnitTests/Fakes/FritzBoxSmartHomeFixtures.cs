namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>
/// One moment of a real box (FRITZ!OS 8.25) twice: as the Smart Home REST API and as AHA describe it. Names and AINs are
/// made up, the values are the box's. Two outlets and two lamps (one in white light, one in color with an unknown level),
/// plus a third outlet copied from the first and put out of radio range: AHA leaves its values empty, the REST API leaves
/// them out and reports <c>notConnected</c>, as its specification says (the box had no such device to look at).
/// </summary>
internal static class FritzBoxSmartHomeFixtures
{
    /// <summary><c>GET api/v0/smarthome/overview</c>, without <c>globals</c> (the location of the house).</summary>
    public const string Overview = """
        {
          "devices": [
            {
              "isDeviceSubscribedLocally": true,
              "productCategory": "socket",
              "isZigbeeDevice": false,
              "UID": "08761 0000001",
              "unitUids": [
                "08761 0000001"
              ],
              "ain": "08761 0000001",
              "manufacturer": "AVM",
              "isBatteryPowered": false,
              "isUpdateAvailable": false,
              "isExternallyPowered": true,
              "productName": "FRITZ!Smart Energy 200",
              "icons": [],
              "isDeviceDeletable": true,
              "name": "Outlet sofa",
              "isConnected": true,
              "batteryState": "unknown",
              "firmwareVersion": "04.32"
            },
            {
              "isDeviceSubscribedLocally": true,
              "productCategory": "socket",
              "isZigbeeDevice": false,
              "UID": "11630 0000002",
              "unitUids": [
                "11630 0000002"
              ],
              "ain": "11630 0000002",
              "manufacturer": "AVM",
              "isBatteryPowered": false,
              "isUpdateAvailable": false,
              "isExternallyPowered": true,
              "productName": "FRITZ!Smart Energy 200",
              "icons": [],
              "isDeviceDeletable": true,
              "name": "Outlet office",
              "isConnected": true,
              "batteryState": "unknown",
              "firmwareVersion": "04.32"
            },
            {
              "isDeviceSubscribedLocally": true,
              "productCategory": "lamp",
              "isZigbeeDevice": false,
              "UID": "13077 0000003",
              "unitUids": [
                "13077 0000003-1"
              ],
              "ain": "13077 0000003",
              "manufacturer": "AVM",
              "isBatteryPowered": false,
              "isUpdateAvailable": false,
              "isExternallyPowered": true,
              "productName": "FRITZ!Smart Light 500",
              "icons": [],
              "isDeviceDeletable": true,
              "name": "Lamp office",
              "isConnected": true,
              "batteryState": "unknown",
              "firmwareVersion": "34.10.16.16.015"
            },
            {
              "isDeviceSubscribedLocally": false,
              "productCategory": "lamp",
              "isZigbeeDevice": false,
              "UID": "13077 0000004",
              "unitUids": [
                "13077 0000004-1"
              ],
              "ain": "13077 0000004",
              "manufacturer": "AVM",
              "isBatteryPowered": false,
              "isUpdateAvailable": false,
              "isExternallyPowered": true,
              "productName": "FRITZ!Smart Light 500",
              "icons": [],
              "isDeviceDeletable": true,
              "name": "Lamp door",
              "isConnected": true,
              "batteryState": "unknown",
              "firmwareVersion": "34.10.16.16.015"
            },
            {
              "isDeviceSubscribedLocally": true,
              "productCategory": "socket",
              "isZigbeeDevice": false,
              "UID": "08761 0000005",
              "unitUids": [
                "08761 0000005"
              ],
              "ain": "08761 0000005",
              "manufacturer": "AVM",
              "isBatteryPowered": false,
              "isUpdateAvailable": false,
              "isExternallyPowered": true,
              "productName": "FRITZ!Smart Energy 200",
              "icons": [],
              "isDeviceDeletable": true,
              "name": "Outlet garden",
              "isConnected": false,
              "batteryState": "unknown",
              "firmwareVersion": "04.32"
            }
          ],
          "units": [
            {
              "groupUid": "",
              "deviceUid": "08761 0000001",
              "interfaces": {
                "multimeterInterface": {
                  "state": "valid",
                  "current": 0,
                  "power": 0,
                  "energyKeyFigures": {
                    "co2emmissions": 550,
                    "electricityRate": 25
                  },
                  "energy": 226533,
                  "voltage": 232993
                },
                "onOffInterface": {
                  "state": "valid",
                  "isLockedDeviceApi": false,
                  "active": false,
                  "isLockedDeviceLocal": false,
                  "outletState": "valid"
                },
                "temperatureInterface": {
                  "state": "valid",
                  "celsius": 21
                }
              },
              "icons": [],
              "UID": "08761 0000001",
              "name": "Outlet sofa",
              "isConnected": true,
              "ain": "08761 0000001",
              "isGroupUnit": false,
              "parentUid": "08761 0000001",
              "unitType": "avmPlugSocket"
            },
            {
              "groupUid": "",
              "deviceUid": "11630 0000002",
              "interfaces": {
                "multimeterInterface": {
                  "state": "valid",
                  "current": 1152,
                  "power": 228380,
                  "energyKeyFigures": {
                    "co2emmissions": 550,
                    "electricityRate": 25
                  },
                  "energy": 1208122,
                  "voltage": 231867
                },
                "onOffInterface": {
                  "state": "valid",
                  "isLockedDeviceApi": true,
                  "active": true,
                  "isLockedDeviceLocal": false,
                  "outletState": "valid"
                },
                "temperatureInterface": {
                  "state": "valid",
                  "celsius": 24.5
                }
              },
              "icons": [],
              "UID": "11630 0000002",
              "name": "Outlet office",
              "isConnected": true,
              "ain": "11630 0000002",
              "isGroupUnit": false,
              "parentUid": "11630 0000002",
              "unitType": "avmPlugSocket"
            },
            {
              "groupUid": "",
              "deviceUid": "13077 0000003",
              "interfaces": {
                "colorControlInterface": {
                  "state": "valid",
                  "supportedColorModes": [
                    "hueSaturation",
                    "temperature"
                  ],
                  "currentColorMode": "temperature",
                  "colorTemperature": 3000
                },
                "levelControlInterface": {
                  "state": "valid",
                  "level": 100
                },
                "onOffInterface": {
                  "state": "valid",
                  "active": false
                }
              },
              "icons": [],
              "UID": "13077 0000003-1",
              "name": "Lamp office",
              "isConnected": true,
              "ain": "13077 0000003-1",
              "isGroupUnit": false,
              "parentUid": "13077 0000003",
              "unitType": "dimmableColorBulb"
            },
            {
              "groupUid": "",
              "deviceUid": "13077 0000004",
              "interfaces": {
                "colorControlInterface": {
                  "state": "valid",
                  "activeHsColorPreset": {},
                  "currentColorMode": "hueSaturation",
                  "hsColor": {
                    "hue": 270,
                    "saturation": 255
                  },
                  "supportedColorModes": [
                    "hueSaturation",
                    "temperature"
                  ]
                },
                "levelControlInterface": {
                  "state": "unknown"
                },
                "onOffInterface": {
                  "state": "valid",
                  "active": false
                }
              },
              "icons": [],
              "UID": "13077 0000004-1",
              "name": "Lamp door",
              "isConnected": true,
              "ain": "13077 0000004-1",
              "isGroupUnit": false,
              "parentUid": "13077 0000004",
              "unitType": "dimmableColorBulb"
            },
            {
              "groupUid": "",
              "deviceUid": "08761 0000005",
              "interfaces": {
                "multimeterInterface": {
                  "state": "notConnected",
                  "energyKeyFigures": {
                    "co2emmissions": 550,
                    "electricityRate": 25
                  }
                },
                "onOffInterface": {
                  "state": "notConnected",
                  "isLockedDeviceApi": false,
                  "isLockedDeviceLocal": false,
                  "outletState": "valid"
                },
                "temperatureInterface": {
                  "state": "notConnected"
                }
              },
              "icons": [],
              "UID": "08761 0000005",
              "name": "Outlet garden",
              "isConnected": false,
              "ain": "08761 0000005",
              "isGroupUnit": false,
              "parentUid": "08761 0000005",
              "unitType": "avmPlugSocket"
            }
          ],
          "templates": [
            {
              "name": "Template 1",
              "ain": "tmp000000-418B80CE0",
              "template": {
                "memberUnitUids": [
                  "13077 0000003-1",
                  "13077 0000004-1",
                  "08761 0000001"
                ],
                "isAutoCreated": false,
                "memberType": "onOff"
              },
              "applyType": [
                "relayManual",
                "level",
                "color"
              ],
              "UID": "tmp000000-418B80CE0"
            },
            {
              "name": "Template 2",
              "ain": "tmp000000-418B7DE7F",
              "scenario": {
                "icons": [
                  3
                ],
                "templateUids": [
                  "tmp000000-418B80CE0"
                ]
              },
              "applyType": [
                "subTemplates"
              ],
              "UID": "tmp000000-418B7DE7F"
            }
          ],
          "triggers": [
            {
              "ain": "trg000000-419524153",
              "enabled": true,
              "name": "Routine 1",
              "UID": "trg000000-419524153"
            }
          ],
          "groups": []
        }
        """;

    /// <summary><c>switchcmd=getdevicelistinfos</c>. Each lamp is in it twice, as device and as unit.</summary>
    public const string DeviceList = """
        <devicelist version="1" fwversion="8.25">
          <device identifier="08761 0000001" id="30" functionbitmask="33664" fwversion="04.32" manufacturer="AVM" productname="FRITZ!Smart Energy 200"><present>1</present><txbusy>0</txbusy><name>Outlet sofa</name><switch><state>0</state><mode>auto</mode><lock>0</lock><devicelock>0</devicelock></switch><simpleonoff><state>0</state></simpleonoff><powermeter><voltage>232993</voltage><power>0</power><energy>226533</energy></powermeter><temperature><celsius>210</celsius><offset>0</offset></temperature></device>
          <device identifier="11630 0000002" id="25" functionbitmask="35712" fwversion="04.32" manufacturer="AVM" productname="FRITZ!Smart Energy 200"><present>1</present><txbusy>0</txbusy><name>Outlet office</name><switch><state>1</state><mode>auto</mode><lock>1</lock><devicelock>0</devicelock></switch><simpleonoff><state>1</state></simpleonoff><powermeter><voltage>231867</voltage><power>228380</power><energy>1208122</energy></powermeter><temperature><celsius>245</celsius><offset>0</offset></temperature></device>
          <device identifier="13077 0000003" id="406" functionbitmask="1" fwversion="34.10.16.16.015" manufacturer="AVM" productname="FRITZ!Smart Light 500"><present>1</present><txbusy>0</txbusy><name>Lamp office</name></device>
          <device identifier="13077 0000003-1" id="2000" functionbitmask="237572" fwversion="0.0" manufacturer="AVM" productname="FRITZ!Smart Light 500"><present>1</present><txbusy>0</txbusy><name>Lamp office</name><simpleonoff><state>0</state></simpleonoff><levelcontrol><level>255</level><levelpercentage>100</levelpercentage></levelcontrol><colorcontrol supported_modes="5" current_mode="4"  fullcolorsupport="1" mapped="0"><hue></hue><saturation></saturation><unmapped_hue></unmapped_hue><unmapped_saturation></unmapped_saturation><temperature>3000</temperature></colorcontrol><etsiunitinfo><etsideviceid>406</etsideviceid><unittype>278</unittype><interfaces>512,514,513</interfaces></etsiunitinfo></device>
          <device identifier="13077 0000004" id="20002" functionbitmask="1" fwversion="34.10.16.16.015" manufacturer="AVM" productname="FRITZ!Smart Light 500"><present>1</present><txbusy>0</txbusy><name>Lamp door</name></device>
          <device identifier="13077 0000004-1" id="2003" functionbitmask="237572" fwversion="0.0" manufacturer="AVM" productname=""><present>1</present><txbusy>0</txbusy><name>Lamp door</name><simpleonoff><state>0</state></simpleonoff><levelcontrol><level></level><levelpercentage></levelpercentage></levelcontrol><colorcontrol supported_modes="5" current_mode="1"  fullcolorsupport="1" mapped="0"><hue>270</hue><saturation>255</saturation><unmapped_hue>270</unmapped_hue><unmapped_saturation>255</unmapped_saturation><temperature></temperature></colorcontrol><etsiunitinfo><etsideviceid>20002</etsideviceid><unittype>278</unittype><interfaces>512,514,513</interfaces></etsiunitinfo></device>
          <device identifier="08761 0000005" id="40" functionbitmask="33664" fwversion="04.32" manufacturer="AVM" productname="FRITZ!Smart Energy 200"><present>0</present><txbusy>0</txbusy><name>Outlet garden</name><switch><state></state><mode>auto</mode><lock>0</lock><devicelock>0</devicelock></switch><simpleonoff><state></state></simpleonoff><powermeter><voltage></voltage><power></power><energy></energy></powermeter><temperature><celsius></celsius><offset>0</offset></temperature></device>
        </devicelist>
        """;
}
