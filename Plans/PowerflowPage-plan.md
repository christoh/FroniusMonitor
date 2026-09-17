# Powerflow page

I want a cool power flow page in HomeAutomationClient.

## Wiring

"Electricity Price" becomes "Settings" and this get two sub-items: "Electricity Price" and "Power Flow"

## Implementation
- We want to visualize how power flows in our house
- We have solar production (incoming), grid (incoming and outgoing), battery (incoming and outgoing) plus the power consumers like Wattpilot and Fritzbox devices. Use a List of IPowerConsumer1P (those who have IPowerMeter1P.CanMeasurePower enabled) so we can add other device types later. 
- No details should be displayed for the device. Just the name and the active power.
- Show a cool animated connection between the device. The animation should visualize how much power is flowing (e.g. fast/slow animation)

